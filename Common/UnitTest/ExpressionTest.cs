using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeoEdit.Common.Expressions;

namespace NeoEdit.Common.UnitTest
{
	public partial class UnitTest
	{
		class ExpressionDotTest
		{
			public int value { get; }
			public ExpressionDotTest(int _value) { value = _value; }
		}

		[TestMethod]
		public void ExpressionTest()
		{
			Assert.AreEqual("1125899906842624", new NEExpression("2^50").Evaluate().ToString());

			Assert.AreEqual("4", new NEExpression("-(-4)").Evaluate().ToString());

			Assert.AreEqual("4", new NEExpression("2 + 2").Evaluate().ToString());
			Assert.AreEqual("7.1", new NEExpression("5.1 + 2").Evaluate().ToString());
			Assert.AreEqual("5.1", new NEExpression("5 + .1").Evaluate().ToString());
			Assert.AreEqual("5", new NEExpression("4.9 + .1").Evaluate().ToString());
			Assert.AreEqual("5.2", new NEExpression("5.1 + .1").Evaluate().ToString());
			Assert.AreEqual("b", new NEExpression("'a' + 1").Evaluate().ToString());
			Assert.AreEqual("a1", new NEExpression("\"a\" + 1").Evaluate().ToString());

			Assert.AreEqual("0", new NEExpression("2 - 2").Evaluate().ToString());
			Assert.AreEqual("3.1", new NEExpression("5.1 - 2").Evaluate().ToString());
			Assert.AreEqual("4.9", new NEExpression("5 - .1").Evaluate().ToString());
			Assert.AreEqual("4.8", new NEExpression("4.9 - .1").Evaluate().ToString());
			Assert.AreEqual("5", new NEExpression("5.1 - .1").Evaluate().ToString());
			Assert.AreEqual("a", new NEExpression("'b' - 1").Evaluate().ToString());

			Assert.AreEqual("10.8", new NEExpression("5.4 * 2").Evaluate().ToString());
			Assert.AreEqual("10.5", new NEExpression("5 * 2.1").Evaluate().ToString());
			Assert.AreEqual("okokokok", new NEExpression("\"ok\" * 4").Evaluate().ToString());
			Assert.AreEqual("oooo", new NEExpression("'o' * 4").Evaluate().ToString());

			Assert.AreEqual("2.5", new NEExpression("5 / 2").Evaluate().ToString());
			Assert.AreEqual("2", new NEExpression("5 // 2").Evaluate().ToString());
			Assert.AreEqual("2.5", new NEExpression("5.0 / 2").Evaluate().ToString());
			Assert.AreEqual("2.5", new NEExpression("5 / 2.0").Evaluate().ToString());

			Assert.AreEqual("64", new NEExpression("4 ^ 3").Evaluate().ToString());
			Assert.AreEqual("4", new NEExpression("64 ^ (1 / 3.0)").Evaluate().ToString());
			Assert.AreEqual("4.5", new NEExpression("410.0625 ^ .25").Evaluate().ToString());

			Assert.AreEqual("8", new NEExpression("2 << 2").Evaluate().ToString());
			Assert.AreEqual("1024", new NEExpression("1048576 >> 10").Evaluate().ToString());

			Assert.AreEqual("-3735928560", new NEExpression("~[0]").Evaluate(0xdeadbeef).ToString());
			Assert.AreEqual("179154957", new NEExpression("&").Evaluate(0xdeadbeef, 0x0badf00d).ToString());
			Assert.AreEqual("3573567202", new NEExpression("^^").Evaluate(0xdeadbeef, 0x0badf00d).ToString());
			Assert.AreEqual("3752722159", new NEExpression("|").Evaluate(0xdeadbeef, 0x0badf00d).ToString());

			Assert.AreEqual(true, new NEExpression("TRUE").Evaluate());
			Assert.AreEqual(false, new NEExpression("False").Evaluate());

			Assert.AreEqual("7.1", new NEExpression("5.0 + 2.1").Evaluate().ToString());
			Assert.AreEqual("14.1", new NEExpression("3.6 + 2.1 * 5.0").Evaluate().ToString());
			Assert.AreEqual("28.5", new NEExpression("(3.6 + 2.1) * 5.0").Evaluate().ToString());
			Assert.AreEqual("12.1", new NEExpression("[0] + [1]").Evaluate(5.4, 6.7).ToString());

			Assert.AreEqual(true, new NEExpression("[0] is \"Int32\"").Evaluate((object)5));

			Assert.AreEqual(true, new NEExpression("\"5a\" == \"5a\"").Evaluate((object)5));
			Assert.AreEqual(false, new NEExpression("\"5a\" == \"5A\"").Evaluate((object)5));
			Assert.AreEqual(true, new NEExpression("\"5a\" i== \"5a\"").Evaluate((object)5));
			Assert.AreEqual(true, new NEExpression("\"5a\" i== \"5A\"").Evaluate((object)5));
			Assert.AreEqual(false, new NEExpression("\"5a\" != \"5a\"").Evaluate((object)5));
			Assert.AreEqual(true, new NEExpression("\"5a\" != \"5A\"").Evaluate((object)5));
			Assert.AreEqual(false, new NEExpression("\"5a\" i!= \"5a\"").Evaluate((object)5));
			Assert.AreEqual(false, new NEExpression("\"5a\" i!= \"5A\"").Evaluate((object)5));

			Assert.AreEqual("5", new NEExpression("[0].\"value\"").Evaluate(new ExpressionDotTest(5)).ToString());

			Assert.AreEqual(typeof(ExpressionDotTest).FullName, new NEExpression("Type([0]).\"FullName\"").Evaluate(new ExpressionDotTest(5)).ToString());

			Assert.AreEqual(true, new NEExpression("ValidRE([0])").Evaluate(@"\d+"));
			Assert.AreEqual(false, new NEExpression("ValidRE([0])").Evaluate(@"["));

			Assert.AreEqual("15.5", new NEExpression("+").Evaluate(1, 2, 3, 4, 5.5).ToString());
			Assert.AreEqual(true, new NEExpression("||").Evaluate(false, false, true, false));
			Assert.AreEqual("120", new NEExpression("*").Evaluate(4, 5, 6).ToString());

			Assert.AreEqual("5", new NEExpression("([0] || [1]) ? [2] : [3]").Evaluate(false, true, 5, 6).ToString());

			Assert.AreEqual("ICanJoinStrings", new NEExpression("+").Evaluate("I", "Can", null, "Join", "Strings").ToString());

			Assert.AreEqual("[0]5+7 is 12", new NEExpression("StrFormat(\"[0]{0}+{1} is {2}\", [0], [1], [0] + [1])").Evaluate(5, 7).ToString());

			Assert.AreEqual("Test", new NEExpression("StrFormat(\"Test\")").Evaluate().ToString());
			Assert.AreEqual("Test 5", new NEExpression("StrFormat(\"Test {0}\", 5)").Evaluate().ToString());
			Assert.AreEqual("Test 5 7", new NEExpression("StrFormat(\"Test {0} {1}\", 5, 7)").Evaluate().ToString());

			Assert.AreEqual(false, new NEExpression("!true").Evaluate());
			Assert.AreEqual(true, new NEExpression("![0]").Evaluate(false));
			Assert.AreEqual("-4", new NEExpression("-4").Evaluate().ToString());

			Assert.AreEqual(typeof(byte), new NEExpression("Type([0])").Evaluate((byte)0));

			Assert.AreEqual("3931877116", new NEExpression("0xdeadbeef + [0]").Evaluate(0x0badf00d).ToString());

			Assert.AreEqual("2", new NEExpression("Min(3,4,2)").Evaluate().ToString());
			Assert.AreEqual("4", new NEExpression("Max(3,4,2)").Evaluate().ToString());

			Assert.AreEqual("3.14159265358979", new NEExpression("pi").Evaluate().ToString());
			Assert.AreEqual("2.71828182845905", new NEExpression("e").Evaluate().ToString());
			Assert.AreEqual("i", new NEExpression("i").Evaluate().ToString());

			Assert.AreEqual("-2", new NEExpression("i*2*i").Evaluate().ToString());

			Assert.AreEqual("120", new NEExpression("5!").Evaluate().ToString());

			Assert.AreEqual("Four hundred eleven billion forty five thousand three hundred twelve", new NEExpression("towords(411000045312)").Evaluate().ToString());
			Assert.AreEqual("Zero", new NEExpression("towords(0)").Evaluate().ToString());
			Assert.AreEqual("Negative five", new NEExpression("towords(-5)").Evaluate().ToString());

			Assert.AreEqual("554212019", new NEExpression("fromwords(\" five -hundred-fifty-four		 million, two hundred twelve thousand, nineteen  \")").Evaluate().ToString());
			Assert.AreEqual("5110000", new NEExpression("fromwords(\"5.11 million\")").Evaluate().ToString());
			Assert.AreEqual("411000045312", new NEExpression("fromwords(\"Four hundred eleven billion forty five thousand three hundred twelve\")").Evaluate().ToString());
			Assert.AreEqual("0", new NEExpression("fromwords(\"Zero\")").Evaluate().ToString());
			Assert.AreEqual("-5", new NEExpression("fromwords(\"Negative five\")").Evaluate().ToString());

			Assert.AreEqual("5", new NEExpression("gcf(30,20,15)").Evaluate().ToString());
			Assert.AreEqual("60", new NEExpression("lcm(30,20,15)").Evaluate().ToString());
			Assert.AreEqual("1/5", new NEExpression("reduce(20,100)").Evaluate().ToString());

			Assert.AreEqual("11 m/s", new NEExpression("5.5 m/(s^2)/s*s *  2 s").Evaluate().ToString());
			Assert.AreEqual("1440 minutes", new NEExpression("1 day => minutes").Evaluate().ToString());
			Assert.AreEqual("17168.481792 m^2", new NEExpression("5 miles * 7 feet => m^2").Evaluate().ToString());
			Assert.AreEqual("5 cm^3", new NEExpression("5 ml => cm^3").Evaluate().ToString());
			Assert.AreEqual("5280 feet", new NEExpression("1 mile => feet").Evaluate().ToString());
			Assert.AreEqual("147197952000 feet^3", new NEExpression("1 mile^3 => feet^3").Evaluate().ToString());
			Assert.AreEqual("328.15 K", new NEExpression("55 degc => K").Evaluate().ToString());
			Assert.AreEqual("55 degc", new NEExpression("328.15 K => degc").Evaluate().ToString());
			Assert.AreEqual("15 degc", new NEExpression("59 degf => degc").Evaluate().ToString());
			Assert.AreEqual("59 degf", new NEExpression("15 degc => degf").Evaluate().ToString());
			Assert.AreEqual("26 in", new NEExpression("2 in + 2 ft").Evaluate().ToString());
			Assert.AreEqual(true, new NEExpression("5 km = 5000000 mm").Evaluate());
			Assert.AreEqual("5000 kg*m^2/s^2", new NEExpression("5 kJ => SI").Evaluate().ToString());
			Assert.AreEqual("1E-06 m^3", new NEExpression("1 mL => SI").Evaluate().ToString());
			Assert.AreEqual("1000000 s^-1", new NEExpression("1 MHz => SI").Evaluate().ToString());
			Assert.AreEqual("10 W", new NEExpression("10 J/s => Simple").Evaluate().ToString());

			Assert.AreEqual("-1", new NEExpression("cos(pi)").Evaluate().ToString());
			Assert.AreEqual("-1", new NEExpression("cos(pi rad)").Evaluate().ToString());
			Assert.AreEqual("-1", new NEExpression("cos(180 deg)").Evaluate().ToString());

			Assert.AreEqual("3.14159265358979 rad", new NEExpression("acos(-1)").Evaluate().ToString());
			Assert.AreEqual("180 deg", new NEExpression("acos(-1) => deg").Evaluate().ToString());

			var variables = new NEVariables(
				NEVariable.Constant("x", "", () => 0xdeadbeef),
				NEVariable.Constant("y", "", () => 0x0badf00d),
				NEVariable.Constant("z", "", () => 0x0defaced)
			);
			var expr = new NEExpression("x - y + [0]");
			var vars = expr.Variables;
			Assert.AreEqual(2, vars.Count);
			Assert.IsTrue(vars.Contains("x"));
			Assert.IsTrue(vars.Contains("y"));
			Assert.AreEqual("7816989104", expr.EvaluateRow(variables, 0xfeedface).ToString());

			CheckDates();
		}

		void CheckDates()
		{
			var now = DateTimeOffset.Now;
			var utcNow = DateTimeOffset.UtcNow;
			var partOffset = TimeSpan.FromHours(-6.5);
			var utcOffset = utcNow.Offset;

			// Check time only formats
			Assert.AreEqual("'22:45:00'", new NEExpression("'22:45'").Evaluate().ToString());
			Assert.AreEqual("'22:45:00'", new NEExpression("'10:45pm'").Evaluate().ToString());
			Assert.AreEqual("'22:45:05'", new NEExpression("'22:45:05'").Evaluate().ToString());
			Assert.AreEqual("'22:45:05'", new NEExpression("'10:45:05  PM'").Evaluate().ToString());
			Assert.AreEqual("'22:45:05.1'", new NEExpression("'22:45:05.100'").Evaluate().ToString());
			Assert.AreEqual("'22:45:05.01'", new NEExpression("'10:45:05.01  PM'").Evaluate().ToString());
			Assert.AreEqual("'123:22:45:05.01'", new NEExpression("'123:10:45:05.01  PM'").Evaluate().ToString());

			// Check date only formats
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(now.Year, 8, 24)).ToString("o")}'", new NEExpression("'8/24'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(2014, 8, 24)).ToString("o")}'", new NEExpression("'2014-08-24'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(2014, 8, 24)).ToString("o")}'", new NEExpression("'2014/8/24'").Evaluate().ToString());

			// Check date/time only formats
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 0)).ToString("o")}'", new NEExpression("'2014-08-24 22:45'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 0)).ToString("o")}'", new NEExpression("'2014/8/24 22:45'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 0)).ToString("o")}'", new NEExpression("'2014-08-24 10:45pm'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 0)).ToString("o")}'", new NEExpression("'2014/8/24 10:45pm'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5)).ToString("o")}'", new NEExpression("'2014-08-24 10:45:05  PM'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5, 100)).ToString("o")}'", new NEExpression("'2014/8/24 22:45:05.100'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5)).ToString("o")}'", new NEExpression("'2014-08-24 22:45:05'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5)).ToString("o")}'", new NEExpression("'2014/8/24 10:45:05  PM'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5, 10)).ToString("o")}'", new NEExpression("'2014-08-24 10:45:05.01  PM'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5)).ToString("o")}'", new NEExpression("'2014/8/24 22:45:05'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5, 100)).ToString("o")}'", new NEExpression("'2014-08-24 22:45:05.100'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5, 10)).ToString("o")}'", new NEExpression("'2014/8/24 10:45:05.01  PM'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 0)).ToString("o")}'", new NEExpression("'2014-08-24 22:45-6'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 0)).ToString("o")}'", new NEExpression("'2014/8/24 22:45-6'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 0)).ToString("o")}'", new NEExpression("'2014-08-24 10:45-6pm'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 0)).ToString("o")}'", new NEExpression("'2014/8/24 10:45-6pm'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5)).ToString("o")}'", new NEExpression("'2014-08-24 10:45:05-6  PM'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5, 100)).ToString("o")}'", new NEExpression("'2014/8/24 22:45:05.100-6'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5)).ToString("o")}'", new NEExpression("'2014-08-24 22:45:05-6'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5)).ToString("o")}'", new NEExpression("'2014/8/24 10:45:05-6  PM'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5, 10)).ToString("o")}'", new NEExpression("'2014-08-24 10:45:05.01-6  PM'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5)).ToString("o")}'", new NEExpression("'2014/8/24 22:45:05-6'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5, 100)).ToString("o")}'", new NEExpression("'2014-08-24 22:45:05.100-6'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5, 10)).ToString("o")}'", new NEExpression("'2014/8/24 10:45:05.01-6  PM'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 0)).ToString("o")}'", new NEExpression("'2014-08-24 22:45-06'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 0)).ToString("o")}'", new NEExpression("'2014/8/24 22:45-06'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 0)).ToString("o")}'", new NEExpression("'2014-08-24 10:45-06pm'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 0)).ToString("o")}'", new NEExpression("'2014/8/24 10:45-06pm'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5)).ToString("o")}'", new NEExpression("'2014-08-24 10:45:05-06  PM'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5, 100)).ToString("o")}'", new NEExpression("'2014/8/24 22:45:05.100-06'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5)).ToString("o")}'", new NEExpression("'2014-08-24 22:45:05-06'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5)).ToString("o")}'", new NEExpression("'2014/8/24 10:45:05-06  PM'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5, 10)).ToString("o")}'", new NEExpression("'2014-08-24 10:45:05.01-06  PM'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5)).ToString("o")}'", new NEExpression("'2014/8/24 22:45:05-06'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5, 100)).ToString("o")}'", new NEExpression("'2014-08-24 22:45:05.100-06'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5, 10)).ToString("o")}'", new NEExpression("'2014/8/24 10:45:05.01-06  PM'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(2014, 8, 24, 22, 45, 0, partOffset).ToString("o")}'", new NEExpression("'2014-08-24 22:45-6:30'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(2014, 8, 24, 22, 45, 0, partOffset).ToString("o")}'", new NEExpression("'2014/8/24 22:45-6:30'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(2014, 8, 24, 22, 45, 0, partOffset).ToString("o")}'", new NEExpression("'2014-08-24 10:45-6:30pm'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(2014, 8, 24, 22, 45, 0, partOffset).ToString("o")}'", new NEExpression("'2014/8/24 10:45-6:30pm'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(2014, 8, 24, 22, 45, 5, partOffset).ToString("o")}'", new NEExpression("'2014-08-24 10:45:05-6:30  PM'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(2014, 8, 24, 22, 45, 5, 100, partOffset).ToString("o")}'", new NEExpression("'2014/8/24 22:45:05.100-6:30'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(2014, 8, 24, 22, 45, 5, partOffset).ToString("o")}'", new NEExpression("'2014-08-24 22:45:05-6:30'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(2014, 8, 24, 22, 45, 5, partOffset).ToString("o")}'", new NEExpression("'2014/8/24 10:45:05-6:30  PM'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(2014, 8, 24, 22, 45, 5, 10, partOffset).ToString("o")}'", new NEExpression("'2014-08-24 10:45:05.01-6:30  PM'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(2014, 8, 24, 22, 45, 5, partOffset).ToString("o")}'", new NEExpression("'2014/8/24 22:45:05-6:30'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(2014, 8, 24, 22, 45, 5, 100, partOffset).ToString("o")}'", new NEExpression("'2014-08-24 22:45:05.100-6:30'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(2014, 8, 24, 22, 45, 5, 10, partOffset).ToString("o")}'", new NEExpression("'2014/8/24 10:45:05.01-6:30  PM'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(2014, 8, 24, 22, 45, 0, partOffset).ToString("o")}'", new NEExpression("'2014-08-24 22:45-06:30'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(2014, 8, 24, 22, 45, 0, partOffset).ToString("o")}'", new NEExpression("'2014/8/24 22:45-06:30'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(2014, 8, 24, 22, 45, 0, partOffset).ToString("o")}'", new NEExpression("'2014-08-24 10:45-06:30pm'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(2014, 8, 24, 22, 45, 0, partOffset).ToString("o")}'", new NEExpression("'2014/8/24 10:45-06:30pm'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(2014, 8, 24, 22, 45, 5, partOffset).ToString("o")}'", new NEExpression("'2014-08-24 10:45:05-06:30  PM'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(2014, 8, 24, 22, 45, 5, 100, partOffset).ToString("o")}'", new NEExpression("'2014/8/24 22:45:05.100-06:30'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(2014, 8, 24, 22, 45, 5, partOffset).ToString("o")}'", new NEExpression("'2014-08-24 22:45:05-06:30'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(2014, 8, 24, 22, 45, 5, partOffset).ToString("o")}'", new NEExpression("'2014/8/24 10:45:05-06:30  PM'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(2014, 8, 24, 22, 45, 5, 10, partOffset).ToString("o")}'", new NEExpression("'2014-08-24 10:45:05.01-06:30  PM'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(2014, 8, 24, 22, 45, 5, partOffset).ToString("o")}'", new NEExpression("'2014/8/24 22:45:05-06:30'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(2014, 8, 24, 22, 45, 5, 100, partOffset).ToString("o")}'", new NEExpression("'2014-08-24 22:45:05.100-06:30'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(2014, 8, 24, 22, 45, 5, 10, partOffset).ToString("o")}'", new NEExpression("'2014/8/24 10:45:05.01-06:30  PM'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(2014, 8, 24, 22, 45, 0, utcOffset).ToString("o")}'", new NEExpression("'2014-08-24 22:45Z'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(2014, 8, 24, 22, 45, 0, utcOffset).ToString("o")}'", new NEExpression("'2014/8/24 22:45Z'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(2014, 8, 24, 22, 45, 0, utcOffset).ToString("o")}'", new NEExpression("'2014-08-24 10:45Z pm'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(2014, 8, 24, 22, 45, 0, utcOffset).ToString("o")}'", new NEExpression("'2014/8/24 10:45Z pm'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(2014, 8, 24, 22, 45, 5, utcOffset).ToString("o")}'", new NEExpression("'2014-08-24 10:45:05Z  PM'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(2014, 8, 24, 22, 45, 5, 100, utcOffset).ToString("o")}'", new NEExpression("'2014/8/24 22:45:05.100Z'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(2014, 8, 24, 22, 45, 5, utcOffset).ToString("o")}'", new NEExpression("'2014-08-24 22:45:05Z'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(2014, 8, 24, 22, 45, 5, utcOffset).ToString("o")}'", new NEExpression("'2014/8/24 10:45:05Z  PM'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(2014, 8, 24, 22, 45, 5, 10, utcOffset).ToString("o")}'", new NEExpression("'2014-08-24 10:45:05.01Z  PM'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(2014, 8, 24, 22, 45, 5, utcOffset).ToString("o")}'", new NEExpression("'2014/8/24 22:45:05Z'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(2014, 8, 24, 22, 45, 5, 100, utcOffset).ToString("o")}'", new NEExpression("'2014-08-24 22:45:05.100Z'").Evaluate().ToString());
			Assert.AreEqual($"'{new DateTimeOffset(2014, 8, 24, 22, 45, 5, 10, utcOffset).ToString("o")}'", new NEExpression("'2014/8/24 10:45:05.01Z  PM'").Evaluate().ToString());

			// Check time math
			Assert.AreEqual("'22:50:00'", new NEExpression("'12:10' + '10:40'").Evaluate().ToString());
			Assert.AreEqual("'22:52:00'", new NEExpression("2+'22:50:00'").Evaluate().ToString());
			Assert.AreEqual("'22:50:05'", new NEExpression("'22:50:00'+5 s").Evaluate().ToString());
			Assert.AreEqual("'22:50:00.0000001'", new NEExpression("'22:50:00'+100 ns").Evaluate().ToString());
			Assert.AreEqual("'1:30:00'", new NEExpression("'12:10' - '10:40'").Evaluate().ToString());
			Assert.AreEqual("'-0:30:00'", new NEExpression("'10:10' - '10:40'").Evaluate().ToString());
			Assert.AreEqual("'22:50:00'", new NEExpression("'12:10' - '-10:40'").Evaluate().ToString());
			Assert.AreEqual("'22:50:00'", new NEExpression("'22:52:00'-2").Evaluate().ToString());
			Assert.AreEqual("'22:50:00'", new NEExpression("'22:50:05'-5 s").Evaluate().ToString());
			Assert.AreEqual("'22:50:00'", new NEExpression("'22:50:00.0000001'-100 ns").Evaluate().ToString());

			// Check date/time math
			Assert.AreEqual("'2014-07-31T00:00:00.0000000-06:00'", new NEExpression("1 + '2014/7/30'").Evaluate().ToString());
			Assert.AreEqual("'2014-07-31T00:00:00.0000000-06:00'", new NEExpression("'2014/7/30' + 1 day").Evaluate().ToString());
			Assert.AreEqual("'2014-07-30T00:00:00.0000001-06:00'", new NEExpression("'2014/7/30' + 100 ns").Evaluate().ToString());
			Assert.AreEqual("'2014-08-30T00:00:00.0000000-06:00'", new NEExpression("'2014/7/30' + 1 month").Evaluate().ToString());
			Assert.AreEqual("'2015-07-30T00:00:00.0000000-06:00'", new NEExpression("'2014/7/30' + 1 yr").Evaluate().ToString());
			Assert.AreEqual("'2014-07-30T22:50:00.0000000-06:00'", new NEExpression("'2014/7/30' + '22:50'").Evaluate().ToString());
			Assert.AreEqual("'2014-07-30T00:00:00.0000000-06:00'", new NEExpression("'2014-07-31T00:00:00.0000000-06:00' - 1").Evaluate().ToString());
			Assert.AreEqual("'2014-07-30T00:00:00.0000000-06:00'", new NEExpression("'2014-07-31T00:00:00.0000000-06:00' - 1 day").Evaluate().ToString());
			Assert.AreEqual("'2014-07-30T00:00:00.0000000-06:00'", new NEExpression("'2014-07-30T00:00:00.0000001-06:00' - 100 ns").Evaluate().ToString());
			Assert.AreEqual("'2014-07-30T00:00:00.0000000-06:00'", new NEExpression("'2014-08-30T00:00:00.0000000-06:00' - 1 month").Evaluate().ToString());
			Assert.AreEqual("'2014-07-30T00:00:00.0000000-06:00'", new NEExpression("'2015-07-30T00:00:00.0000000-06:00' - 1 yr").Evaluate().ToString());
			Assert.AreEqual("'2014-07-30T00:00:00.0000000-06:00'", new NEExpression("'2014-07-30T22:50:00.0000000-06:00' - '22:50'").Evaluate().ToString());
			Assert.AreEqual("11 days", new NEExpression("'2014-07-31' - '2014-07-20' => days").Evaluate().ToString());
		}
	}
}
