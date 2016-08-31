using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;

namespace PvpEvents
{
	public class PvPConfig
	{
		public string duelArenaName;
		public Point duelArenaSpawn1;
		public Point duelArenaSpawn2;
		public int duelMatchesNeededToWin;

		public PvPConfig()
		{
			duelArenaName = "duel";
			duelArenaSpawn1 = new Point(0, 0);
			duelArenaSpawn2 = new Point(0, 0);
			duelMatchesNeededToWin = 3;
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
