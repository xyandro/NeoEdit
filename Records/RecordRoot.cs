using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace NeoEdit.Records
{
	public abstract class RecordRoot : Record
	{
		protected RecordRoot(string uri, Record parent) : base(uri, parent) { }

		public virtual Record GetRecord(string uri)
		{
			string findUri = "", remaining = uri;
			var record = this as Record;
			while (record != null)
			{
				if (uri.Equals(record.FullName, StringComparison.OrdinalIgnoreCase))
					return record;

				var match = new Regex(@"^(\\*[^\\]+)(.*)$").Match(remaining);
				if (!match.Success)
					break;
				findUri += match.Groups[1].Value;
				remaining = match.Groups[2].Value;

				record = record.Records.SingleOrDefault(a => a.FullName.Equals(findUri, StringComparison.OrdinalIgnoreCase));
			}

			throw new Exception(String.Format("Invalid input: {0}", uri));
		}
	}
}
