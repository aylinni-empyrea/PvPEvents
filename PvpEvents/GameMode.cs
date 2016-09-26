using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using TShockAPI;

namespace PvpEvents
{
	public abstract class GameMode
	{
		public abstract string Name { get; }
		protected abstract List<TSPlayer> PlayerList { get; set; }
		protected abstract int GameTicks { get; set; }
		public abstract GameState State { get; set; }
		protected abstract Timer GameTimer { get; }
		protected abstract Arena LoadedArena { get; set; }
		protected abstract GameFlags LoadedFlags { get; set; }
		public abstract Command Command { get; }

		public abstract void Create();
		public abstract void EndMatch(GameEnding ending, string player);
		public abstract bool ContainsPlayer(int index);
		protected abstract void Broadcast(string message);
	}

	[Flags]
	public enum GameState
	{
		inactive = 1,
		signup = 2,
		pregame = 4,
		active = 8,
		cooldown = 16
	}

	[Flags]
	public enum GameFlags
	{
		None = 1,
		NoMelee = 2,
		NoYoyo = 4
	}

	public enum GameEnding
	{
		Normal,
		TimeUp,
		OutOfBounds,
		Interference,
		PlayerLeave,
		PlayerQuit,
		ForceEnd
	}
}
