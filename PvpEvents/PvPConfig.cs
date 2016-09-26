using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using TShockAPI;

namespace PvpEvents
{
	public class PvPConfig
	{
		public List<Arena> arenaList;
		public int duelMatchesNeededToWin;
		public int ffaMatchesNeededToWin;
		public int ffaSignupTime;

		public PvPConfig()
		{
			arenaList = new List<Arena>() { new Arena() { Name = "Example", RegionName = "example", SpawnPoints = new List<Point>() { new Point(0, 0), new Point(1, 1) } } };
			duelMatchesNeededToWin = 3;
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
