using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeoEdit.Records
{
	class Line
	{
		public int start, end;
	}

	public class TextFile
	{
		public enum EncodingType { Unknown, UTF8N, UTF8, UTF16LN, UTF16L, UTF16BN, UTF16B, UTF32LN, UTF32L, UTF32BN, UTF32B };
		public EncodingType encodingType { get; private set; }
		public Encoding encoding { get { return availableEncodings[encodingType]; } }
		static Dictionary<EncodingType, Encoding> availableEncodings;
		static Dictionary<EncodingType, byte[]> preambles;

		static TextFile()
		{
			availableEncodings = new Dictionary<EncodingType, Encoding>()
			{
				{ EncodingType.UTF8N, new UTF8Encoding(false) },
				{ EncodingType.UTF8, Encoding.UTF8 },
				{ EncodingType.UTF16LN, new UnicodeEncoding(false, false) },
				{ EncodingType.UTF16L, Encoding.Unicode },
				{ EncodingType.UTF16BN, new UnicodeEncoding(true, false) },
				{ EncodingType.UTF16B, Encoding.BigEndianUnicode },
				{ EncodingType.UTF32LN, new UTF32Encoding(false, false) },
				{ EncodingType.UTF32L, Encoding.UTF32 },
				{ EncodingType.UTF32BN, new UTF32Encoding(true, false) },
				{ EncodingType.UTF32B, new UTF32Encoding(true, true) },
			};
			preambles = availableEncodings.Select(a => new { key = a.Key, value = a.Value.GetPreamble() }).Where(a => a.value.Length != 0).OrderByDescending(a => a.value.Length).ToDictionary(a => a.key, a => a.value);
		}

		RecordItem file;
		int preambleBytes = 0;
		List<Line> lines = new List<Line>();
		string text = "";
		public int numLines { get { return lines.Count; } }
		public int numCols { get { return lines.Max(a => a.end - a.start); } }

		public string GetLine(int line)
		{
			var useLine = lines[line];
			return text.Substring(useLine.start, useLine.end - useLine.start);
		}

		public TextFile(RecordItem _file)
		{
			file = _file;

			// Figure out encoding
			var firstBlock = file.Read(0, 64);

			encodingType = EncodingType.Unknown;
			if (encodingType == EncodingType.Unknown)
			{
				foreach (var preamble in preambles)
				{
					if (firstBlock.ArrayEqual(preamble.Value, preamble.Value.Length))
					{
						encodingType = preamble.Key;
						preambleBytes = preamble.Value.Length;
						break;
					}
				}
			}

			if (firstBlock.Length >= 4)
			{
				if ((encodingType == EncodingType.Unknown) && (BitConverter.ToInt32(firstBlock, 0) <= 0xffff))
					encodingType = EncodingType.UTF32LN;
				if ((encodingType == EncodingType.Unknown) && (BitConverter.ToInt32(firstBlock.Take(4).Reverse().ToArray(), 0) <= 0xffff))
					encodingType = EncodingType.UTF32BN;
			}
			if (encodingType == EncodingType.Unknown)
			{
				var zeroIndex = firstBlock.IndexOf<byte>(0);
				if (zeroIndex == -1)
					encodingType = EncodingType.UTF8N;
				else if ((zeroIndex & 1) == 1)
					encodingType = EncodingType.UTF16LN;
				else
					encodingType = EncodingType.UTF16BN;
			}

			var data = file.Read(0, (int)file[Property.PropertyType.Size]);
			text = encoding.GetString(data);

			var start = 0;
			for (var ctr = 0; ctr < text.Length; ctr++)
				if (text[ctr] == '\n')
				{
					var end = ctr + 1;
					lines.Add(new Line() { start = start, end = ctr + 1 });
					start = end;
				}
			if (start != text.Length)
				lines.Add(new Line() { start = start, end = text.Length });

			foreach (var line in lines)
			{
				if ((line.end > line.start) && (text[line.end - 1] == '\n'))
					--line.end;
				if ((line.end > line.start) && (text[line.end - 1] == '\r'))
					--line.end;
			}
		}
	}
}
