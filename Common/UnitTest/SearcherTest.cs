using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NeoEdit.Common.UnitTest
{
	public partial class UnitTest
	{
		const string searchText = "This is my test.";
		readonly string[] findStrings = new string[] { "th", "is", "est" };
		readonly int[] sensitiveResultPos = new int[] { 2, 5, 12 };
		readonly int[] sensitiveResultLen = new int[] { 2, 2, 3 };
		readonly int[] insensitiveResultPos = new int[] { 0, 2, 5, 12 };
		readonly int[] insensitiveResultLen = new int[] { 2, 2, 2, 3 };

		void VerifyResults(List<Tuple<int, int>> sensitiveResults, List<Tuple<int, int>> insensitiveResults)
		{
			Assert.AreEqual(sensitiveResults.Count, sensitiveResultPos.Length);
			for (var ctr = 0; ctr < sensitiveResults.Count; ++ctr)
			{
				Assert.AreEqual(sensitiveResults[ctr].Item1, sensitiveResultPos[ctr]);
				Assert.AreEqual(sensitiveResults[ctr].Item2, sensitiveResultLen[ctr]);
			}

			Assert.AreEqual(insensitiveResults.Count, insensitiveResultPos.Length);
			for (var ctr = 0; ctr < insensitiveResults.Count; ++ctr)
			{
				Assert.AreEqual(insensitiveResults[ctr].Item1, insensitiveResultPos[ctr]);
				Assert.AreEqual(insensitiveResults[ctr].Item2, insensitiveResultLen[ctr]);
			}
		}

		[TestMethod]
		public void SearcherStringTest()
		{
			var testData = searchText;
			var findStrs = findStrings;
			var sensitiveSearcher = Searcher.Create(findStrs);
			var insensitiveSearcher = Searcher.Create(findStrs, false);
			var sensitiveResults = sensitiveSearcher.Find(testData);
			var insensitiveResults = insensitiveSearcher.Find(testData);
			VerifyResults(sensitiveResults, insensitiveResults);
		}

		[TestMethod]
		public void SearcherCharArrayTest()
		{
			var testData = searchText.ToCharArray();
			var findStrs = findStrings.Select(str => str.ToCharArray()).ToArray();
			var sensitiveSearcher = Searcher.Create(findStrs);
			var insensitiveSearcher = Searcher.Create(findStrs, false);
			var sensitiveResults = sensitiveSearcher.Find(testData);
			var insensitiveResults = insensitiveSearcher.Find(testData);
			VerifyResults(sensitiveResults, insensitiveResults);
		}

		[TestMethod]
		public void SearcherByteTest()
		{
			var testData = Encoding.UTF8.GetBytes(searchText);
			var findStrs = findStrings.Select(str => Encoding.UTF8.GetBytes(str)).ToArray();
			var sensitiveSearcher = Searcher.Create(findStrs);
			var insensitiveSearcher = Searcher.Create(findStrs, false);
			var sensitiveResults = sensitiveSearcher.Find(testData);
			var insensitiveResults = insensitiveSearcher.Find(testData);
			VerifyResults(sensitiveResults, insensitiveResults);
		}
	}
}
