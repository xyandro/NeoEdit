using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NeoEdit.Records.Network
{
	public class NetworkRoot : RecordRoot
	{
		public static NetworkRoot Static { get; private set; }

		internal NetworkRoot(Record parent)
			: base("Network", parent)
		{
			if (Static != null)
				throw new Exception("Can only create root nodes once.");
			Static = this;
		}

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

		protected override IEnumerable<Record> InternalRecords
		{
			get
			{
				foreach (var path in paths)
					yield return new NetworkDir(path, this);
			}
		}
	}
}
