using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeoEdit.Common;

namespace NeoEdit.Tests
{
	public partial class UnitTest
	{
		const string searchText = "This is my test.";
		readonly List<string> findStrings = new List<string> { "th", "is", "est" };
		readonly List<int> caseResultPos = new List<int> { 2, 5, 12 };
		readonly List<int> caseResultLen = new List<int> { 2, 2, 3 };
		readonly List<int> ignoreCaseResultPos = new List<int> { 0, 2, 5, 12 };
		readonly List<int> ignoreCaseResultLen = new List<int> { 2, 2, 2, 3 };

		void VerifyResults(List<Tuple<int, int>> caseResults, List<Tuple<int, int>> ignoreCaseResults)
		{
			Assert.AreEqual(caseResults.Count, caseResultPos.Count);
			for (var ctr = 0; ctr < caseResults.Count; ++ctr)
			{
				Assert.AreEqual(caseResults[ctr].Item1, caseResultPos[ctr]);
				Assert.AreEqual(caseResults[ctr].Item2, caseResultLen[ctr]);
			}

			Assert.AreEqual(ignoreCaseResults.Count, ignoreCaseResultPos.Count);
			for (var ctr = 0; ctr < ignoreCaseResults.Count; ++ctr)
			{
				Assert.AreEqual(ignoreCaseResults[ctr].Item1, ignoreCaseResultPos[ctr]);
				Assert.AreEqual(ignoreCaseResults[ctr].Item2, ignoreCaseResultLen[ctr]);
			}
		}

		[TestMethod]
		public void SearcherStringTest()
		{
			var testData = searchText;
			var findStrs = findStrings;
			var caseSearcher = new Searcher(findStrs, true);
			var ignoreCaseSearcher = new Searcher(findStrs);
			Assert.AreEqual(caseSearcher.MaxLen, 3);
			Assert.AreEqual(ignoreCaseSearcher.MaxLen, 3);
			var caseResults = caseSearcher.Find(testData);
			var ignoreCaseResults = ignoreCaseSearcher.Find(testData);
			VerifyResults(caseResults, ignoreCaseResults);
		}

		[TestMethod]
		public void SearcherByteTest()
		{
			var testData = Encoding.UTF8.GetBytes(searchText);
			var findStrs = findStrings.Select(str => Encoding.UTF8.GetBytes(str)).ToList();
			var caseSearcher = new Searcher(findStrs, true);
			var ignoreCaseSearcher = new Searcher(findStrs);
			Assert.AreEqual(caseSearcher.MaxLen, 3);
			Assert.AreEqual(ignoreCaseSearcher.MaxLen, 3);
			var caseResults = caseSearcher.Find(testData);
			var ignoreCaseResults = ignoreCaseSearcher.Find(testData);
			VerifyResults(caseResults, ignoreCaseResults);
		}
	}
}
