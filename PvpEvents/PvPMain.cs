﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace PvpEvents
{
	[ApiVersion(1, 23)]
	public class PvPMain : TerrariaPlugin
	{
		#region Plugin Info
		public override string Name { get { return "PvPEvents"; } }
		public override string Author { get { return "Zaicon"; } }
		public override string Description { get { return "A PvP Events plugin for tShock."; } }
		public override Version Version { get { return Assembly.GetExecutingAssembly().GetName().Version; } }

		public PvPMain(Main game)
			: base(game)
		{

		}
		#endregion

		#region Init/Dispose
		public override void Initialize()
		{
			ServerApi.Hooks.GameInitialize.Register(this, onInitialize);
			ServerApi.Hooks.GamePostInitialize.Register(this, onWorldLoad);
			ServerApi.Hooks.NetGetData.Register(this, onGetData);
			ServerApi.Hooks.ServerLeave.Register(this, onLeave);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ServerApi.Hooks.GameInitialize.Deregister(this, onInitialize);
				ServerApi.Hooks.GamePostInitialize.Deregister(this, onWorldLoad);
				ServerApi.Hooks.NetGetData.Deregister(this, onGetData);
				ServerApi.Hooks.ServerLeave.Deregister(this, onLeave);
			}
			base.Dispose(disposing);
		}
		#endregion
		
		public static EventHandler<PlayerDeathEventArgs> onPlayerDeath;
		public static EventHandler<PvPEventArgs> onPvPToggle;
		public static EventHandler<LeaveEventArgs> onPlayerLeave;
		public static PvPConfig pvpConfig;
		public static List<Tuple<int, int>> duelChallenges = new List<Tuple<int, int>>();

		#region Hooks
		private void onInitialize(EventArgs args)
		{
			pvpConfig = PvPConfig.Read();
			
			Commands.ChatCommands.Add(new Command("duel.start", DuelCMD, "duel"));
			Commands.ChatCommands.Add(new Command("pvp.reload", EventReload, "eventreload", "ereload"));
		}

		private void onWorldLoad(EventArgs args)
		{
			DuelEvent.Create();
		}

		private void onGetData(GetDataEventArgs args)
		{
			using (var reader = new BinaryReader(new MemoryStream(args.Msg.readBuffer, args.Index, args.Length)))
			{
				switch (args.MsgID)
				{
//					case PacketTypes.PlayerDamage:
//						byte playerid = reader.ReadByte();
//						byte hitdirection = reader.ReadByte();
//						short damage = reader.ReadInt16();
//						string deathtext = reader.ReadString();
//						byte flags = reader.ReadByte(); //1 - pvp | 2 - crit | 4 - cooldown countdown is -1 | 8 - cooldown countdown is 1
//#if DEBUG
//						//TSPlayer.Server.SendInfoMessage($"PlayerDamage: PlayerID {playerid} | HitDirection {hitdirection} | Damage {damage} | Deathtext {deathtext} | Flags {flags}");
//#endif
//						break;
					case PacketTypes.PlayerKillMe:
						byte playerid = reader.ReadByte();
						byte hitdirection = reader.ReadByte();
						short damage = reader.ReadInt16();
						bool pvp = reader.ReadBoolean();
						string deathtext = reader.ReadString();

						onPlayerDeath?.Invoke(this, new PlayerDeathEventArgs() { playerID = playerid, hitDirection = hitdirection, damage = damage, pvp = pvp, deathText = deathtext });
#if DEBUG
						File.AppendAllText("debug.txt", $"PlayerKillMe: PlayerID {playerid} | HitDirection {hitdirection} | Damage {damage} | PvP {pvp} | Deathtext {deathtext}");
						//TSPlayer.Server.SendInfoMessage($"PlayerKillMe: PlayerID {playerid} | HitDirection {hitdirection} | Damage {damage} | PvP {pvp} | Deathtext {deathtext}");
#endif
						break;
					case PacketTypes.TogglePvp:
						playerid = reader.ReadByte();
						bool enabled = reader.ReadBoolean();

						onPvPToggle?.Invoke(this, new PvPEventArgs() { playerID = playerid, enabled = enabled });

#if DEBUG
						File.AppendAllText("debug.txt", $"TogglePvP: PlayerID {playerid} | Enabled {enabled}");
#endif
						break;
				}
			}
		}

		private void onLeave(LeaveEventArgs args)
		{
			if (TShock.Players[args.Who] != null && TShock.Players[args.Who].Active)
				onPlayerLeave?.Invoke(this, args);
		}
		#endregion

		#region Commands
		private void DuelCMD(CommandArgs args)
		{
			/*
			 * /duel <player name>
			 * /duel accept/decline
			 * /duel quit
			 * /duel stop
			 */

			if (DuelEvent.disabled)
			{
				args.Player.SendErrorMessage("Dueling is currently disabled.");
				return;
			}

			if (args.Parameters.Count == 0 || args.Parameters[0].ToLower() == "help")
			{
				args.Player.SendErrorMessage("Invalid syntax:");
				args.Player.SendErrorMessage("/duel <player name>");
				args.Player.SendErrorMessage("/duel accept/decline");
				args.Player.SendErrorMessage("/duel quit");
				args.Player.SendErrorMessage("/duel stop");
				return;
			}
			switch (args.Parameters[0].ToLower())
			{
				case "accept":
					var result = from Tuple<int, int> t in duelChallenges where t.Item2 == args.Player.Index select t;
					if (result.Count() == 0)
					{
						args.Player.SendErrorMessage("You do not have any pending challenges!");
					}
					else
					{
						int p1 = result.First().Item1;
						duelChallenges.Clear();
						DuelEvent.StartPreDuel(TShock.Players[p1], args.Player);
					}
					break;
				case "decline":
					result = from Tuple<int, int> t in duelChallenges where t.Item2 == args.Player.Index select t;
					if (result.Count() == 0)
						args.Player.SendErrorMessage("You do not have any pending challenges!");
					else
					{
						int p1 = result.First().Item1;
						args.Player.SendSuccessMessage($"You have declined the challenge from {TShock.Players[p1].Name}");
					}
					break;
				case "quit":
					if (DuelEvent.state == DuelState.inactive)
						args.Player.SendErrorMessage("You are not in a duel!");
					else if (DuelEvent.player1.tsplayer.Index != args.Player.Index && DuelEvent.player2.tsplayer.Index != args.Player.Index)
						args.Player.SendErrorMessage("You are not in a duel!");
					else
						DuelEvent.endMatch(Endings.playerQuit, args.Player.Name);
					break;
				case "stop":
					if (!args.Player.HasPermission("duel.mod"))
						args.Player.SendErrorMessage("You do not have access to this command.");
					else if (DuelEvent.state == DuelState.inactive)
						args.Player.SendErrorMessage("There is no ongoing duel!");
					else
					{
						args.Player.SendSuccessMessage("You have ended the duel.");
						DuelEvent.endMatch(Endings.forceEnd, args.Player.Name);
					}
					break;
				default:
					if (DuelEvent.state != DuelState.inactive)
					{
						args.Player.SendErrorMessage("There is already an ongoing duel!");
						return;
					}
					string playername = string.Join(" ", args.Parameters);

					var plist = TShock.Utils.FindPlayer(playername);

					if (plist.Count == 0)
					{
						args.Player.SendErrorMessage("No players found by that name.");
						return;
					}
					else if (plist.Count > 1)
					{
						TShock.Utils.SendMultipleMatchError(args.Player, plist.Select(p => p.Name));
						return;
					}
					else if (plist[0].Index == args.Player.Index)
					{
						args.Player.SendErrorMessage("You cannot duel yourself!");
						return;
					}

					result = from Tuple<int, int> t in duelChallenges where t.Item2 == args.Player.Index && t.Item1 == plist[0].Index select t;

					if (result.Count() == 1)
					{
						duelChallenges.Clear();
						DuelEvent.StartPreDuel(TShock.Players[result.First().Item1], args.Player);
						return;
					}

					args.Player.SendSuccessMessage($"{plist[0].Name} has been sent a duel challenge!");
					plist[0].SendInfoMessage($"{args.Player.Name} has challenged you to a duel! Use /duel accept to duel, or /duel decline to reject the challenge.");
					duelChallenges.RemoveAll(p => p.Item1 == args.Player.Index);
					duelChallenges.Add(new Tuple<int, int>(args.Player.Index, plist[0].Index));
					break;
			}
		}

		private void EventReload(CommandArgs args)
		{
			pvpConfig = PvPConfig.Read();
			DuelEvent.Create();

			args.Player.SendSuccessMessage("PvP Events reloaded.");
		}
		#endregion

		#region Misc Stuff
		public class PlayerDeathEventArgs : EventArgs
		{
			public byte playerID;
			public byte hitDirection;
			public short damage;
			public bool pvp;
			public string deathText;
		}

		public class PvPEventArgs : EventArgs
		{
			public byte playerID;
			public bool enabled;
		}
		#endregion
	}
}
