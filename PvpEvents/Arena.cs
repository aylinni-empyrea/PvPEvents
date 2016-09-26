using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PvpEvents
{
	public class Arena
	{
		public string Name { get; set; }
		public string RegionName { get; set; }
		public List<Point> SpawnPoints { get; set; }
	}
}
