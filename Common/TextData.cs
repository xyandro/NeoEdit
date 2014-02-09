using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NeoEdit.Common
{
	public class TextData
	{
		static Regex RecordsRE = new Regex("(.*?)(\r\n|\n\r|\n|\r|$)");
		List<string> lines = new List<string>();
		List<string> endings = new List<string>();
		string defaultEnding = "\r\n";
		bool bom = false;
		public TextData(BinaryData data, BinaryData.EncodingName encoding = BinaryData.EncodingName.None)
		{
			if (encoding == BinaryData.EncodingName.None)
				encoding = data.GuessEncoding();

			var str = data.ToString(encoding);
			bom = (str.Length > 0) && (str[0] == '\ufeff');
			var matches = RecordsRE.Matches(str, bom ? 1 : 0);

			foreach (Match match in matches)
			{
				if ((String.IsNullOrEmpty(match.Groups[1].Value)) && (String.IsNullOrEmpty(match.Groups[2].Value)))
					continue;
				lines.Add(match.Groups[1].Value);
				endings.Add(match.Groups[1].Value);
			}

			// Select most popular line ending
			if (endings.Count != 0)
				defaultEnding = endings.GroupBy(a => a).OrderByDescending(a => a.Count()).Select(a => a.Key).First();
		}

		public string this[int index]
		{
			get
			{
				if ((index < 0) || (index >= lines.Count))
					throw new IndexOutOfRangeException();
				return lines[index];
			}
		}

		public BinaryData GetData(BinaryData.EncodingName encoding)
		{
			var sb = new StringBuilder();
			if (bom)
				sb.Append("\ufeff");
			for (var ctr = 0; ctr < lines.Count; ctr++)
			{
				sb.Append(lines[ctr]);
				sb.Append(endings[ctr]);
			}
			return BinaryData.FromString(encoding, sb.ToString());
		}

		public int NumLines { get { return lines.Count; } }
	}
}
