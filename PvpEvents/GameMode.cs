using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using TShockAPI;

namespace PvpEvents
{
	public interface IGameMode
	{
		string Name { get; }
		List<DPlayer> PlayerList { get; set; }
		int GameTicks { get; set; }
		GameState State { get; set; }
		Timer GameTimer { get; }
		Arena LoadedArena { get; set; }
		GameFlags LoadedFlags { get; set; }

		void Create();
		void EndMatch(GameEnding ending, int player);
		bool ContainsPlayer(int index);
		void Broadcast(string message);
	}

    public class DPlayer
    {
        public TSPlayer P;
        public int MatchesWon;
        public Point ArenaSpawn;

        public DPlayer(TSPlayer _player)
        {
            P = _player;
            MatchesWon = 0;
            ArenaSpawn = new Point(0,0);
        }
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
