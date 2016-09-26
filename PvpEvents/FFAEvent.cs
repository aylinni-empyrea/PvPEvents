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
using static PvpEvents.PvPMain;

namespace PvpEvents
{
	public static class FFAEvent
	{
		private static string arenaname;
		private static Region arena;
		public static bool disabled = true;
		private static List<FPlayer> players = new List<FPlayer>();
		private static Timer gameTimer = new Timer(1000);
		private static int count = 0;
		public static FFAState state = FFAState.inactive;
		private static Random rand;

		static FFAEvent()
		{
			gameTimer.Elapsed += onGameTimerElapsed;
			gameTimer.AutoReset = true;
			rand = new Random();
			PvPMain.onPlayerDeath += onPlayerDeath;
			PvPMain.onPlayerLeave += onPlayerLeave;
			PvPMain.onPvPToggle += onPvPToggle;
		}

		public static void Create()
		{
			arenaname = pvpConfig.ffaArenaName;
			var region = TShock.Regions.GetRegionByName(arenaname);

			if (region == null)
				TShock.Log.ConsoleError("No region found for FFA.");
			else
			{
				disabled = false;
				arena = region;
			}
		}

		public static void StartPreGame(List<TSPlayer> playerList)
		{
			players.Clear();
			foreach (TSPlayer plr in playerList)
			{
				players.Add(new FPlayer(plr));
			}
			TSPlayer.All.SendInfoMessage($"A new FFA event is starting soon! The participants are {string.Join(", ", players.Select(p => p.tsplayer.Name))}");
			count = 0;
			state = FFAState.pregame;
			gameTimer.Start();

			List<Point> points = pvpConfig.ffaArenaSpawnPointsList.Select(p => p).ToList();

			for (int i = 0; i < players.Count; i++)
			{
				if (points.Count == 0)
					points = pvpConfig.ffaArenaSpawnPointsList.Select(p => p).ToList();

				int pindex = rand.Next(points.Count);
				players[i].spawnpoint = points[pindex];
				points.RemoveAt(pindex);

				if (players[i].tsplayer.TPlayer.hostile)
				{
					players[i].tsplayer.TPlayer.hostile = false;
					NetMessage.SendData((int)PacketTypes.TogglePvp, -1, -1, "", players[i].tsplayer.Index, 0);
				}
			}
		}

		private static void onGameTimerElapsed(object sender, ElapsedEventArgs args)
		{
			switch (state)
			{
				case FFAState.pregame:
					if (count < 10)
					{
						count++;
						foreach(FPlayer plr in players)
						{
							if ((int)plr.tsplayer.X / 16 != plr.spawnpoint.X || (int)plr.tsplayer.Y / 16 != plr.spawnpoint.Y)
								plr.tsplayer.Teleport(plr.spawnpoint.X * 16, plr.spawnpoint.Y * 16);
						}
						
						if (count == 0 || count == 5 || count == 8)
							broadcast($"The match will begin in {10 - count} seconds!");
					}
					else
					{
						TSPlayer.All.SendInfoMessage("The FFA event has started!");

						state = FFAState.active;
						count = 0;

						foreach(FPlayer plr in players)
						{
							plr.tsplayer.TPlayer.hostile = true;
							NetMessage.SendData((int)PacketTypes.TogglePvp, -1, -1, "", plr.tsplayer.Index, 1);
							plr.tsplayer.Heal();
						}
					}
					break;
				case FFAState.active:
					if (count < 60)
					{
						count++;
						foreach (FPlayer plr in players)
						{
							if (plr.tsplayer.CurrentRegion?.Name != arena.Name && !plr.isDead)
								endMatch(Endings.outofbounds, plr.tsplayer.Name);
						}
					}
					else
					{
						endMatch(Endings.time);
						count = 0;
					}
					break;
				case FFAState.cooldown:
					if (count < 10)
					{
						count++;
						foreach (FPlayer plr in players)
						{
							if ((int)plr.tsplayer.X / 16 != plr.spawnpoint.X || (int)plr.tsplayer.Y / 16 != plr.spawnpoint.Y)
								plr.tsplayer.Teleport(plr.spawnpoint.X * 16, plr.spawnpoint.Y * 16);
						}

						if (count == 0 || count == 5 || count == 8)
							broadcast($"The match will begin in {10 - count} seconds!");
					}
					else
					{
						broadcast("FIGHT!");

						state = FFAState.active;
						count = 0;

						foreach (FPlayer plr in players)
						{
							plr.tsplayer.TPlayer.hostile = true;
							NetMessage.SendData((int)PacketTypes.TogglePvp, -1, -1, "", plr.tsplayer.Index, 1);
							plr.tsplayer.Heal();
						}
					}
					break;
			}
		}

		private static void onPlayerDeath(object sender, PlayerDeathEventArgs args)
		{
			if (!containsPlayer(args.playerID))
				return;

			if (state != FFAState.active)
				return;

			if (!args.pvp)
			{
				endMatch(Endings.nonpvpdeath, TShock.Players[args.playerID].Name);
				return;
			}

			int index = players.FindIndex(p => p.tsplayer.Index == args.playerID);

			players[index].isDead = true;

			int playersRemaining = players.Count(p => !p.isDead);

			if (playersRemaining == 1)
			{
				int index2 = players.FindIndex(p => !p.isDead);
				players[index2].matchesWon++;
				broadcast($"{players[index2].tsplayer.Name} has won the round!");
				if (players[index2].matchesWon == pvpConfig.ffaMatchesNeededToWin)
				{
					endMatch(Endings.normal, players[index2].tsplayer.Name);
					return;
				}
				state = FFAState.cooldown;
				foreach (FPlayer plr in players)
				{
					plr.isDead = false;
					plr.tsplayer.TPlayer.hostile = false;
					NetMessage.SendData((int)PacketTypes.TogglePvp, -1, -1, "", plr.tsplayer.Index, 0);
				}
				count = 0;
				return;
			}

			players[index].tsplayer.TPlayer.hostile = false;
			NetMessage.SendData((int)PacketTypes.TogglePvp, -1, -1, "", players[index].tsplayer.Index, 0);
		}

		public static void onPlayerLeave(object sender, LeaveEventArgs args)
		{
			if (!containsPlayer(args.Who))
				return;

			if (state == FFAState.inactive)
				return;

			if ((state == FFAState.pregame && players.Count > 3) || (state == FFAState.active && players.Count > 2) || (state == FFAState.cooldown && players.Count > 2))
			{
				broadcast($"{TShock.Players[args.Who].Name} has left the match.");
				players.RemoveAll(p => p.tsplayer.Index == args.Who);

				if (state == FFAState.active)
				{
					if (players.Count(p => !p.isDead) == 1)
					{
						int index2 = players.FindIndex(p => !p.isDead);
						players[index2].matchesWon++;
						broadcast($"{players[index2].tsplayer.Name} has won the round!");
						if (players[index2].matchesWon == pvpConfig.ffaMatchesNeededToWin)
						{
							endMatch(Endings.normal, players[index2].tsplayer.Name);
							return;
						}
						state = FFAState.cooldown;
						foreach (FPlayer plr in players)
						{
							plr.isDead = false;
							plr.tsplayer.TPlayer.hostile = false;
							NetMessage.SendData((int)PacketTypes.TogglePvp, -1, -1, "", plr.tsplayer.Index, 0);
						}
						count = 0;
						return;
					}
				}
			}
			else if (state != FFAState.inactive)
				endMatch(Endings.playerLeave);
		}

		private static void onPvPToggle(object sender, PvPEventArgs args)
		{
			if (!containsPlayer(args.playerID))
				return;

			switch (state)
			{
				case FFAState.inactive:
					return;
				case FFAState.pregame:
					if (args.enabled)
					{
						TShock.Players[args.playerID].SendWarningMessage("You cannot enable PvP until the match starts!");
						TShock.Players[args.playerID].TPlayer.hostile = false;
						NetMessage.SendData((int)PacketTypes.TogglePvp, -1, -1, "", args.playerID, 0);
					}
					break;
				case FFAState.active:
					if (!args.enabled)
					{
						int index = players.FindIndex(p => p.tsplayer.Index == args.playerID);
						if (!players[index].isDead)
						{
							TShock.Players[args.playerID].SendWarningMessage("You cannot disable PvP during FFA!");
							TShock.Players[args.playerID].TPlayer.hostile = true;
							NetMessage.SendData((int)PacketTypes.TogglePvp, -1, -1, "", args.playerID, 1);
						}
					}
					else if (args.enabled)
					{
						int index = players.FindIndex(p => p.tsplayer.Index == args.playerID);
						if (players[index].isDead)
						{
							TShock.Players[args.playerID].SendWarningMessage("You cannot continue fighting until someone has won the round!");
							TShock.Players[args.playerID].TPlayer.hostile = false;
							NetMessage.SendData((int)PacketTypes.TogglePvp, -1, -1, "", args.playerID, 0);
						}
					}
					break;
				case FFAState.cooldown:
					if (args.enabled)
					{
						TShock.Players[args.playerID].SendWarningMessage("You cannot enable PvP until the match starts!");
						TShock.Players[args.playerID].TPlayer.hostile = false;
						NetMessage.SendData((int)PacketTypes.TogglePvp, -1, -1, "", args.playerID, 0);
					}
					break;
			}
		}

		public static void endMatch(Endings ending, string name = "")
		{
			gameTimer.Stop();
			state = FFAState.inactive;

			switch (ending)
			{
				case Endings.forceEnd:
					TSPlayer.All.SendInfoMessage($"{name} has force-stopped the FFA event.");
					break;
				case Endings.nonpvpdeath:
					TSPlayer.All.SendInfoMessage("The FFA event has ended.");
					broadcast($"{name} has been killed by outside influences.");
					break;
				case Endings.normal:
					TSPlayer.All.SendInfoMessage($"{name} has won the FFA event!");
					break;
				case Endings.outofbounds:
					TSPlayer.All.SendInfoMessage($"The FFA event has ended.");
					broadcast($"{name} has left the arena.");
					break;
				case Endings.playerLeave:
					TSPlayer.All.SendInfoMessage($"The FFA event has ended.");
					broadcast("Not enough players to continue.");
					break;
				case Endings.playerQuit:
					TSPlayer.All.SendInfoMessage($"The FFA event has ended.");
					broadcast("Not enough players to continue.");
					break;
				case Endings.time:
					TSPlayer.All.SendInfoMessage($"The FFA event has ended.");
					broadcast("Time has ran out.");
					break;
			}
		}

		private static void broadcast(string msg)
		{
			foreach (FPlayer plr in players)
			{
				plr.tsplayer.SendInfoMessage($"[FFA] {msg}");
			}
		}

		public static bool containsPlayer(int id)
		{
			if (players.Exists(p => p != null && p.tsplayer.Index == id))
				return true;
			else
				return false;
		}
	}

	public class FPlayer
	{
		public TSPlayer tsplayer;
		public int matchesWon;
		public Point spawnpoint;
		public bool isDead;

		public FPlayer(TSPlayer _p)
		{
			tsplayer = _p;
			matchesWon = 0;
			isDead = false;
		}
	}

	public enum FFAState
	{
		inactive,
		pregame,
		active,
		cooldown
	}
}
