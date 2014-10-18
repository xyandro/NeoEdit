using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeoEdit.Common.Transform;

namespace NeoEdit.Common.UnitTest
{
	public partial class UnitTest
	{
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

		string GetText(Coder.Type encoding, bool bom, Endings ending)
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

		List<T> GetValues<T>()
		{
			return Enum.GetValues(typeof(T)).Cast<T>().ToList();
		}

		[TestMethod]
		public void TestUnicode()
		{
			var dir = Path.Combine(Directory.GetCurrentDirectory(), "TestData");
			Directory.CreateDirectory(dir);

			using (var combined = File.Create(Path.Combine(dir, "Combined.txt")))
			{
				foreach (var encoding in GetValues<Coder.Type>().Where(a => a.IsStr()))
					for (var useBom = 0; useBom < 2; ++useBom)
					{
						var bom = useBom == 0;
						foreach (var ending in GetValues<Endings>())
						{
							Encoding encoder = null;
							switch (encoding)
							{
								case Coder.Type.UTF8:
									encoder = new UTF8Encoding(false);
									break;
								case Coder.Type.UTF7:
									encoder = new UTF7Encoding();
									break;
								case Coder.Type.UTF16LE:
								case Coder.Type.UTF16BE:
									encoder = new UnicodeEncoding(encoding == Coder.Type.UTF16BE, false);
									break;
								case Coder.Type.UTF32BE:
								case Coder.Type.UTF32LE:
									encoder = new UTF32Encoding(encoding == Coder.Type.UTF32BE, false);
									break;
								default:
									throw new Exception("No encoder found");
							}
							var filename = Path.Combine(dir, String.Format("{0}-{1}-{2}.txt", encoding, bom, ending));
							File.WriteAllText(filename, GetText(encoding, bom, ending), encoder);

							var bytes = File.ReadAllBytes(filename);
							combined.Write(bytes, 0, bytes.Length);

							// UTF7 in general thinks it's UTF8; ignore it
							if (encoding == Coder.Type.UTF7)
								continue;

							var encoding2 = Coder.GuessEncoding(bytes);
							var str = Coder.BytesToString(bytes, encoding2);
							var bom2 = (str.Length > 0) && (str[0] == '\ufeff');

							Assert.AreEqual(encoding, encoding2);
							Assert.AreEqual(bom, bom2);
						}
					}
			}
		}
	}
}
