using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeoEdit.Common.Expressions;

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
			Assert.AreEqual(new NEExpression("2^50").Evaluate().ToString(), "1125899906842624");

			Assert.AreEqual(new NEExpression("-(-4)").Evaluate().ToString(), "4");

			Assert.AreEqual(new NEExpression("2 + 2").Evaluate().ToString(), "4");
			Assert.AreEqual(new NEExpression("5.1 + 2").Evaluate().ToString(), "7.1");
			Assert.AreEqual(new NEExpression("5 + .1").Evaluate().ToString(), "5.1");
			Assert.AreEqual(new NEExpression("4.9 + .1").Evaluate().ToString(), "5");
			Assert.AreEqual(new NEExpression("5.1 + .1").Evaluate().ToString(), "5.2");
			Assert.AreEqual(new NEExpression("'a' + 1").Evaluate().ToString(), "b");
			Assert.AreEqual(new NEExpression("\"a\" + 1").Evaluate().ToString(), "a1");

			Assert.AreEqual(new NEExpression("2 - 2").Evaluate().ToString(), "0");
			Assert.AreEqual(new NEExpression("5.1 - 2").Evaluate().ToString(), "3.1");
			Assert.AreEqual(new NEExpression("5 - .1").Evaluate().ToString(), "4.9");
			Assert.AreEqual(new NEExpression("4.9 - .1").Evaluate().ToString(), "4.8");
			Assert.AreEqual(new NEExpression("5.1 - .1").Evaluate().ToString(), "5");
			Assert.AreEqual(new NEExpression("'b' - 1").Evaluate().ToString(), "a");

			Assert.AreEqual(new NEExpression("5.4 * 2").Evaluate().ToString(), "10.8");
			Assert.AreEqual(new NEExpression("5 * 2.1").Evaluate().ToString(), "10.5");
			Assert.AreEqual(new NEExpression("5 * 2.1").Evaluate().ToString(), "10.5");
			Assert.AreEqual(new NEExpression("\"ok\" * 4").Evaluate().ToString(), "okokokok");
			Assert.AreEqual(new NEExpression("'o' * 4").Evaluate().ToString(), "oooo");

			Assert.AreEqual(new NEExpression("5 / 2").Evaluate().ToString(), "2.5");
			Assert.AreEqual(new NEExpression("5.0 / 2").Evaluate().ToString(), "2.5");
			Assert.AreEqual(new NEExpression("5 / 2.0").Evaluate().ToString(), "2.5");

			Assert.AreEqual(new NEExpression("4 ^ 3").Evaluate().ToString(), "64");
			Assert.AreEqual(new NEExpression("64 ^ (1 / 3.0)").Evaluate().ToString(), "4");
			Assert.AreEqual(new NEExpression("410.0625 ^ .25").Evaluate().ToString(), "4.5");

			Assert.AreEqual(new NEExpression("2 << 2").Evaluate().ToString(), "8");
			Assert.AreEqual(new NEExpression("1048576 >> 10").Evaluate().ToString(), "1024");

			Assert.AreEqual(new NEExpression("~[0]").Evaluate(0xdeadbeef).ToString(), "-3735928560");
			Assert.AreEqual(new NEExpression("&").Evaluate(0xdeadbeef, 0x0badf00d).ToString(), "179154957");
			Assert.AreEqual(new NEExpression("^^").Evaluate(0xdeadbeef, 0x0badf00d).ToString(), "3573567202");
			Assert.AreEqual(new NEExpression("|").Evaluate(0xdeadbeef, 0x0badf00d).ToString(), "3752722159");

			Assert.AreEqual(new NEExpression("TRUE").Evaluate(), true);
			Assert.AreEqual(new NEExpression("False").Evaluate(), false);

			Assert.AreEqual(new NEExpression("5.0 + 2.1").Evaluate().ToString(), "7.1");
			Assert.AreEqual(new NEExpression("3.6 + 2.1 * 5.0").Evaluate().ToString(), "14.1");
			Assert.AreEqual(new NEExpression("(3.6 + 2.1) * 5.0").Evaluate().ToString(), "28.5");
			Assert.AreEqual(new NEExpression("[0] + [1]").Evaluate(5.4, 6.7).ToString(), "12.1");

			Assert.AreEqual(new NEExpression("[0] is \"Int32\"").Evaluate((object)5), true);

			Assert.AreEqual(new NEExpression("\"5a\" == \"5a\"").Evaluate((object)5), true);
			Assert.AreEqual(new NEExpression("\"5a\" == \"5A\"").Evaluate((object)5), false);
			Assert.AreEqual(new NEExpression("\"5a\" i== \"5a\"").Evaluate((object)5), true);
			Assert.AreEqual(new NEExpression("\"5a\" i== \"5A\"").Evaluate((object)5), true);
			Assert.AreEqual(new NEExpression("\"5a\" != \"5a\"").Evaluate((object)5), false);
			Assert.AreEqual(new NEExpression("\"5a\" != \"5A\"").Evaluate((object)5), true);
			Assert.AreEqual(new NEExpression("\"5a\" i!= \"5a\"").Evaluate((object)5), false);
			Assert.AreEqual(new NEExpression("\"5a\" i!= \"5A\"").Evaluate((object)5), false);

			Assert.AreEqual(new NEExpression("[0].\"value\"").Evaluate(new ExpressionDotTest(5)).ToString(), "5");

			Assert.AreEqual(new NEExpression("Type([0]).\"FullName\"").Evaluate(new ExpressionDotTest(5)).ToString(), typeof(ExpressionDotTest).FullName);

			Assert.AreEqual(new NEExpression("ValidRE([0])").Evaluate(@"\d+"), true);
			Assert.AreEqual(new NEExpression("ValidRE([0])").Evaluate(@"["), false);

			Assert.AreEqual(new NEExpression("+").Evaluate(1, 2, 3, 4, 5.5).ToString(), "15.5");
			Assert.AreEqual(new NEExpression("||").Evaluate(false, false, true, false), true);
			Assert.AreEqual(new NEExpression("*").Evaluate(4, 5, 6).ToString(), "120");

			Assert.AreEqual(new NEExpression("([0] || [1]) ? [2] : [3]").Evaluate(false, true, 5, 6).ToString(), "5");

			Assert.AreEqual(new NEExpression("+").Evaluate("I", "Can", null, "Join", "Strings").ToString(), "ICanJoinStrings");

			Assert.AreEqual(new NEExpression("StrFormat(\"[0]{0}+{1} is {2}\", [0], [1], [0] + [1])").Evaluate(5, 7).ToString(), "[0]5+7 is 12");

			Assert.AreEqual(new NEExpression("StrFormat(\"Test\")").Evaluate().ToString(), "Test");
			Assert.AreEqual(new NEExpression("StrFormat(\"Test {0}\", 5)").Evaluate().ToString(), "Test 5");
			Assert.AreEqual(new NEExpression("StrFormat(\"Test {0} {1}\", 5, 7)").Evaluate().ToString(), "Test 5 7");

			Assert.AreEqual(new NEExpression("!true").Evaluate(), false);
			Assert.AreEqual(new NEExpression("![0]").Evaluate(false), true);
			Assert.AreEqual(new NEExpression("-4").Evaluate().ToString(), "-4");

			Assert.AreEqual(new NEExpression("Type([0])").Evaluate((byte)0), typeof(byte));

			Assert.AreEqual(new NEExpression("0xdeadbeef + [0]").Evaluate(0x0badf00d).ToString(), "3931877116");

			Assert.AreEqual(new NEExpression("Min(3,4,2)").Evaluate().ToString(), "2");
			Assert.AreEqual(new NEExpression("Max(3,4,2)").Evaluate().ToString(), "4");

			Assert.AreEqual(new NEExpression("pi").Evaluate().ToString(), "3.14159265358979");
			Assert.AreEqual(new NEExpression("e").Evaluate().ToString(), "2.71828182845905");
			Assert.AreEqual(new NEExpression("i").Evaluate().ToString(), "i");

			Assert.AreEqual(new NEExpression("i*2*i").Evaluate().ToString(), "-2");

			Assert.AreEqual(new NEExpression("5!").Evaluate().ToString(), "120");

			Assert.AreEqual(new NEExpression("towords(411000045312)").Evaluate().ToString(), "Four hundred eleven billion forty five thousand three hundred twelve");
			Assert.AreEqual(new NEExpression("towords(0)").Evaluate().ToString(), "Zero");
			Assert.AreEqual(new NEExpression("towords(-5)").Evaluate().ToString(), "Negative five");

			Assert.AreEqual(new NEExpression("fromwords(\" five -hundred-fifty-four		 million, two hundred twelve thousand, nineteen  \")").Evaluate().ToString(), "554212019");
			Assert.AreEqual(new NEExpression("fromwords(\"5.11 million\")").Evaluate().ToString(), "5110000");
			Assert.AreEqual(new NEExpression("fromwords(\"Four hundred eleven billion forty five thousand three hundred twelve\")").Evaluate().ToString(), "411000045312");
			Assert.AreEqual(new NEExpression("fromwords(\"Zero\")").Evaluate().ToString(), "0");
			Assert.AreEqual(new NEExpression("fromwords(\"Negative five\")").Evaluate().ToString(), "-5");

			var dict = new Dictionary<string, object>
			{
				{ "x", 0xdeadbeef },
				{ "y", 0x0badf00d },
				{ "z", 0x0defaced },
			};
			var expr = new NEExpression("x - y + [0]");
			var vars = expr.Variables;
			Assert.AreEqual(vars.Count, 2);
			Assert.IsTrue(vars.Contains("x"));
			Assert.IsTrue(vars.Contains("y"));
			Assert.AreEqual(expr.Evaluate(dict, 0xfeedface).ToString(), "7816989104");
		}
	}
}
