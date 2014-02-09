using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeoEdit.Common
{
	public class TextData
	{
		List<string> lines = new List<string>();
		List<string> endings = new List<string>();
		string defaultEnding = "\r\n";
		bool bom = false;
		public TextData(BinaryData data, BinaryData.EncodingName encoding = BinaryData.EncodingName.None)
		{
			if (encoding == BinaryData.EncodingName.None)
			{
				bool bom;
				data.GuessEncoding(out encoding, out bom);
			}

			var str = data.ToString(encoding);
			var endingChars = new char[] { '\r', '\n' };
			var start = 0;
			while (start < str.Length)
			{
				if ((start == 0) && (str[0] == '\ufeff'))
				{
					bom = true;
					start++;
				}

				int ending, endingEnd;
				ending = endingEnd = str.IndexOfAny(endingChars, start);
				if (ending == -1)
					ending = endingEnd = str.Length;
				else
					++endingEnd;

				if ((str.Length > endingEnd) && (str[endingEnd - 1] == '\r') && (str[endingEnd] == '\n'))
					++endingEnd;

				lines.Add(str.Substring(start, ending - start));
				endings.Add(str.Substring(ending, endingEnd - ending));
				start = endingEnd;
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
