using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NeoEdit
{
	class TestData
	{
		enum Encodings
		{
			UTF8,
			UTF16LE,
			UTF16BE,
			UTF32LE,
			UTF32BE,
		};
		enum BOMs
		{
			Yes,
			No,
		};
		enum Endings
		{
			CRLF,
			CR,
			LF,
			Mixed,
		};

		string GetEnding(Endings ending)
		{
			switch (ending)
			{
				case Endings.CRLF: return "\r\n";
				case Endings.CR: return "\r";
				case Endings.LF: return "\n";
				default: throw new Exception("Invalid ending");
			}
		}

		string GetText(Endings ending)
		{
			var text = new List<string>
			{
				"BEGIN",
				"Encoding: UTF8",
				"Byte marker: Yes",
				"Line endings: CR/LF",
				"",
				"This is my example text.",
				"",
				"↖↑↗",
				"←↕↔↕→",
				"↙↓↘",
				"",
				"END",
			};

			if (ending != Endings.Mixed)
				return string.Join(GetEnding(ending), text);

			var result = "";
			ending = 0;
			foreach (var line in text)
			{
				result += line + GetEnding(ending);
				if (++ending == Endings.Mixed)
					ending = 0;
			}

			return result;
		}

		List<T> GetValues<T>()
		{
			return Enum.GetValues(typeof(T)).Cast<T>().ToList();
		}

		public void Generate()
		{
			var dir = Path.Combine(Directory.GetCurrentDirectory(), "TestData");
			Directory.CreateDirectory(dir);

			using (var combined = File.Create(Path.Combine(dir, "Combined.txt")))
			{
				foreach (var encoding in GetValues<Encodings>())
					foreach (var bom in GetValues<BOMs>())
						foreach (var ending in GetValues<Endings>())
						{
							Encoding encoder = null;
							switch (encoding)
							{
								case Encodings.UTF8:
									encoder = new UTF8Encoding(bom == BOMs.Yes);
									break;
								case Encodings.UTF16LE:
								case Encodings.UTF16BE:
									encoder = new UnicodeEncoding(encoding == Encodings.UTF16BE, bom == BOMs.Yes);
									break;
								case Encodings.UTF32BE:
								case Encodings.UTF32LE:
									encoder = new UTF32Encoding(encoding == Encodings.UTF32BE, bom == BOMs.Yes);
									break;
								default:
									throw new Exception("No encoder found");
							}
							var filename = Path.Combine(dir, String.Format("{0}-{1}-{2}.txt", encoding, bom, ending));
							File.WriteAllText(filename, GetText(ending), encoder);

							var bytes = File.ReadAllBytes(filename);
							combined.Write(bytes, 0, bytes.Length);
						}
			}
		}
	}
}
