using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;

namespace PvpEvents
{
	public static class DuelEvent
	{
		public static string duelArenaName;
		public static Region duelArena;
		public static bool disabled = false;
		public static DPlayer player1 = null;
		public static DPlayer player2 = null;
		public static Timer gameTimer = new Timer();
		public static int count = 0;
		public static DuelState state = DuelState.inactive;

		static DuelEvent()
		{
			gameTimer.Elapsed += onGameTimerElapsed;
			PvPMain.onPvPToggle += onPvPToggle;
			PvPMain.onPlayerDeath += onPlayerDeath;
			PvPMain.onPlayerLeave += onPlayerLeave;
		}

		public static void Create()
		{
			duelArenaName = PvPMain.pvpConfig.duelArenaName;
			duelArena = TShock.Regions.GetRegionByName(duelArenaName);
			count = 0;

			if (duelArena == null)
			{
				TShock.Log.Warn("[Duel Event] No region found by the name: " + duelArenaName);
				disabled = true;
			}
			else
			{
				TShock.Log.Debug("[Duel Event] Arena found.");
				disabled = false;
			}

			gameTimer.Interval = 1000;
			gameTimer.AutoReset = true;
		}

		public static void StartPreDuel(TSPlayer p1, TSPlayer p2)
		{
			player1 = new DPlayer(p1);
			player2 = new DPlayer(p2);

			player1.tsplayer.Teleport(PvPMain.pvpConfig.duelArenaSpawn1.X * 16, PvPMain.pvpConfig.duelArenaSpawn1.Y * 16);
			player2.tsplayer.Teleport(PvPMain.pvpConfig.duelArenaSpawn2.X * 16, PvPMain.pvpConfig.duelArenaSpawn2.Y * 16);

			TSPlayer.All.SendInfoMessage($"A duel between {player1.tsplayer.Name} and {player2.tsplayer.Name} will begin in 10 seconds!");

			player1.tsplayer.TPlayer.hostile = false;
			player2.tsplayer.TPlayer.hostile = false;
			NetMessage.SendData((int)PacketTypes.TogglePvp, -1, -1, "", player1.tsplayer.Index, 0);
			NetMessage.SendData((int)PacketTypes.TogglePvp, -1, -1, "", player2.tsplayer.Index, 0);

			gameTimer.Start();
			state = DuelState.pregame;
		}

		public static void onGameTimerElapsed(object sender, ElapsedEventArgs args)
		{
			//Force players to stay in arena spawn
			if (state == DuelState.pregame && count < 10)
			{
				count++;
				if ((int)player1.tsplayer.X / 16 != PvPMain.pvpConfig.duelArenaSpawn1.X || (int)player1.tsplayer.Y / 16 != PvPMain.pvpConfig.duelArenaSpawn1.Y)
					player1.tsplayer.Teleport(PvPMain.pvpConfig.duelArenaSpawn1.X * 16, PvPMain.pvpConfig.duelArenaSpawn1.Y * 16);
				if ((int)player2.tsplayer.X / 16 != PvPMain.pvpConfig.duelArenaSpawn2.X || (int)player2.tsplayer.Y / 16 != PvPMain.pvpConfig.duelArenaSpawn2.Y)
					player2.tsplayer.Teleport(PvPMain.pvpConfig.duelArenaSpawn2.X * 16, PvPMain.pvpConfig.duelArenaSpawn2.Y * 16);

				TSPlayer.Server.SendInfoMessage("player1.tsplayer.X: " + player1.tsplayer.X + " | PvPMain.pvpConfig.duelArenaSpawn1.X " + PvPMain.pvpConfig.duelArenaSpawn1.X + " | PvPMain.pvpConfig.duelArenaSpawn1.X * 16 " + PvPMain.pvpConfig.duelArenaSpawn1.X);

				if (count == 0 || count == 5 || count == 8)
				broadcast($"The duel will begin in {10 - count} seconds!");
			}
			//After 10 seconds, start duel.
			else if (state == DuelState.pregame)
			{
				TSPlayer.All.SendInfoMessage($"The duel between {player1.tsplayer.Name} and {player2.tsplayer.Name} is starting now!");
				player1.tsplayer.TPlayer.hostile = true;
				player2.tsplayer.TPlayer.hostile = true;
				NetMessage.SendData((int)PacketTypes.TogglePvp, -1, -1, "", player1.tsplayer.Index, 1);
				NetMessage.SendData((int)PacketTypes.TogglePvp, -1, -1, "", player2.tsplayer.Index, 1);
				player1.tsplayer.TPlayer.statLife = player1.tsplayer.TPlayer.statLifeMax;
				player2.tsplayer.TPlayer.statLife = player2.tsplayer.TPlayer.statLifeMax;
				NetMessage.SendData((int)PacketTypes.PlayerHp, -1, -1, "", player1.tsplayer.Index, player1.tsplayer.TPlayer.statLife, player1.tsplayer.TPlayer.statLifeMax);
				NetMessage.SendData((int)PacketTypes.PlayerHp, -1, -1, "", player2.tsplayer.Index, player2.tsplayer.TPlayer.statLife, player2.tsplayer.TPlayer.statLifeMax);
				state = DuelState.active;
				count = 0;
			}
			//Check if players are in duel arena
			else if (state == DuelState.active && count < 60)
			{
				count++;
				if (player1.tsplayer.CurrentRegion?.Name != duelArena.Name)
					endMatch(Endings.outofbounds, player1.tsplayer.Name);
				else if (player2.tsplayer.CurrentRegion?.Name != duelArena.Name)
					endMatch(Endings.outofbounds, player2.tsplayer.Name);
			}
			//Duel timed out
			else if (state == DuelState.active)
			{
				endMatch(Endings.time);
			}
			//If in-between rounds, force players to arena spawn
			else if (state == DuelState.cooldown && count < 10)
			{
				count++;
				if ((int)player1.tsplayer.X / 16 != PvPMain.pvpConfig.duelArenaSpawn1.X || (int)player1.tsplayer.Y / 16 != PvPMain.pvpConfig.duelArenaSpawn1.Y)
					player1.tsplayer.Teleport(PvPMain.pvpConfig.duelArenaSpawn1.X * 16, PvPMain.pvpConfig.duelArenaSpawn1.Y * 16);
				if ((int)player2.tsplayer.X / 16 != PvPMain.pvpConfig.duelArenaSpawn2.X || (int)player2.tsplayer.Y / 16 != PvPMain.pvpConfig.duelArenaSpawn2.Y)
					player2.tsplayer.Teleport(PvPMain.pvpConfig.duelArenaSpawn2.X * 16, PvPMain.pvpConfig.duelArenaSpawn2.Y * 16);

				if (count == 0 || count == 5 || count == 8)
					broadcast($"The next round will begin in {10 - count} seconds!");
			}
			//When next round starts
			else if (state == DuelState.cooldown)
			{
				broadcast($"FIGHT!");
				player1.tsplayer.TPlayer.hostile = true;
				player2.tsplayer.TPlayer.hostile = true;
				NetMessage.SendData((int)PacketTypes.TogglePvp, -1, -1, "", player1.tsplayer.Index, 1);
				NetMessage.SendData((int)PacketTypes.TogglePvp, -1, -1, "", player2.tsplayer.Index, 1);
				player1.tsplayer.TPlayer.statLife = player1.tsplayer.TPlayer.statLifeMax;
				player2.tsplayer.TPlayer.statLife = player2.tsplayer.TPlayer.statLifeMax;
				NetMessage.SendData((int)PacketTypes.PlayerHp, -1, -1, "", player1.tsplayer.Index, player1.tsplayer.TPlayer.statLife, player1.tsplayer.TPlayer.statLifeMax);
				NetMessage.SendData((int)PacketTypes.PlayerHp, -1, -1, "", player2.tsplayer.Index, player2.tsplayer.TPlayer.statLife, player2.tsplayer.TPlayer.statLifeMax);
				state = DuelState.active;
				count = 0;
			}
		}

		public static void endMatch(Endings end, string player = "")
		{
			gameTimer.Stop();
			state = DuelState.inactive;

			switch (end)
			{
				case Endings.normal:
					string winner = player1.matchesWon > player2.matchesWon ? player1.tsplayer.Name : player2.tsplayer.Name;
					string loser = player1.matchesWon < player2.matchesWon ? player1.tsplayer.Name : player2.tsplayer.Name;
					TSPlayer.All.SendInfoMessage($"{winner} has defeated {loser} in a duel!");
					break;
				case Endings.outofbounds:
					broadcast($"{player} has left the duel arena. The duel has been cancelled.");
					TSPlayer.All.SendInfoMessage($"The duel between {player1.tsplayer.Name} and {player2.tsplayer.Name} has ended.");
					break;
				case Endings.time:
					broadcast($"Time has ran out. The duel has been cancelled.");
					TSPlayer.All.SendInfoMessage($"The duel between {player1.tsplayer.Name} and {player2.tsplayer.Name} has ended.");
					break;
				case Endings.nonpvpdeath:
					broadcast($"{player} has been killed by outside influences. The duel has been cancelled.");
					TSPlayer.All.SendInfoMessage($"The duel between {player1.tsplayer.Name} and {player2.tsplayer.Name} has ended.");
					break;
				case Endings.playerLeave:
					TSPlayer.All.SendInfoMessage($"The duel between {player1.tsplayer.Name} and {player2.tsplayer.Name} has ended.");
					break;
				case Endings.playerQuit:
					broadcast($"{player} has quit the duel.");
					TSPlayer.All.SendInfoMessage($"The duel between {player1.tsplayer.Name} and {player2.tsplayer.Name} has ended.");
					break;
				case Endings.forceEnd:
					broadcast($"{player} has force-stopped this duel.");
					TSPlayer.All.SendInfoMessage($"The duel between {player1.tsplayer.Name} and {player2.tsplayer.Name} has ended.");
					break;
			}
		}

		public static void onPvPToggle(object sender, PvPMain.PvPEventArgs args)
		{
			if (state == DuelState.inactive)
				return;

			if ((state == DuelState.pregame || state == DuelState.cooldown) && args.playerID == player1.tsplayer.Index && args.enabled)
			{
				player1.tsplayer.TPlayer.hostile = false;
				player1.tsplayer.SendData(PacketTypes.TogglePvp, "", args.playerID, 0);
				player1.tsplayer.SendWarningMessage("You cannot enable PvP until the duel begins!");
			}
			else if ((state == DuelState.pregame || state == DuelState.cooldown) && args.playerID == player2.tsplayer.Index && args.enabled)
			{
				player2.tsplayer.TPlayer.hostile = false;
				player2.tsplayer.SendData(PacketTypes.TogglePvp, "", args.playerID, 0);
				player2.tsplayer.SendWarningMessage("You cannot enable PvP until the duel begins!");
			}
			else if (state == DuelState.active && args.playerID == player1.tsplayer.Index && !args.enabled)
			{
				player1.tsplayer.TPlayer.hostile = true;
				player1.tsplayer.SendData(PacketTypes.TogglePvp, "", args.playerID, 1);
				player1.tsplayer.SendWarningMessage("You cannot disable PvP while in a duel!");
			}
			else if (state == DuelState.active && args.playerID == player2.tsplayer.Index && !args.enabled)
			{
				player2.tsplayer.TPlayer.hostile = true;
				player2.tsplayer.SendData(PacketTypes.TogglePvp, "", args.playerID, 1);
				player2.tsplayer.SendWarningMessage("You cannot disable PvP while in a duel!");
			}
		}

		public static void onPlayerDeath(object sender, PvPMain.PlayerDeathEventArgs args)
		{
			if (state != DuelState.active)
				return;

			if (player1.tsplayer.Index != args.playerID && player2.tsplayer.Index != args.playerID)
				return;

			if (!args.pvp)
			{
				endMatch(Endings.nonpvpdeath, TShock.Players[args.playerID].Name);
				return;
			}

			if (player1.tsplayer.Index == args.playerID)
			{
				player2.matchesWon++;
				if (player2.matchesWon == PvPMain.pvpConfig.duelMatchesNeededToWin)
				{
					endMatch(Endings.normal);
				}
				else
				{
					state = DuelState.cooldown;
					broadcast($"{player2.tsplayer.Name} now has {player2.matchesWon} win(s)! The next round will begin in 10 seconds.");
					count = 0;
					player1.tsplayer.TPlayer.hostile = false;
					player2.tsplayer.TPlayer.hostile = false;
					NetMessage.SendData((int)PacketTypes.TogglePvp, -1, -1, "", player1.tsplayer.Index, 0);
					NetMessage.SendData((int)PacketTypes.TogglePvp, -1, -1, "", player2.tsplayer.Index, 0);
				}
			}
			else
			{
				player1.matchesWon++;
				if (player1.matchesWon == PvPMain.pvpConfig.duelMatchesNeededToWin)
				{
					endMatch(Endings.normal);
				}
				else
				{
					state = DuelState.cooldown;
					broadcast($"{player1.tsplayer.Name} now has {player1.matchesWon} win(s)! The next round will begin in 10 seconds.");
					count = 0;
					player1.tsplayer.TPlayer.hostile = false;
					player2.tsplayer.TPlayer.hostile = false;
					NetMessage.SendData((int)PacketTypes.TogglePvp, -1, -1, "", player1.tsplayer.Index, 0);
					NetMessage.SendData((int)PacketTypes.TogglePvp, -1, -1, "", player2.tsplayer.Index, 0);
				}
			}
		}

		public static void onPlayerLeave(object sender, LeaveEventArgs args)
		{
			if (state == DuelState.inactive)
				return;

			if (args.Who != player1.tsplayer.Index && args.Who != player2.tsplayer.Index)
				return;

			endMatch(Endings.playerLeave, TShock.Players[args.Who].Name);
		}

		public static void broadcast(string msg)
		{
			player1.tsplayer.SendInfoMessage($"[Duel] {msg}");
			player2.tsplayer.SendInfoMessage($"[Duel] {msg}");
		}
	}

	public class DPlayer
	{
		public TSPlayer tsplayer;
		public int matchesWon;

		public DPlayer(TSPlayer _player)
		{
			tsplayer = _player;
			matchesWon = 0;
		}
	}

	public enum DuelState
	{
		inactive,
		pregame,
		active,
		cooldown
	}

	public enum Endings
	{
		normal,
		time,
		outofbounds,
		nonpvpdeath,
		playerLeave,
		playerQuit,
		forceEnd
	}
}
