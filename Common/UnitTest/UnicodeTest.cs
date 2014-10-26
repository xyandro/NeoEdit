﻿using System;
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

		string GetText(StrCoder.CodePage codePage, bool bom, Endings ending)
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

			result = result.Replace("<Encoding>", codePage.ToString()).Replace("<BOM>", bom.ToString()).Replace("<Ending>", ending.ToString());
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
				foreach (var codePage in GetValues<StrCoder.CodePage>())
				{
					if (codePage <= 0)
						continue;

					for (var useBom = 0; useBom < 2; ++useBom)
					{
						var bom = useBom == 0;
						foreach (var ending in GetValues<Endings>())
						{
							Encoding encoder = null;
							switch (codePage)
							{
								case StrCoder.CodePage.UTF8:
									encoder = new UTF8Encoding(false);
									break;
								case StrCoder.CodePage.UTF16LE:
								case StrCoder.CodePage.UTF16BE:
									encoder = new UnicodeEncoding(codePage == StrCoder.CodePage.UTF16BE, false);
									break;
								case StrCoder.CodePage.UTF32BE:
								case StrCoder.CodePage.UTF32LE:
									encoder = new UTF32Encoding(codePage == StrCoder.CodePage.UTF32BE, false);
									break;
								default:
									throw new Exception("No encoder found");
							}
							var filename = Path.Combine(dir, String.Format("{0}-{1}-{2}.txt", codePage, bom, ending));
							var str = GetText(codePage, bom, ending);
							File.WriteAllText(filename, str, encoder);

							var bytes = File.ReadAllBytes(filename);
							combined.Write(bytes, 0, bytes.Length);

							var encoding2 = StrCoder.GuessUnicodeEncoding(bytes);
							var str2 = StrCoder.BytesToString(bytes, encoding2);
							var bom2 = (str2.Length > 0) && (str2[0] == '\ufeff');

							Assert.AreEqual(codePage, encoding2);
							Assert.AreEqual(bom, bom2);
							Assert.AreEqual(str, str2);
						}
					}
				}
			}
		}
	}
}
