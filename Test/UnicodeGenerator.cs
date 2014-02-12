using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NeoEdit.Common;

namespace NeoEdit.Test
{
	class UnicodeGenerator
	{
		enum Endings
		{
			CRLF,
			CR,
			LF,
			Mixed,
		};

		static string GetEnding(Endings ending)
		{
			switch (ending)
			{
				case Endings.CRLF: return "\r\n";
				case Endings.CR: return "\r";
				case Endings.LF: return "\n";
				default: throw new Exception("Invalid ending");
			}
		}

		static string GetText(BinaryData.EncodingName encoding, bool bom, Endings ending)
		{
			var text = new List<string>
			{
				"BEGIN",
				"Encoding: <Encoding>",
				"BOM: <BOM>",
				"Ending: <Ending>",
				"",
				"This is my example text.",
				"",
				"↖↑↗",
				"←↕↔↕→",
				"↙↓↘",
				"",
				"END",
			};

			string result;
			if (ending != Endings.Mixed)
				result = String.Join(GetEnding(ending), text);
			else
			{
				result = "";
				ending = 0;
				foreach (var line in text)
				{
					result += line + GetEnding(ending);
					if (++ending == Endings.Mixed)
						ending = 0;
				}
			}

			result = result.Replace("<Encoding>", encoding.ToString()).Replace("<BOM>", bom.ToString()).Replace("<Ending>", ending.ToString());
			if (bom)
				result = "\ufeff" + result;

			return result;
		}

		static List<T> GetValues<T>()
		{
			return Enum.GetValues(typeof(T)).Cast<T>().ToList();
		}

		static public void Run()
		{
			var dir = Path.Combine(Directory.GetCurrentDirectory(), "TestData");
			Directory.CreateDirectory(dir);

			using (var combined = File.Create(Path.Combine(dir, "Combined.txt")))
			{
				foreach (var encoding in GetValues<BinaryData.EncodingName>().Where(a => a.IsStr()))
					for (var useBom = 0; useBom < 2; ++useBom)
					{
						var bom = useBom == 0;
						foreach (var ending in GetValues<Endings>())
						{
							Encoding encoder = null;
							switch (encoding)
							{
								case BinaryData.EncodingName.UTF7:
									encoder = new UTF7Encoding();
									break;
								case BinaryData.EncodingName.UTF8:
									encoder = new UTF8Encoding(false);
									break;
								case BinaryData.EncodingName.UTF16LE:
								case BinaryData.EncodingName.UTF16BE:
									encoder = new UnicodeEncoding(encoding == BinaryData.EncodingName.UTF16BE, false);
									break;
								case BinaryData.EncodingName.UTF32BE:
								case BinaryData.EncodingName.UTF32LE:
									encoder = new UTF32Encoding(encoding == BinaryData.EncodingName.UTF32BE, false);
									break;
								default:
									throw new Exception("No encoder found");
							}
							var filename = Path.Combine(dir, String.Format("{0}-{1}-{2}.txt", encoding, bom, ending));
							File.WriteAllText(filename, GetText(encoding, bom, ending), encoder);

							var bytes = File.ReadAllBytes(filename);
							combined.Write(bytes, 0, bytes.Length);

							BinaryData data = bytes;
							var encoding2 = data.GuessEncoding();
							var str = data.ToString(encoding2);
							var bom2 = (str.Length > 0) && (str[0] == '\ufeff');

							if ((encoding != encoding2) || (bom != bom2))
							{
								// UTF7 in general thinks it's UTF8; ignore it
								if (encoding == BinaryData.EncodingName.UTF7)
									continue;

								if (encoding != encoding2)
									throw new Exception("Failed to guess encoding");
								if (bom != bom2)
									throw new Exception("Failed to detect BOM");
							}
						}
					}
			}
		}
	}
}
