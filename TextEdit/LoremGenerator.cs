using System;
using System.Collections.Generic;
using System.Text;
using NeoEdit.Common;

namespace NeoEdit.TextEdit
{
	class LoremGenerator
	{
		static readonly List<string> LoremWords = new List<string> { "consectetur", "adipiscing", "elit", "pellentesque", "sed", "arcu", "mollis", "fringilla", "ante", "lorem", "a", "sagittis", "odio", "nam", "auctor", "commodo", "placerat", "purus", "ex", "volutpat", "risus", "iaculis", "ipsum", "nulla", "sem", "at", "nisi", "mauris", "dictum", "lacus", "ac", "dolor", "efficitur", "pharetra", "leo", "maecenas", "vulputate", "in", "felis", "proin", "urna", "est", "sollicitudin", "sit", "id", "eleifend", "eget", "non", "enim", "dui", "amet", "donec", "pretium", "nibh", "et", "porta", "consequat", "duis", "quis", "vivamus", "aliquet", "etiam", "aliquam", "tincidunt", "nunc", "tempus", "mi", "sodales", "lacinia", "sapien", "suscipit", "ut", "neque", "venenatis", "aenean", "laoreet", "scelerisque", "lobortis", "vel", "phasellus", "vehicula", "elementum", "quam", "interdum", "rutrum", "egestas", "eu", "pulvinar", "posuere", "accumsan", "augue", "mattis", "morbi", "luctus", "nullam", "maximus", "cum", "sociis", "natoque", "penatibus", "magnis", "dis", "parturient", "montes", "nascetur", "ridiculus", "mus", "euismod", "libero", "porttitor", "ligula", "vitae", "finibus", "magna", "quisque", "massa", "feugiat", "nisl", "fusce", "tempor", "eros", "gravida", "hendrerit", "ultricies", "imperdiet", "suspendisse", "orci", "class", "aptent", "taciti", "sociosqu", "ad", "litora", "torquent", "per", "conubia", "nostra", "inceptos", "himenaeos", "cras", "molestie", "ultrices", "nec", "condimentum", "integer", "viverra", "dapibus", "turpis", "ornare", "curabitur", "vestibulum", "tellus", "varius", "cursus", "velit", "diam", "congue", "primis", "faucibus", "cubilia", "curae", "metus", "blandit", "lectus", "tristique", "malesuada", "tortor", "facilisis", "ullamcorper", "convallis", "fermentum", "justo", "bibendum", "potenti", "praesent", "erat", "dignissim", "rhoncus", "semper", "habitant", "senectus", "netus", "fames", "hac", "habitasse", "platea", "dictumst", "facilisi" };
		static readonly List<int> LoremFreqs = new List<int> { 68, 7, 95, 135, 288, 97, 59, 55, 110, 95, 169, 63, 90, 68, 73, 64, 56, 68, 90, 66, 85, 64, 112, 146, 86, 155, 78, 138, 58, 85, 165, 81, 60, 52, 89, 55, 51, 229, 85, 60, 81, 84, 60, 172, 177, 67, 189, 169, 86, 75, 172, 107, 67, 90, 199, 55, 59, 73, 149, 50, 64, 48, 114, 120, 157, 73, 71, 77, 58, 86, 75, 195, 76, 67, 71, 75, 59, 62, 168, 55, 68, 61, 91, 72, 56, 66, 145, 67, 85, 66, 85, 73, 60, 64, 59, 61, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 66, 74, 51, 77, 153, 58, 81, 50, 66, 60, 69, 60, 69, 81, 66, 67, 73, 53, 62, 94, 13, 13, 13, 13, 13, 13, 13, 26, 13, 13, 13, 13, 57, 56, 58, 157, 64, 51, 55, 62, 100, 57, 53, 130, 92, 68, 59, 76, 78, 51, 19, 75, 9, 9, 74, 58, 83, 57, 89, 74, 62, 75, 48, 76, 96, 59, 14, 60, 96, 55, 65, 54, 5, 5, 5, 15, 7, 7, 7, 7, 5 };
		static readonly ThreadSafeRandom random = new ThreadSafeRandom();

		public IEnumerable<string> GetWords()
		{
			var loremWord = 0;
			var loremFreq = 0;
			const int skipValue = 1000;

			while (true)
			{
				loremFreq += skipValue;
				while (loremFreq >= LoremFreqs[loremWord])
				{
					loremFreq -= LoremFreqs[loremWord];
					++loremWord;
					if (loremWord >= LoremWords.Count)
						loremWord = 0;
				}

				yield return LoremWords[loremWord];
			}
		}

		public IEnumerable<string> GetSentences()
		{
			using (var words = GetWords().GetEnumerator())
				while (true)
				{
					var sentence = new StringBuilder();
					var totalWords = 0;
					var numWords = 0;
					while (true)
					{
						if (!words.MoveNext())
							throw new Exception("Failed to get more words");
						var word = words.Current;
						if (sentence.Length == 0)
							word = char.ToUpper(word[0]) + word.Substring(1);
						sentence.Append(word);
						++totalWords;
						++numWords;
						if (numWords >= 2)
						{
							var rand = random.Next(0, 10);
							if ((rand == 0) || (totalWords >= 10))
							{
								sentence.Append(".");
								yield return sentence.ToString();
								break;
							}
							if ((rand == 1) || (rand == 2))
							{
								sentence.Append(",");
								numWords = 0;
							}
						}
						sentence.Append(" ");
					}
				}
		}
	}
}
