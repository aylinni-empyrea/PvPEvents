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
		public List<TSPlayer> PlayerList { get; set; }
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
			PlayerList = new List<TSPlayer>();
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
			return PlayerList.Exists(p => p.Index == index);
		}

		public void EndMatch(GameEnding ending, string player)
		{
			throw new NotImplementedException();
		}

		public void Broadcast(string msg)
		{
			foreach (TSPlayer plr in PlayerList)
			{
				plr.SendInfoMessage($"[Duel] {msg}");
			}
		}

		private void onUpdate(object sender, ElapsedEventArgs args)
		{
			throw new NotImplementedException();
		}

		private void onPlayerLeave(object sender, LeaveEventArgs args)
		{
			throw new NotImplementedException();
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
