using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using TerrariaApi.Server;
using TShockAPI;

namespace PvpEvents
{
	public class DuelMode : GameMode
	{
		public override string Name { get { return "Duel"; } }
		public override GameState State { get; set; }
		public override Command Command { get { return _cmd; } }
		protected override List<TSPlayer> PlayerList { get; set; }
		protected override Arena LoadedArena { get; set; }
		protected override GameFlags LoadedFlags { get; set; }
		protected override int GameTicks { get; set; }
		protected override Timer GameTimer { get { return _gameTimer; } }

		private static Command _cmd = new Command("pvpevents.duel", DuelCMD, "duel");
		private static Timer _gameTimer = new Timer(1000);
		private static int p1 = -1;
		private static int p2 = -1;

		public override void Create()
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

		public static void DuelCMD(CommandArgs args)
		{
			string flag;
			if (args.Parameters.Count > 0)
				flag = args.Parameters[0].ToLower();
			else
				flag = "help";

			switch(flag)
			{
				//duel <player>
				//duel accept
				//duel decline
				//duel stop
				//duel quit
				case "help":
					if (State == GameState.inactive)
					{

					}
					break;
			}
		}

		public override bool ContainsPlayer(int index)
		{
			return PlayerList.Exists(p => p.Index == index);
		}

		public override void EndMatch(GameEnding ending, string player)
		{
			throw new NotImplementedException();
		}

		protected override void Broadcast(string msg)
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
