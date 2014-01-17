using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NeoEdit.Records.Network
{
	public class NetworkRoot : RecordRoot
	{
		public NetworkRoot(RecordList parent) : base("Network", parent) { }

		static Regex networkRE = new Regex(@"^(\\\\[^\\]+)");
		static List<string> paths = new List<string>();
		public override Record GetRecord(string uri)
		{
			var match = networkRE.Match(uri);
			if (!match.Success)
				return null;

			var root = match.Groups[1].Value.ToLower();
			if (!paths.Contains(root))
				paths.Add(root);
			return base.GetRecord(uri);
		}

		public override IEnumerable<Record> Records { get { return paths.Select(a => new NetworkDir(a, this)); } }
	}
}
