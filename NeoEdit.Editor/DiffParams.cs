using System;
using System.Collections.Generic;
using System.Text;

namespace NeoEdit.Editor
{
	class DiffParams
	{
		public readonly bool IgnoreWhitespace, IgnoreCase, IgnoreNumbers, IgnoreLineEndings;
		public readonly string IgnoreCharacters;
		public readonly int LineStartTabStop;

		readonly HashSet<char> IgnoreCharactersHash;

		public DiffParams(bool ignoreWhitespace, bool ignoreCase, bool ignoreNumbers, bool ignoreLineEndings, string ignoreCharacters, int lineStartTabStop = 0)
		{
			IgnoreWhitespace = ignoreWhitespace;
			IgnoreCase = ignoreCase;
			IgnoreNumbers = ignoreNumbers;
			IgnoreLineEndings = ignoreLineEndings;
			IgnoreCharacters = ignoreCharacters;
			LineStartTabStop = lineStartTabStop;

			IgnoreCharactersHash = new HashSet<char>(IgnoreCharacters ?? "");
		}

		public Tuple<string, List<int>> FormatLine(string line)
		{
			var map = new List<int>();
			var sb = new StringBuilder(line.Length);
			var inNumber = false;
			var lineStart = (LineStartTabStop != 0) && (!string.IsNullOrWhiteSpace(line));
			for (var ctr = 0; ctr < line.Length; ++ctr)
			{
				var ch = line[ctr];

				if ((lineStart) && ((!char.IsWhiteSpace(ch)) || (ch == '\r') || (ch == '\n')))
					lineStart = false;
				if (lineStart)
				{
					var str = ch.ToString();
					if (str == "\t")
						str = new string(' ', LineStartTabStop - sb.Length % LineStartTabStop);
					map.Add(ctr);
					sb.Append(str);
					continue;
				}

				var isNumber = (char.IsDigit(ch)) || ((ch == '.') && (((ctr - 1 >= 0) && (char.IsDigit(line[ctr - 1]))) || ((ctr + 1 < line.Length) && (char.IsDigit(line[ctr + 1])))));
				if (!isNumber)
					inNumber = false;
				if (IgnoreCharactersHash.Contains(ch))
					continue;
				if ((IgnoreWhitespace) && (char.IsWhiteSpace(ch)) && (ch != '\r') && (ch != '\n'))
					continue;
				if (IgnoreCase)
					ch = char.ToLowerInvariant(ch);
				if ((IgnoreNumbers) && (isNumber))
				{
					if (inNumber)
						continue;
					inNumber = true;
					ch = '0';
				}
				if ((IgnoreLineEndings) && ((ch == '\r') || (ch == '\n')))
					continue;

				map.Add(ctr);
				sb.Append(ch);
			}
			map.Add(line.Length);

			return Tuple.Create(sb.ToString(), map);
		}

		public bool Equals(DiffParams diffParams)
		{
			if (IgnoreWhitespace != diffParams.IgnoreWhitespace)
				return false;
			if (IgnoreCase != diffParams.IgnoreCase)
				return false;
			if (IgnoreNumbers != diffParams.IgnoreNumbers)
				return false;
			if (IgnoreLineEndings != diffParams.IgnoreLineEndings)
				return false;
			if (IgnoreCharacters != diffParams.IgnoreCharacters)
				return false;
			return true;
		}
	}
}
