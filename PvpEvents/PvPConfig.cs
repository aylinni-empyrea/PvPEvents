using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using TShockAPI;

namespace PvpEvents
{
	public class PvPConfig
	{
		public string duelArenaName;
		public Point duelArenaSpawn1;
		public Point duelArenaSpawn2;
		public int duelMatchesNeededToWin;
		public string ffaArenaName;
		public List<Point> ffaArenaSpawnPointsList;
		public int ffaMatchesNeededToWin;
		public int ffaSignupTime;

		public PvPConfig()
		{
			duelArenaName = "duel";
			duelArenaSpawn1 = new Point(0, 0);
			duelArenaSpawn2 = new Point(0, 0);
			duelMatchesNeededToWin = 3;
			ffaArenaName = "ffa";
			ffaArenaSpawnPointsList = new List<Point>() { new Point(0, 0), new Point(0, 0) };
			ffaMatchesNeededToWin = 5;
			ffaSignupTime = 30;
		}

		public static PvPConfig Read()
		{
			if (!File.Exists(Path.Combine(TShock.SavePath, "pvpconfig.json")))
			{
				TShock.Log.ConsoleInfo("PvPConfig.json not found. Creating new one...");
				PvPConfig pvpc = new PvPConfig();
				File.WriteAllText(Path.Combine(TShock.SavePath, "pvpconfig.json"), JsonConvert.SerializeObject(pvpc, Formatting.Indented));
				return pvpc;
			}

			try
			{
				string raw = File.ReadAllText(Path.Combine(TShock.SavePath, "pvpconfig.json"));
				PvPConfig pvpc = JsonConvert.DeserializeObject<PvPConfig>(raw);
				pvpc.ffaArenaSpawnPointsList.RemoveAll(p => p.X == 0 && p.Y == 0);
				return pvpc;
			}
			catch
			{
				TShock.Log.ConsoleError("PvPConfig.json not valid. Creating new one...");
				PvPConfig pvpc = new PvPConfig();
				File.WriteAllText(Path.Combine(TShock.SavePath, "pvpconfig.json"), JsonConvert.SerializeObject(pvpc, Formatting.Indented));
				return pvpc;
			}
		}
	}
}
