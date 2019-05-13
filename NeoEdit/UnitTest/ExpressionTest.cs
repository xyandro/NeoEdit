using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeoEdit;
using NeoEdit.Expressions;

namespace NeoEdit.UnitTest
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
			Assert.AreEqual("bcd", new NEExpression("\"abc\" t++ 1").Evaluate().ToString());
			Assert.AreEqual("25", new NEExpression("\"Z\" t--- \"A\"").Evaluate().ToString());
			Assert.AreEqual("a1", new NEExpression("\"a\" t+ 1").Evaluate().ToString());

			Assert.AreEqual("0", new NEExpression("2 - 2").Evaluate().ToString());
			Assert.AreEqual("3.1", new NEExpression("5.1 - 2").Evaluate().ToString());
			Assert.AreEqual("4.9", new NEExpression("5 - .1").Evaluate().ToString());
			Assert.AreEqual("4.8", new NEExpression("4.9 - .1").Evaluate().ToString());
			Assert.AreEqual("5", new NEExpression("5.1 - .1").Evaluate().ToString());
			Assert.AreEqual("a", new NEExpression("\"b\" t-- 1").Evaluate().ToString());

			Assert.AreEqual("10.8", new NEExpression("5.4 * 2").Evaluate().ToString());
			Assert.AreEqual("10.5", new NEExpression("5 * 2.1").Evaluate().ToString());
			Assert.AreEqual("okokokok", new NEExpression("\"ok\" t* 4").Evaluate().ToString());
			Assert.AreEqual("oooo", new NEExpression("\"o\" t* 4").Evaluate().ToString());

			Assert.AreEqual("2.5", new NEExpression("5 / 2").Evaluate().ToString());
			Assert.AreEqual("2", new NEExpression("5 // 2").Evaluate().ToString());
			Assert.AreEqual("2.5", new NEExpression("5.0 / 2").Evaluate().ToString());
			Assert.AreEqual("2.5", new NEExpression("5 / 2.0").Evaluate().ToString());

			Assert.AreEqual("64", new NEExpression("4 ^ 3").Evaluate().ToString());
			Assert.AreEqual("4", new NEExpression("64 ^ (1 / 3.0)").Evaluate().ToString());
			Assert.AreEqual("4.5", new NEExpression("410.0625 ^ .25").Evaluate().ToString());

			Assert.AreEqual("8", new NEExpression("2 << 2").Evaluate().ToString());
			Assert.AreEqual("1024", new NEExpression("1048576 >> 10").Evaluate().ToString());

			Assert.AreEqual("-3735928560", new NEExpression("~p0").Evaluate(new NEVariables(0xdeadbeef)).ToString());
			Assert.AreEqual("179154957", new NEExpression("&").Evaluate(new NEVariables(0xdeadbeef, 0x0badf00d)).ToString());
			Assert.AreEqual("3573567202", new NEExpression("^^").Evaluate(new NEVariables(0xdeadbeef, 0x0badf00d)).ToString());
			Assert.AreEqual("3752722159", new NEExpression("|").Evaluate(new NEVariables(0xdeadbeef, 0x0badf00d)).ToString());

			Assert.AreEqual(true, new NEExpression("TRUE").Evaluate());
			Assert.AreEqual(false, new NEExpression("False").Evaluate());

			Assert.AreEqual("7.1", new NEExpression("5.0 + 2.1").Evaluate().ToString());
			Assert.AreEqual("14.1", new NEExpression("3.6 + 2.1 * 5.0").Evaluate().ToString());
			Assert.AreEqual("28.5", new NEExpression("(3.6 + 2.1) * 5.0").Evaluate().ToString());
			Assert.AreEqual("12.1", new NEExpression("p0 + p1").Evaluate(new NEVariables(5.4, 6.7)).ToString());

			Assert.AreEqual(true, new NEExpression("p0 is \"Int32\"").Evaluate(new NEVariables(5)));

			Assert.AreEqual(true, new NEExpression("\"5a\" t== \"5a\"").Evaluate());
			Assert.AreEqual(false, new NEExpression("\"5a\" t== \"5A\"").Evaluate());
			Assert.AreEqual(true, new NEExpression("\"5a\" ti== \"5a\"").Evaluate());
			Assert.AreEqual(true, new NEExpression("\"5a\" ti== \"5A\"").Evaluate());
			Assert.AreEqual(false, new NEExpression("\"5a\" t!= \"5a\"").Evaluate());
			Assert.AreEqual(true, new NEExpression("\"5a\" t!= \"5A\"").Evaluate());
			Assert.AreEqual(false, new NEExpression("\"5a\" ti!= \"5a\"").Evaluate());
			Assert.AreEqual(false, new NEExpression("\"5a\" ti!= \"5A\"").Evaluate());

			Assert.AreEqual("5", new NEExpression("p0.\"value\"").Evaluate(new NEVariables(new ExpressionDotTest(5))).ToString());

			Assert.AreEqual(typeof(ExpressionDotTest).FullName, new NEExpression("Type(p0).\"FullName\"").Evaluate(new NEVariables(new ExpressionDotTest(5))).ToString());

			Assert.AreEqual(true, new NEExpression("ValidRE(p0)").Evaluate(new NEVariables(@"\d+")));
			Assert.AreEqual(false, new NEExpression("ValidRE(p0)").Evaluate(new NEVariables(@"[")));

			Assert.AreEqual("15.5", new NEExpression("+").Evaluate(new NEVariables(1, 2, 3, 4, 5.5)).ToString());
			Assert.AreEqual(true, new NEExpression("||").Evaluate(new NEVariables(false, false, true, false)));
			Assert.AreEqual("120", new NEExpression("*").Evaluate(new NEVariables(4, 5, 6)).ToString());

			Assert.AreEqual("5", new NEExpression("(p0 || p1) ? p2 : p3").Evaluate(new NEVariables(false, true, 5, 6)).ToString());

			Assert.AreEqual("ICanJoinStrings", new NEExpression("t+").Evaluate(new NEVariables("I", "Can", null, "Join", "Strings")).ToString());

			Assert.AreEqual("a\\\'\"\0\a\b\f\n\r\t\v\x1\x12\x123\x1234\u1234\U00001234\U0001d161", new NEExpression(@"""a"" t+ ""\\"" t+ ""\'"" t+ ""\"""" t+ ""\0"" t+ ""\a"" t+ ""\b"" t+ ""\f"" t+ ""\n"" t+ ""\r"" t+ ""\t"" t+ ""\v"" t+ ""\x1"" t+ ""\x12"" t+ ""\x123"" t+ ""\x1234"" t+ ""\u1234"" t+ ""\U00001234"" t+ ""\U0001d161""").Evaluate().ToString());
			Assert.AreEqual("", new NEExpression("\"\"").Evaluate().ToString());
			Assert.AreEqual(" Slash: \\ Quote: \' Double: \" Null: \0 Alert: \a Backspace: \b Form feed: \f New line: \n Carriage return:\r Tab: \t Vertical quote: \v Hex1: \x1 Hex2: \x12 Hex3: \x123 Hex4: \x1234 Unicode4: \u1234 Unicode8: \U00001234 Unicode8: \U0001d161 ", new NEExpression(@""" Slash: \\ Quote: \' Double: \"" Null: \0 Alert: \a Backspace: \b Form feed: \f New line: \n Carriage return:\r Tab: \t Vertical quote: \v Hex1: \x1 Hex2: \x12 Hex3: \x123 Hex4: \x1234 Unicode4: \u1234 Unicode8: \U00001234 Unicode8: \U0001d161 """).Evaluate().ToString());
			Assert.AreEqual("", new NEExpression("@\"\"").Evaluate().ToString());
			Assert.AreEqual(@"This \is "" my string", new NEExpression(@"@""This \is """" my string""").Evaluate().ToString());
			Assert.AreEqual("", new NEExpression("$\"\"").Evaluate().ToString());
			Assert.AreEqual("{2 + 2 = 4}\r\n", new NEExpression(@"$""{{\u0032 + 2 = {2 + 2}}}\r\n""").Evaluate().ToString());
			Assert.AreEqual(@"\n ""{2 + 2} \= 4"" \n", new NEExpression(@"@$""\n """"{{2 + 2}} \= {2 + 2}"""" \n""").Evaluate().ToString());
			Assert.AreEqual(@"\n ""{2 + 2} \= 4"" \n", new NEExpression(@"$@""\n """"{{2 + 2}} \= {2 + 2}"""" \n""").Evaluate().ToString());
			Assert.AreEqual("", new NEExpression("@$\"\"").Evaluate().ToString());
			Assert.AreEqual("", new NEExpression("$@\"\"").Evaluate().ToString());

			Assert.AreEqual("p05+7 is 12", new NEExpression("StrFormat(\"p0{0}+{1} is {2}\", p0, p1, p0 + p1)").Evaluate(new NEVariables(5, 7)).ToString());

			Assert.AreEqual("Test", new NEExpression("StrFormat(\"Test\")").Evaluate().ToString());
			Assert.AreEqual("Test 5", new NEExpression("StrFormat(\"Test {0}\", 5)").Evaluate().ToString());
			Assert.AreEqual("Test 5 7", new NEExpression("StrFormat(\"Test {0} {1}\", 5, 7)").Evaluate().ToString());

			Assert.AreEqual(false, new NEExpression("!true").Evaluate());
			Assert.AreEqual(true, new NEExpression("!p0").Evaluate(new NEVariables(false)));
			Assert.AreEqual("-4", new NEExpression("-4").Evaluate().ToString());

			Assert.AreEqual(typeof(byte), new NEExpression("Type(p0)").Evaluate(new NEVariables((byte)0)));

			Assert.AreEqual("3931877116", new NEExpression("0xdeadbeef + p0").Evaluate(new NEVariables(0x0badf00d)).ToString());

			Assert.AreEqual("2", new NEExpression("Min(3,4,2)").Evaluate().ToString());
			Assert.AreEqual("4", new NEExpression("Max(3,4,2)").Evaluate().ToString());

			Assert.AreEqual("16", new NEExpression("len(\"1125899906842624\")").Evaluate().ToString());

			Assert.AreEqual("8", new NEExpression("multiple(8,8)").Evaluate().ToString());
			Assert.AreEqual("16", new NEExpression("multiple(9,8)").Evaluate().ToString());
			Assert.AreEqual("8.4", new NEExpression("multiple(1.1,8.4)").Evaluate().ToString());
			Assert.AreEqual("16.8", new NEExpression("multiple(9.5,8.4)").Evaluate().ToString());
			Assert.AreEqual("3 foot", new NEExpression("multiple(30 in,1 foot)").Evaluate().ToString());

			Assert.AreEqual("3.14159265358979", new NEExpression("pi").Evaluate().ToString());
			Assert.AreEqual("2.71828182845905", new NEExpression("e").Evaluate().ToString());

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
			Assert.AreEqual("25 $/lessons", new NEExpression("1000$ / 40 lessons").Evaluate().ToString());

			Assert.AreEqual("25", new NEExpression("25").Evaluate(unit: "bytes").ToString());
			Assert.AreEqual("25", new NEExpression("25 bytes").Evaluate(unit: "bytes").ToString());
			Assert.AreEqual("25600", new NEExpression("25 kb").Evaluate(unit: "bytes").ToString());
			Assert.AreEqual("26214400", new NEExpression("25 mb").Evaluate(unit: "bytes").ToString());
			Assert.AreEqual("25600", new NEExpression("25 mb").Evaluate(unit: "kb").ToString());

			Assert.AreEqual("-1", new NEExpression("cos(pi)").Evaluate().ToString());
			Assert.AreEqual("-1", new NEExpression("cos(pi rad)").Evaluate().ToString());
			Assert.AreEqual("-1", new NEExpression("cos(180 deg)").Evaluate().ToString());

			Assert.AreEqual("3.14159265358979 rad", new NEExpression("acos(-1)").Evaluate().ToString());
			Assert.AreEqual("180 deg", new NEExpression("acos(-1) => deg").Evaluate().ToString());

			Assert.AreEqual("0", new NEExpression("factor(0)").Evaluate().ToString());
			Assert.AreEqual("-1*2*2*3*5*7*11", new NEExpression("factor(-4620)").Evaluate().ToString());

			Assert.AreEqual(true, new NEExpression("valideval(\"(5+2)\")").Evaluate());
			Assert.AreEqual(false, new NEExpression("valideval(\"(5+)2\")").Evaluate());

			var values = new NEExpression("random(2,10)").EvaluateList<int>(null, 1000).Distinct().OrderBy().ToList();
			Assert.IsTrue(values.Count == 9);

			var miscVars = new NEVariables(
				NEVariable.Constant("x", "", () => 0xdeadbeef),
				NEVariable.Constant("y", "", () => 0x0badf00d),
				NEVariable.Constant("z", "", () => 0x0defaced)
			);
			var expr = new NEExpression("x - y + 0xfeedface");
			var vars = expr.Variables;
			Assert.AreEqual(2, vars.Count);
			Assert.IsTrue(vars.Contains("x"));
			Assert.IsTrue(vars.Contains("y"));
			Assert.AreEqual("7816989104", expr.Evaluate(miscVars).ToString());

			CheckDates();
		}

		void CheckDates()
		{
			var now = DateTimeOffset.Now;
			var utcNow = DateTimeOffset.UtcNow;
			var partOffset = TimeSpan.FromHours(-6.5);
			var utcOffset = utcNow.Offset;

			// Check time only formats
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(now.Year, now.Month, now.Day, 22, 45, 0)).ToString("o")}", new NEExpression("todate(fromdate(\"22:45\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(now.Year, now.Month, now.Day, 22, 45, 0)).ToString("o")}", new NEExpression("todate(fromdate(\"10:45pm\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(now.Year, now.Month, now.Day, 22, 45, 5)).ToString("o")}", new NEExpression("todate(fromdate(\"22:45:05\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(now.Year, now.Month, now.Day, 22, 45, 5)).ToString("o")}", new NEExpression("todate(fromdate(\"10:45:05  PM\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(now.Year, now.Month, now.Day, 22, 45, 5, 100)).ToString("o")}", new NEExpression("todate(fromdate(\"22:45:05.100\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(now.Year, now.Month, now.Day, 22, 45, 5, 10)).ToString("o")}", new NEExpression("todate(fromdate(\"10:45:05.01  PM\"))").Evaluate().ToString());

			// Check date only formats
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(now.Year, 8, 24)).ToString("o")}", new NEExpression("todate(fromdate(\"8/24\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(2014, 8, 24)).ToString("o")}", new NEExpression("todate(fromdate(\"2014-08-24\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(2014, 8, 24)).ToString("o")}", new NEExpression("todate(fromdate(\"2014/8/24\"))").Evaluate().ToString());

			// Check date/time only formats
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 0)).ToString("o")}", new NEExpression("todate(fromdate(\"2014-08-24 22:45\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 0)).ToString("o")}", new NEExpression("todate(fromdate(\"2014/8/24 22:45\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 0)).ToString("o")}", new NEExpression("todate(fromdate(\"2014-08-24 10:45pm\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 0)).ToString("o")}", new NEExpression("todate(fromdate(\"2014/8/24 10:45pm\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5)).ToString("o")}", new NEExpression("todate(fromdate(\"2014-08-24 10:45:05  PM\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5, 100)).ToString("o")}", new NEExpression("todate(fromdate(\"2014/8/24 22:45:05.100\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5)).ToString("o")}", new NEExpression("todate(fromdate(\"2014-08-24 22:45:05\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5)).ToString("o")}", new NEExpression("todate(fromdate(\"2014/8/24 10:45:05  PM\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5, 10)).ToString("o")}", new NEExpression("todate(fromdate(\"2014-08-24 10:45:05.01  PM\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5)).ToString("o")}", new NEExpression("todate(fromdate(\"2014/8/24 22:45:05\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5, 100)).ToString("o")}", new NEExpression("todate(fromdate(\"2014-08-24 22:45:05.100\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5, 10)).ToString("o")}", new NEExpression("todate(fromdate(\"2014/8/24 10:45:05.01  PM\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 0)).ToString("o")}", new NEExpression("todate(fromdate(\"2014-08-24 22:45-6\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 0)).ToString("o")}", new NEExpression("todate(fromdate(\"2014/8/24 22:45-6\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 0)).ToString("o")}", new NEExpression("todate(fromdate(\"2014-08-24 10:45-6pm\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 0)).ToString("o")}", new NEExpression("todate(fromdate(\"2014/8/24 10:45-6pm\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5)).ToString("o")}", new NEExpression("todate(fromdate(\"2014-08-24 10:45:05-6  PM\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5, 100)).ToString("o")}", new NEExpression("todate(fromdate(\"2014/8/24 22:45:05.100-6\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5)).ToString("o")}", new NEExpression("todate(fromdate(\"2014-08-24 22:45:05-6\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5)).ToString("o")}", new NEExpression("todate(fromdate(\"2014/8/24 10:45:05-6  PM\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5, 10)).ToString("o")}", new NEExpression("todate(fromdate(\"2014-08-24 10:45:05.01-6  PM\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5)).ToString("o")}", new NEExpression("todate(fromdate(\"2014/8/24 22:45:05-6\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5, 100)).ToString("o")}", new NEExpression("todate(fromdate(\"2014-08-24 22:45:05.100-6\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5, 10)).ToString("o")}", new NEExpression("todate(fromdate(\"2014/8/24 10:45:05.01-6  PM\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 0)).ToString("o")}", new NEExpression("todate(fromdate(\"2014-08-24 22:45-06\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 0)).ToString("o")}", new NEExpression("todate(fromdate(\"2014/8/24 22:45-06\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 0)).ToString("o")}", new NEExpression("todate(fromdate(\"2014-08-24 10:45-06pm\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 0)).ToString("o")}", new NEExpression("todate(fromdate(\"2014/8/24 10:45-06pm\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5)).ToString("o")}", new NEExpression("todate(fromdate(\"2014-08-24 10:45:05-06  PM\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5, 100)).ToString("o")}", new NEExpression("todate(fromdate(\"2014/8/24 22:45:05.100-06\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5)).ToString("o")}", new NEExpression("todate(fromdate(\"2014-08-24 22:45:05-06\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5)).ToString("o")}", new NEExpression("todate(fromdate(\"2014/8/24 10:45:05-06  PM\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5, 10)).ToString("o")}", new NEExpression("todate(fromdate(\"2014-08-24 10:45:05.01-06  PM\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5)).ToString("o")}", new NEExpression("todate(fromdate(\"2014/8/24 22:45:05-06\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5, 100)).ToString("o")}", new NEExpression("todate(fromdate(\"2014-08-24 22:45:05.100-06\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(new DateTime(2014, 8, 24, 22, 45, 5, 10)).ToString("o")}", new NEExpression("todate(fromdate(\"2014/8/24 10:45:05.01-06  PM\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(2014, 8, 24, 22, 45, 0, partOffset).ToString("o")}", new NEExpression("todate(fromdate(\"2014-08-24 22:45-6:30\"), -6.5 hours)").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(2014, 8, 24, 22, 45, 0, partOffset).ToString("o")}", new NEExpression("todate(fromdate(\"2014/8/24 22:45-6:30\"), -6.5 hours)").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(2014, 8, 24, 22, 45, 0, partOffset).ToString("o")}", new NEExpression("todate(fromdate(\"2014-08-24 10:45-6:30pm\"), -6.5 hours)").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(2014, 8, 24, 22, 45, 0, partOffset).ToString("o")}", new NEExpression("todate(fromdate(\"2014/8/24 10:45-6:30pm\"), -6.5 hours)").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(2014, 8, 24, 22, 45, 5, partOffset).ToString("o")}", new NEExpression("todate(fromdate(\"2014-08-24 10:45:05-6:30  PM\"), -6.5 hours)").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(2014, 8, 24, 22, 45, 5, 100, partOffset).ToString("o")}", new NEExpression("todate(fromdate(\"2014/8/24 22:45:05.100-6:30\"), -6.5 hours)").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(2014, 8, 24, 22, 45, 5, partOffset).ToString("o")}", new NEExpression("todate(fromdate(\"2014-08-24 22:45:05-6:30\"), -6.5 hours)").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(2014, 8, 24, 22, 45, 5, partOffset).ToString("o")}", new NEExpression("todate(fromdate(\"2014/8/24 10:45:05-6:30  PM\"), -6.5 hours)").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(2014, 8, 24, 22, 45, 5, 10, partOffset).ToString("o")}", new NEExpression("todate(fromdate(\"2014-08-24 10:45:05.01-6:30  PM\"), -6.5 hours)").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(2014, 8, 24, 22, 45, 5, partOffset).ToString("o")}", new NEExpression("todate(fromdate(\"2014/8/24 22:45:05-6:30\"), -6.5 hours)").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(2014, 8, 24, 22, 45, 5, 100, partOffset).ToString("o")}", new NEExpression("todate(fromdate(\"2014-08-24 22:45:05.100-6:30\"), -6.5 hours)").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(2014, 8, 24, 22, 45, 5, 10, partOffset).ToString("o")}", new NEExpression("todate(fromdate(\"2014/8/24 10:45:05.01-6:30  PM\"), -6.5 hours)").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(2014, 8, 24, 22, 45, 0, partOffset).ToString("o")}", new NEExpression("todate(fromdate(\"2014-08-24 22:45-06:30\"), -6.5 hours)").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(2014, 8, 24, 22, 45, 0, partOffset).ToString("o")}", new NEExpression("todate(fromdate(\"2014/8/24 22:45-06:30\"), -6.5 hours)").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(2014, 8, 24, 22, 45, 0, partOffset).ToString("o")}", new NEExpression("todate(fromdate(\"2014-08-24 10:45-06:30pm\"), -6.5 hours)").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(2014, 8, 24, 22, 45, 0, partOffset).ToString("o")}", new NEExpression("todate(fromdate(\"2014/8/24 10:45-06:30pm\"), -6.5 hours)").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(2014, 8, 24, 22, 45, 5, partOffset).ToString("o")}", new NEExpression("todate(fromdate(\"2014-08-24 10:45:05-06:30  PM\"), -6.5 hours)").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(2014, 8, 24, 22, 45, 5, 100, partOffset).ToString("o")}", new NEExpression("todate(fromdate(\"2014/8/24 22:45:05.100-06:30\"), -6.5 hours)").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(2014, 8, 24, 22, 45, 5, partOffset).ToString("o")}", new NEExpression("todate(fromdate(\"2014-08-24 22:45:05-06:30\"), -6.5 hours)").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(2014, 8, 24, 22, 45, 5, partOffset).ToString("o")}", new NEExpression("todate(fromdate(\"2014/8/24 10:45:05-06:30  PM\"), -6.5 hours)").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(2014, 8, 24, 22, 45, 5, 10, partOffset).ToString("o")}", new NEExpression("todate(fromdate(\"2014-08-24 10:45:05.01-06:30  PM\"), -6.5 hours)").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(2014, 8, 24, 22, 45, 5, partOffset).ToString("o")}", new NEExpression("todate(fromdate(\"2014/8/24 22:45:05-06:30\"), -6.5 hours)").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(2014, 8, 24, 22, 45, 5, 100, partOffset).ToString("o")}", new NEExpression("todate(fromdate(\"2014-08-24 22:45:05.100-06:30\"), -6.5 hours)").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(2014, 8, 24, 22, 45, 5, 10, partOffset).ToString("o")}", new NEExpression("todate(fromdate(\"2014/8/24 10:45:05.01-06:30  PM\"), -6.5 hours)").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(2014, 8, 24, 22, 45, 0, utcOffset).ToString("o")}", new NEExpression("toutcdate(fromdate(\"2014-08-24 22:45Z\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(2014, 8, 24, 22, 45, 0, utcOffset).ToString("o")}", new NEExpression("toutcdate(fromdate(\"2014/8/24 22:45Z\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(2014, 8, 24, 22, 45, 0, utcOffset).ToString("o")}", new NEExpression("toutcdate(fromdate(\"2014-08-24 10:45Z pm\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(2014, 8, 24, 22, 45, 0, utcOffset).ToString("o")}", new NEExpression("toutcdate(fromdate(\"2014/8/24 10:45Z pm\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(2014, 8, 24, 22, 45, 5, utcOffset).ToString("o")}", new NEExpression("toutcdate(fromdate(\"2014-08-24 10:45:05Z  PM\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(2014, 8, 24, 22, 45, 5, 100, utcOffset).ToString("o")}", new NEExpression("toutcdate(fromdate(\"2014/8/24 22:45:05.100Z\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(2014, 8, 24, 22, 45, 5, utcOffset).ToString("o")}", new NEExpression("toutcdate(fromdate(\"2014-08-24 22:45:05Z\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(2014, 8, 24, 22, 45, 5, utcOffset).ToString("o")}", new NEExpression("toutcdate(fromdate(\"2014/8/24 10:45:05Z  PM\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(2014, 8, 24, 22, 45, 5, 10, utcOffset).ToString("o")}", new NEExpression("toutcdate(fromdate(\"2014-08-24 10:45:05.01Z  PM\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(2014, 8, 24, 22, 45, 5, utcOffset).ToString("o")}", new NEExpression("toutcdate(fromdate(\"2014/8/24 22:45:05Z\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(2014, 8, 24, 22, 45, 5, 100, utcOffset).ToString("o")}", new NEExpression("toutcdate(fromdate(\"2014-08-24 22:45:05.100Z\"))").Evaluate().ToString());
			Assert.AreEqual($"{new DateTimeOffset(2014, 8, 24, 22, 45, 5, 10, utcOffset).ToString("o")}", new NEExpression("toutcdate(fromdate(\"2014/8/24 10:45:05.01Z  PM\"))").Evaluate().ToString());

			// Check time math
			Assert.AreEqual("90 min", new NEExpression("fromdate(\"12:10\") - fromdate(\"10:40\") => min").Evaluate().ToString());
			Assert.AreEqual("-0.5 hour", new NEExpression("fromdate(\"10:10\") - fromdate(\"10:40\") => hour").Evaluate().ToString());

			// Check date/time math
			Assert.AreEqual("2014-07-31T00:00:00.0000000-06:00", new NEExpression("todate(1 day + fromdate(\"2014/7/30\"))").Evaluate().ToString());
			Assert.AreEqual("2014-07-31T00:00:00.0000000-06:00", new NEExpression("todate(fromdate(\"2014/7/30\") + 1 day)").Evaluate().ToString());
			Assert.AreEqual("2014-07-30T00:00:00.0000001-06:00", new NEExpression("todate(fromdate(\"2014/7/30\") + 100 ns)").Evaluate().ToString());
			Assert.AreEqual("2014-07-30T00:00:00.0000000-06:00", new NEExpression("todate(fromdate(\"2014-07-31T00:00:00.0000000-06:00\") - 1 day)").Evaluate().ToString());
			Assert.AreEqual("2014-07-30T00:00:00.0000000-06:00", new NEExpression("todate(fromdate(\"2014-07-30T00:00:00.0000001-06:00\") - 100 ns)").Evaluate().ToString());
			Assert.AreEqual("11 days", new NEExpression("fromdate(\"2014-07-31\") - fromdate(\"2014-07-20\") => days").Evaluate().ToString());
		}
	}
}
