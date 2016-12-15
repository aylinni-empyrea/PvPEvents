using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using TerrariaApi.Server;
using TShockAPI;

namespace PvpEvents
{
	public class DuelMode : IGameMode
	{
		public string Name { get { return "Duel"; } }
		public GameState State { get; set; }
		public List<DPlayer> PlayerList { get; set; }
		public Arena LoadedArena { get; set; }
		public GameFlags LoadedFlags { get; set; }
		public int GameTicks { get; set; }
		public Timer GameTimer { get { return _gameTimer; } }
		
		private static Timer _gameTimer = new Timer(1000);
		private static int p1 = -1;
		private static int p2 = -1;

		public void Create()
		{
			State = GameState.inactive;
			PlayerList = new List<DPlayer>();
			LoadedArena = null;
			LoadedFlags = GameFlags.None;
			GameTicks = 0;
			GameTimer.AutoReset = true;
			GameTimer.Elapsed += onUpdate;
			PvPMain.onPlayerLeave += onPlayerLeave;
			PvPMain.onPlayerDeath += onPlayerDeath;
			PvPMain.onPvPToggle += onPvPToggle;
		}

		public bool ContainsPlayer(int index)
		{
			return PlayerList.Exists(p => p.P.Index == index);
		}

		public void EndMatch(GameEnding ending, int player)
        {
            GameTimer.Stop();
            State = GameState.inactive;

            switch (ending)
            {
                case GameEnding.Normal:
                    
                    string winner = PlayerList[0].MatchesWon > PlayerList[1].MatchesWon ? PlayerList[0].P.Name : PlayerList[1].P.Name;
                    string loser = PlayerList[0].MatchesWon > PlayerList[1].MatchesWon ? PlayerList[1].P.Name : PlayerList[0].P.Name;

                    TSPlayer.All.SendInfoMessage($"{winner} has defeated {loser} in a duel!");
                    break;
                case GameEnding.TimeUp:
                    Broadcast($"Time has ran out. The duel has been cancelled.");
                    TSPlayer.All.SendInfoMessage($"The duel between {PlayerList[0].P.Name} and {PlayerList[1].P.Name} has ended.");
                    break;
                case GameEnding.OutOfBounds:
                    Broadcast($"{player} has left the duel arena. The duel has been cancelled.");
                    TSPlayer.All.SendInfoMessage($"The duel between {PlayerList[0].P.Name} and {PlayerList[1].P.Name} has ended.");
                    break;
                case GameEnding.Interference:
                    Broadcast($"Time has ran out. The duel has been cancelled.");
                    TSPlayer.All.SendInfoMessage($"The duel between {PlayerList[0].P.Name} and {PlayerList[1].P.Name} has ended.");
                    break;
                case GameEnding.PlayerLeave:
                    TSPlayer.All.SendInfoMessage($"The duel between {PlayerList[0].P.Name} and {PlayerList[1].P.Name} has ended.");
                    break;
                case GameEnding.PlayerQuit:
                    Broadcast($"{player} has quit the duel.");
                    TSPlayer.All.SendInfoMessage($"The duel between {PlayerList[0].P.Name} and {PlayerList[1].P.Name} has ended.");
                    break;
                case GameEnding.ForceEnd:
                    Broadcast($"{player} has force-stopped this duel.");
                    TSPlayer.All.SendInfoMessage($"The duel between {PlayerList[0].P.Name} and {PlayerList[1].P.Name} has ended.");
                    break;
            }

            // cleanup
            PvPMain.onPlayerLeave -= onPlayerLeave;
            PvPMain.onPlayerDeath -= onPlayerDeath;
            PvPMain.onPvPToggle -= onPvPToggle;
        }

		public void Broadcast(string msg)
		{
			foreach (DPlayer plr in PlayerList)
			{
				plr.P.SendInfoMessage($"[Duel] {msg}");
			}
		}

		private void onUpdate(object sender, ElapsedEventArgs args)
        {
            //Force players to stay in arena spawn
            if (State == GameState.pregame && GameTicks == 0)
            {
                foreach(DPlayer plr in PlayerList)
                {
                    Random rnd = new Random();
                    // get random spawn TODO: make sure not 2 same spawns are used
                    plr.ArenaSpawn = LoadedArena.SpawnPoints[rnd.Next(LoadedArena.SpawnPoints.Count)];
                }
                GameTicks++;
            }
            else if (State == GameState.pregame && GameTicks < 10)
            {
                GameTicks++;
                if ((int)player1.tsplayer.X / 16 != PvPMain.pvpConfig.duelArenaSpawn1.X || (int)player1.tsplayer.Y / 16 != PvPMain.pvpConfig.duelArenaSpawn1.Y)
                    player1.tsplayer.Teleport(PvPMain.pvpConfig.duelArenaSpawn1.X * 16, PvPMain.pvpConfig.duelArenaSpawn1.Y * 16);
                if ((int)player2.tsplayer.X / 16 != PvPMain.pvpConfig.duelArenaSpawn2.X || (int)player2.tsplayer.Y / 16 != PvPMain.pvpConfig.duelArenaSpawn2.Y)
                    player2.tsplayer.Teleport(PvPMain.pvpConfig.duelArenaSpawn2.X * 16, PvPMain.pvpConfig.duelArenaSpawn2.Y * 16);

                //TSPlayer.Server.SendInfoMessage("player1.tsplayer.X: " + player1.tsplayer.X + " | PvPMain.pvpConfig.duelArenaSpawn1.X " + PvPMain.pvpConfig.duelArenaSpawn1.X + " | PvPMain.pvpConfig.duelArenaSpawn1.X * 16 " + PvPMain.pvpConfig.duelArenaSpawn1.X);

                if (GameTicks == 0 || GameTicks == 5 || GameTicks == 8)
                    Broadcast($"The duel will begin in {10 - GameTicks} seconds!");
            }
            //After 10 seconds, start duel.
            else if (State == GameState.pregame)
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
                State = GameState.active;
                GameTicks = 0;
            }
            //Check if players are in duel arena
            else if (State == GameState.active && GameTicks < 60)
            {
                GameTicks++;
                if (player1.tsplayer.CurrentRegion?.Name != duelArena.Name)
                    EndMatch(GameEnding.OutOfBounds, player1.tsplayer.Name);
                else if (player2.tsplayer.CurrentRegion?.Name != duelArena.Name)
                    EndMatch(GameEnding.OutOfBounds, player2.tsplayer.Name);
            }
            //Duel timed out
            else if (State == GameState.active)
            {
                EndMatch(GameEnding.TimeUp, 0);
            }
            //If in-between rounds, force players to arena spawn
            else if (State == GameState.cooldown && GameTicks < 10)
            {
                GameTicks++;
                if ((int)player1.tsplayer.X / 16 != PvPMain.pvpConfig.duelArenaSpawn1.X || (int)player1.tsplayer.Y / 16 != PvPMain.pvpConfig.duelArenaSpawn1.Y)
                    player1.tsplayer.Teleport(PvPMain.pvpConfig.duelArenaSpawn1.X * 16, PvPMain.pvpConfig.duelArenaSpawn1.Y * 16);
                if ((int)player2.tsplayer.X / 16 != PvPMain.pvpConfig.duelArenaSpawn2.X || (int)player2.tsplayer.Y / 16 != PvPMain.pvpConfig.duelArenaSpawn2.Y)
                    player2.tsplayer.Teleport(PvPMain.pvpConfig.duelArenaSpawn2.X * 16, PvPMain.pvpConfig.duelArenaSpawn2.Y * 16);

                if (GameTicks == 0 || GameTicks == 5 || GameTicks == 8)
                    Broadcast($"The next round will begin in {10 - GameTicks} seconds!");
            }
            //When next round starts
            else if (State == GameState.cooldown)
            {
                Broadcast($"FIGHT!");
                player1.tsplayer.TPlayer.hostile = true;
                player2.tsplayer.TPlayer.hostile = true;
                NetMessage.SendData((int)PacketTypes.TogglePvp, -1, -1, "", player1.tsplayer.Index, 1);
                NetMessage.SendData((int)PacketTypes.TogglePvp, -1, -1, "", player2.tsplayer.Index, 1);
                player1.tsplayer.TPlayer.statLife = player1.tsplayer.TPlayer.statLifeMax;
                player2.tsplayer.TPlayer.statLife = player2.tsplayer.TPlayer.statLifeMax;
                NetMessage.SendData((int)PacketTypes.PlayerHp, -1, -1, "", player1.tsplayer.Index, player1.tsplayer.TPlayer.statLife, player1.tsplayer.TPlayer.statLifeMax);
                NetMessage.SendData((int)PacketTypes.PlayerHp, -1, -1, "", player2.tsplayer.Index, player2.tsplayer.TPlayer.statLife, player2.tsplayer.TPlayer.statLifeMax);
                State = GameState.active;
                GameTicks = 0;
            }
        }

		private void onPlayerLeave(object sender, LeaveEventArgs args)
		{
            if (State == GameState.inactive)
                return;

            if (!ContainsPlayer(args.Who))
                return;

            EndMatch(GameEnding.PlayerLeave, args.Who);
        }

		private void onPlayerDeath(object sender, PvPMain.PlayerDeathEventArgs args)
		{
			throw new NotImplementedException();
		}

		private void onPvPToggle(object sender, PvPMain.PvPEventArgs args)
		{
			throw new NotImplementedException();
		}
    }
}
