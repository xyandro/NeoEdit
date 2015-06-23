using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeoEdit.Common;
using NeoEdit.Common.Transform;

namespace NeoEdit.TextEdit.UnitTest
{
	[TestClass]
	public class TextDataTest
	{
		const string TestString =
			"Line 0\r\n" +
			"Line 1\n" +
			"Line 2\r" +
			"Line 3\n\r" +
			"Line 4\r\n" +
			"\tLine\t5\t\r\n" +
			"{Asdf}\r\n";
		const int numLines = 7;

		TextData GetTextData()
		{
			return new TextData(Encoding.UTF8.GetBytes(TestString), Coder.CodePage.UTF8);
		}

		[TestMethod]
		public void LimitsTest()
		{
			var textData = GetTextData();

			try { var test = textData[-1]; Assert.Fail(); }
			catch (IndexOutOfRangeException) { }
			Assert.AreEqual(textData[3], "Line 3");
			try { var test = textData[numLines + 1]; Assert.Fail(); }
			catch (IndexOutOfRangeException) { }

			try { var test = textData.GetLine(-1); Assert.Fail(); }
			catch (IndexOutOfRangeException) { }
			Assert.AreEqual(textData.GetLine(3), "Line 3");
			Assert.AreEqual(textData.GetLine(5), "\tLine\t5\t");
			try { var test = textData.GetLine(numLines + 1); Assert.Fail(); }
			catch (IndexOutOfRangeException) { }

			try { var test = textData.GetLineColumns(-1); Assert.Fail(); }
			catch (IndexOutOfRangeException) { }
			Assert.AreEqual(textData.GetLineColumns(3), "Line 3");
			Assert.AreEqual(textData.GetLineColumns(5), "    Line    5   ");
			try { var test = textData.GetLineColumns(numLines + 1); Assert.Fail(); }
			catch (IndexOutOfRangeException) { }

			try { var test = textData[-1, 0]; Assert.Fail(); }
			catch (IndexOutOfRangeException) { }
			try { var test = textData[numLines + 1, 0]; Assert.Fail(); }
			catch (IndexOutOfRangeException) { }
			try { var test = textData[0, -1]; Assert.Fail(); }
			catch (IndexOutOfRangeException) { }
			Assert.AreEqual(textData[0, 2], 'n');
			Assert.AreEqual(textData[0, 5], '0');
			try { var test = textData[0, 6]; Assert.Fail(); }
			catch (IndexOutOfRangeException) { }

			try { var test = textData.GetLineLength(-1); Assert.Fail(); }
			catch (IndexOutOfRangeException) { }
			Assert.AreEqual(textData.GetLineLength(4), 6);
			Assert.AreEqual(textData.GetLineLength(5), 8);
			try { var test = textData.GetLineLength(numLines + 1); Assert.Fail(); }
			catch (IndexOutOfRangeException) { }

			try { var test = textData.GetLineColumnsLength(-1); Assert.Fail(); }
			catch (IndexOutOfRangeException) { }
			Assert.AreEqual(textData.GetLineColumnsLength(4), 6);
			Assert.AreEqual(textData.GetLineColumnsLength(5), 16);
			try { var test = textData.GetLineColumnsLength(numLines + 1); Assert.Fail(); }
			catch (IndexOutOfRangeException) { }

			try { var test = textData.GetEnding(-1); Assert.Fail(); }
			catch (IndexOutOfRangeException) { }
			Assert.AreEqual(textData.GetEnding(0), "\r\n");
			Assert.AreEqual(textData.GetEnding(3), "\n\r");
			try { var test = textData.GetEnding(numLines + 1); Assert.Fail(); }
			catch (IndexOutOfRangeException) { }

			try { var test = textData.GetOffset(-1, 0); Assert.Fail(); }
			catch (IndexOutOfRangeException) { }
			try { var test = textData.GetOffset(numLines + 1, 0); Assert.Fail(); }
			catch (IndexOutOfRangeException) { }
			try { var test = textData.GetOffset(4, -1); Assert.Fail(); }
			catch (IndexOutOfRangeException) { }
			Assert.AreEqual(textData.GetOffset(4, 0), 30);
			Assert.AreEqual(textData.GetOffset(4, 6), 36);
			Assert.AreEqual(textData.GetOffset(4, 7), 38);
			try { var test = textData.GetOffset(4, 8); Assert.Fail(); }
			catch (IndexOutOfRangeException) { }

			try { var test = textData.GetOffsetLine(-1); Assert.Fail(); }
			catch (IndexOutOfRangeException) { }
			Assert.AreEqual(textData.GetOffsetLine(30), 4);
			Assert.AreEqual(textData.GetOffsetLine(36), 4);
			Assert.AreEqual(textData.GetOffsetLine(37), 4);
			Assert.AreEqual(textData.GetOffsetLine(38), 5);
			try { var test = textData.GetOffsetLine(TestString.Length + 1); Assert.Fail(); }
			catch (IndexOutOfRangeException) { }

			try { var test = textData.GetOffsetIndex(0, -1); Assert.Fail(); }
			catch (IndexOutOfRangeException) { }
			try { var test = textData.GetOffsetIndex(0, numLines + 1); Assert.Fail(); }
			catch (IndexOutOfRangeException) { }
			try { var test = textData.GetOffsetIndex(29, 4); Assert.Fail(); }
			catch (IndexOutOfRangeException) { }
			Assert.AreEqual(textData.GetOffsetIndex(30, 4), 0);
			Assert.AreEqual(textData.GetOffsetIndex(36, 4), 6);
			Assert.AreEqual(textData.GetOffsetIndex(37, 4), 7);
			Assert.AreEqual(textData.GetOffsetIndex(38, 4), 7);
			try { var test = textData.GetOffsetIndex(39, 4); Assert.Fail(); }
			catch (IndexOutOfRangeException) { }
			Assert.AreEqual(textData.GetOffsetIndex(38, 5), 0);

			try { var test = textData.GetColumnFromIndex(-1, 0); Assert.Fail(); }
			catch (IndexOutOfRangeException) { }
			try { var test = textData.GetColumnFromIndex(numLines + 1, 0); Assert.Fail(); }
			catch (IndexOutOfRangeException) { }
			try { var test = textData.GetColumnFromIndex(5, -1); Assert.Fail(); }
			catch (IndexOutOfRangeException) { }
			Assert.AreEqual(textData.GetColumnFromIndex(5, 0), 0);
			Assert.AreEqual(textData.GetColumnFromIndex(5, 1), 4);
			Assert.AreEqual(textData.GetColumnFromIndex(5, 2), 5);
			Assert.AreEqual(textData.GetColumnFromIndex(5, 5), 8);
			Assert.AreEqual(textData.GetColumnFromIndex(5, 6), 12);
			Assert.AreEqual(textData.GetColumnFromIndex(5, 7), 13);
			Assert.AreEqual(textData.GetColumnFromIndex(5, 8), 16);
			Assert.AreEqual(textData.GetColumnFromIndex(5, 9), 17);
			try { var test = textData.GetColumnFromIndex(5, 10); Assert.Fail(); }
			catch (IndexOutOfRangeException) { }

			try { var test = textData.GetIndexFromColumn(-1, 0); Assert.Fail(); }
			catch (IndexOutOfRangeException) { }
			try { var test = textData.GetIndexFromColumn(numLines + 1, 0); Assert.Fail(); }
			catch (IndexOutOfRangeException) { }
			try { var test = textData.GetIndexFromColumn(5, -1); Assert.Fail(); }
			catch (IndexOutOfRangeException) { }
			Assert.AreEqual(textData.GetIndexFromColumn(5, 0), 0);
			Assert.AreEqual(textData.GetIndexFromColumn(5, 3), 1);
			Assert.AreEqual(textData.GetIndexFromColumn(5, 4), 1);
			Assert.AreEqual(textData.GetIndexFromColumn(5, 5), 2);
			Assert.AreEqual(textData.GetIndexFromColumn(5, 8), 5);
			Assert.AreEqual(textData.GetIndexFromColumn(5, 11), 6);
			Assert.AreEqual(textData.GetIndexFromColumn(5, 12), 6);
			Assert.AreEqual(textData.GetIndexFromColumn(5, 13), 7);
			Assert.AreEqual(textData.GetIndexFromColumn(5, 15), 8);
			Assert.AreEqual(textData.GetIndexFromColumn(5, 16), 8);
			Assert.AreEqual(textData.GetIndexFromColumn(5, 17), 9);
			try { var test = textData.GetIndexFromColumn(5, 18); Assert.Fail(); }
			catch (IndexOutOfRangeException) { }
			Assert.AreEqual(textData.GetIndexFromColumn(5, 18, true), 9);

			try { var test = textData.GetString(-1, 0); Assert.Fail(); }
			catch (IndexOutOfRangeException) { }
			try { var test = textData.GetString(0, -1); Assert.Fail(); }
			catch (IndexOutOfRangeException) { }
			try { var test = textData.GetString(0, TestString.Length + 1); Assert.Fail(); }
			catch (IndexOutOfRangeException) { }
			Assert.AreEqual(textData.GetString(0, TestString.Length), TestString);

			Assert.AreEqual(textData.GetOppositeBracket(-1), -1);
			Assert.AreEqual(textData.GetOppositeBracket(TestString.Length + 1), -1);
			Assert.AreEqual(textData.GetOppositeBracket(48), 54);
			Assert.AreEqual(textData.GetOppositeBracket(54), 48);
		}
	}
}
