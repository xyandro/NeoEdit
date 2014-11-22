﻿using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NeoEdit.Common.UnitTest
{
	public partial class UnitTest
	{
		class ExpressionDotTest
		{
			public int value { get; private set; }
			public ExpressionDotTest(int _value)
			{
				value = _value;
			}
		}

		[TestMethod]
		public void ExpressionTest()
		{
			Assert.AreEqual(new Expression("2 << 2").Evaluate().ToString(), "8");
			Assert.AreEqual(new Expression("1048576 >> 10").Evaluate().ToString(), "1024");

			Assert.AreEqual(new Expression("&").Evaluate(0xdeadbeef, 0x0badf00d).ToString(), "179154957");
			Assert.AreEqual(new Expression("^").Evaluate(0xdeadbeef, 0x0badf00d).ToString(), "3573567202");
			Assert.AreEqual(new Expression("|").Evaluate(0xdeadbeef, 0x0badf00d).ToString(), "3752722159");

			Assert.AreEqual(new Expression("TRUE").Evaluate().ToString(), "True");
			Assert.AreEqual(new Expression("FaLsE").Evaluate().ToString(), "False");

			Assert.AreEqual(new Expression("5.0 + 2.1").Evaluate().ToString(), "7.1");
			Assert.AreEqual(new Expression("3.6 + 2.1 * 5.0").Evaluate().ToString(), "14.1");
			Assert.AreEqual(new Expression("(3.6 + 2.1) * 5.0").Evaluate().ToString(), "28.5");
			Assert.AreEqual(new Expression("[0] + [1]").Evaluate(5.4, 6.7).ToString(), "12.1");

			Assert.AreEqual(new Expression("[0] IS 'Int32'").Evaluate((object)5).ToString(), "True");

			Assert.AreEqual(new Expression("'5a' t== '5a'").Evaluate((object)5).ToString(), "True");
			Assert.AreEqual(new Expression("'5a' t== '5A'").Evaluate((object)5).ToString(), "False");
			Assert.AreEqual(new Expression("'5a' ti== '5a'").Evaluate((object)5).ToString(), "True");
			Assert.AreEqual(new Expression("'5a' ti== '5A'").Evaluate((object)5).ToString(), "True");
			Assert.AreEqual(new Expression("'5a' t!= '5a'").Evaluate((object)5).ToString(), "False");
			Assert.AreEqual(new Expression("'5a' t!= '5A'").Evaluate((object)5).ToString(), "True");
			Assert.AreEqual(new Expression("'5a' ti!= '5a'").Evaluate((object)5).ToString(), "False");
			Assert.AreEqual(new Expression("'5a' ti!= '5A'").Evaluate((object)5).ToString(), "False");

			Assert.AreEqual(new Expression("[0].'value'").Evaluate(new ExpressionDotTest(5)).ToString(), "5");

			Assert.AreEqual(new Expression("Type([0]).'FullName'").Evaluate(new ExpressionDotTest(5)).ToString(), typeof(ExpressionDotTest).FullName);

			Assert.AreEqual(new Expression("ValidRE([0])").Evaluate(@"\d+").ToString(), "True");
			Assert.AreEqual(new Expression("ValidRE([0])").Evaluate(@"[").ToString(), "False");

			Assert.AreEqual(new Expression("+").Evaluate(1, 2, 3, 4, 5.5).ToString(), "15.5");
			Assert.AreEqual(new Expression("||").Evaluate(false, false, true, false).ToString(), "True");
			Assert.AreEqual(new Expression("*").Evaluate(4, 5, 6).ToString(), "120");

			Assert.AreEqual(new Expression("([0] || [1]) ? [2] : [3]").Evaluate(false, true, 5, 6).ToString(), "5");

			Assert.AreEqual(new Expression("t+").Evaluate("I", "Can", null, "Join", "Strings").ToString(), "ICanJoinStrings");

			Assert.AreEqual(new Expression("StrFormat('[0]{0}+{1} is {2}', [0], [1], ([0]+[1]))").Evaluate(5, 7).ToString(), "[0]5+7 is 12");
		}
	}
}
