using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace NeoEdit.Common.Expressions
{
	public class ExpressionResult : IComparable
	{
		public object Value { get; private set; }
		public ExpressionUnits Units { get; }

		public ExpressionResult(object value) : this(value, new ExpressionUnits()) { }
		public ExpressionResult(object value, string unit, int exp = 1) : this(value, new ExpressionUnits(unit, exp)) { }

		public ExpressionResult(object value, ExpressionUnits units)
		{
			Value = value;
			Units = units;

			if ((Value != null) && (Value.GetType().FullName == "MS.Internal.NamedObject") && (Value.ToString() == "{DependencyProperty.UnsetValue}"))
				Value = null;

			if (Value is Complex)
			{
				var complex = RoundComplex((Complex)Value);
				Value = complex.Imaginary == 0 ? complex.Real : complex;
			}

			if (Value is double)
			{
				var d = (double)Value;
				if ((!Double.IsInfinity(d)) && (!Double.IsNaN(d)) && (Math.Floor(d) == d))
					Value = new BigInteger(d);
			}

			if (Value is BigInteger)
			{
				var bigint = (BigInteger)Value;
				if ((bigint >= long.MinValue) && (bigint <= long.MaxValue))
					Value = (long)bigint;
			}
		}

		bool IsInteger => (Value is sbyte) || (Value is byte) || (Value is short) || (Value is ushort) || (Value is int) || (Value is uint) || (Value is long) || (Value is ulong) || (Value is BigInteger);
		bool IsFloat => (IsInteger) || (Value is float) || (Value is double) || (Value is decimal);
		bool IsComplex => (IsFloat) || (Value is Complex);
		bool IsCharacter => Value is char;
		bool IsString => (IsCharacter) || (Value is string);
		bool IsDateTime => (Value is DateTime) || (Value is DateTimeOffset);
		bool IsTimeSpan => (Value is TimeSpan);
		public bool True => (Value is bool) && ((bool)Value);

		static Complex RoundComplex(Complex complex)
		{
			var real = Math.Round(complex.Real, 10);
			var imaginary = Math.Round(complex.Imaginary, 10);
			if (Math.Abs(real) < 1e-10)
				real = 0;
			if (Math.Abs(imaginary) < 1e-10)
				imaginary = 0;
			return new Complex(real, imaginary);
		}

		bool GetBool
		{
			get
			{
				if (Value == null)
					throw new Exception("NULL value");
				return (bool)Convert.ChangeType(Value, typeof(bool));
			}
		}

		BigInteger GetInteger
		{
			get
			{
				if (Value == null)
					throw new Exception("NULL value");
				if (Value is BigInteger)
					return (BigInteger)Value;
				if (Value is Complex)
				{
					var complex = (Complex)Value;
					if (complex.Imaginary != 0)
						throw new Exception("Can't convert complex to double");
					Value = complex.Real;
				}
				return new BigInteger((long)Convert.ChangeType(Value, typeof(long)));
			}
		}

		double GetFloat
		{
			get
			{
				if (Value == null)
					throw new Exception("NULL value");
				if (Value is Complex)
				{
					var complex = (Complex)Value;
					if (complex.Imaginary != 0)
						throw new Exception("Can't convert complex to double");
					return complex.Real;
				}
				if (Value is BigInteger)
					return (double)(BigInteger)Value;
				return (double)Convert.ChangeType(Value, typeof(double));
			}
		}

		Complex GetComplex
		{
			get
			{
				if (Value == null)
					throw new Exception("NULL value");
				if (Value is Complex)
					return (Complex)Value;
				if (Value is BigInteger)
					return (Complex)(BigInteger)Value;
				return new Complex((double)Convert.ChangeType(Value, typeof(double)), 0);
			}
		}

		char GetChar
		{
			get
			{
				if (Value == null)
					throw new Exception("NULL value");
				return (char)Convert.ChangeType(Value, typeof(char));
			}
		}

		public string GetString
		{
			get
			{
				if (Value == null)
					return "";
				if (Value is Complex)
				{
					var complex = (Complex)Value;
					if (complex.Imaginary == 0)
						return complex.Real.ToString();

					var result = "";
					if (complex.Real != 0)
						result += complex.Real.ToString();
					if ((complex.Real != 0) || (complex.Imaginary < 0))
						result += complex.Imaginary < 0 ? "-" : "+";
					var absImag = Math.Abs(complex.Imaginary);
					if (absImag != 1)
						result += $"{absImag}*";
					result += "i";
					return result;
				}
				return Value.ToString();
			}
		}

		public DateTimeOffset GetDateTime
		{
			get
			{
				if (Value is DateTime)
					return new DateTimeOffset((DateTime)Value);
				return (DateTimeOffset)Value;
			}
		}

		public TimeSpan GetTimeSpan => (TimeSpan)Value;

		public static ExpressionResult DotOp(ExpressionResult obj, ExpressionResult fileName)
		{
			string fieldNameStr = (fileName.Value ?? "").ToString();
			if (obj.Units.HasUnits)
				throw new Exception("Can't do dot operator with units.");
			var field = obj.Value?.GetType().GetProperty(fieldNameStr, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (field == null)
				return null;
			return new ExpressionResult(field.GetValue(obj.Value));
		}

		public static ExpressionResult operator !(ExpressionResult obj) => new ExpressionResult(!obj.GetBool, obj.Units);
		public static ExpressionResult operator ~(ExpressionResult obj) => new ExpressionResult(~obj.GetInteger, obj.Units);
		public static ExpressionResult operator +(ExpressionResult obj) => obj;

		public static ExpressionResult operator -(ExpressionResult obj)
		{
			if (obj.IsInteger)
				return new ExpressionResult(-obj.GetInteger, obj.Units);
			if (obj.IsFloat)
				return new ExpressionResult(-obj.GetFloat, obj.Units);
			if (obj.IsComplex)
				return new ExpressionResult(-obj.GetComplex, obj.Units);
			throw new Exception("Invalid operation");
		}

		public static ExpressionResult Factorial(ExpressionResult obj)
		{
			if (obj.Units.HasUnits)
				throw new Exception("Cannot do factorial with units");

			if (!obj.IsInteger)
				throw new Exception("Factorials only for integers");

			var value = obj.GetInteger;
			BigInteger result = 1;
			for (var ctr = 2; ctr <= value; ++ctr)
				result *= ctr;
			return new ExpressionResult(result);
		}

		public static ExpressionResult Exp(ExpressionResult baseVal, ExpressionResult exponentVal)
		{
			if (exponentVal.Units.HasUnits)
				throw new Exception("Exponent cannot have units.");

			if ((baseVal.IsFloat) && (exponentVal.IsInteger))
			{
				var exponent = (int)exponentVal.GetInteger;
				object value;
				if ((exponent >= 0) && (baseVal.IsInteger))
					value = BigInteger.Pow(baseVal.GetInteger, exponent);
				else
					value = Math.Pow(baseVal.GetFloat, exponent);
				var units = baseVal.Units ^ exponent;
				return new ExpressionResult(value, units);
			}

			if (baseVal.Units.HasUnits)
				throw new Exception("Invalid base units.");

			if ((baseVal.IsFloat) && (exponentVal.IsFloat))
			{
				var value = Math.Pow(baseVal.GetFloat, exponentVal.GetFloat);
				if (!double.IsNaN(value))
					return new ExpressionResult(value);
			}

			return new ExpressionResult(Complex.Pow(baseVal.GetComplex, exponentVal.GetComplex));
		}

		static void Swap<T>(ref T obj1, ref T obj2)
		{
			var tmp = obj1;
			obj1 = obj2;
			obj2 = tmp;
		}

		public static ExpressionResult operator *(ExpressionResult factor1, ExpressionResult factor2)
		{
			if ((factor1.Value == null) || (factor2.Value == null))
				throw new Exception("NULL value");

			var units = factor1.Units * factor2.Units;

			if ((factor2.IsString) && (factor1.IsComplex))
				Swap(ref factor1, ref factor2);
			if ((factor1.IsString) && (factor2.IsComplex))
			{
				var str = factor1.GetString;
				var count = (int)factor2.GetInteger;
				var sb = new StringBuilder(str.Length * count);
				for (var ctr = 0; ctr < count; ++ctr)
					sb.Append(str);
				return new ExpressionResult(sb.ToString(), units);
			}

			if ((factor1.IsInteger) && (factor2.IsInteger))
				return new ExpressionResult(factor1.GetInteger * factor2.GetInteger, units);

			if ((factor1.IsFloat) && (factor2.IsFloat))
				return new ExpressionResult(factor1.GetFloat * factor2.GetFloat, units);

			return new ExpressionResult(factor1.GetComplex * factor2.GetComplex, units);
		}

		public static ExpressionResult operator /(ExpressionResult dividend, ExpressionResult divisor)
		{
			if ((dividend.Value == null) || (divisor.Value == null))
				throw new Exception("NULL value");

			var units = dividend.Units / divisor.Units;

			if ((dividend.IsInteger) && (divisor.IsInteger))
			{
				var int1 = dividend.GetInteger;
				var int2 = divisor.GetInteger;
				if ((int2 != 0) && ((int1 % int2) == 0))
					return new ExpressionResult(int1 / int2, units);
			}

			if ((dividend.IsFloat) && (divisor.IsFloat))
				return new ExpressionResult(dividend.GetFloat / divisor.GetFloat, units);

			return new ExpressionResult(dividend.GetComplex / divisor.GetComplex, units);
		}

		public static ExpressionResult IntDiv(ExpressionResult dividend, ExpressionResult divisor)
		{
			if ((dividend.Value == null) || (divisor.Value == null))
				throw new Exception("NULL value");

			return new ExpressionResult(dividend.GetInteger / divisor.GetInteger, dividend.Units / divisor.Units);
		}

		public static ExpressionResult operator %(ExpressionResult dividend, ExpressionResult divisor)
		{
			var units = dividend.Units / divisor.Units;

			if ((dividend.Value == null) || (divisor.Value == null))
				throw new Exception("NULL value");

			if ((dividend.IsInteger) && (divisor.IsInteger))
				return new ExpressionResult(dividend.GetInteger % divisor.GetInteger, units);

			return new ExpressionResult(dividend.GetFloat % divisor.GetFloat, units);
		}

		public static ExpressionResult operator +(ExpressionResult addend1, ExpressionResult addend2)
		{
			if ((addend1.Value == null) || (addend2.Value == null))
			{
				if (addend1.IsString)
					return addend1;
				if (addend2.IsString)
					return addend2;
				throw new Exception("NULL value");
			}

			if ((addend2.IsDateTime) && (!addend1.IsDateTime))
				Swap(ref addend1, ref addend2);
			if (addend1.IsDateTime)
			{
				var datetime1 = addend1.GetDateTime;
				if (addend2.IsTimeSpan)
					return new ExpressionResult(datetime1 + addend2.GetTimeSpan, addend1.Units);
				if (addend2.IsFloat)
				{
					if (!addend2.Units.HasUnits)
						addend2 = new ExpressionResult(addend2.Value, "days");
					var unit = addend2.Units.Single();
					if (unit.Exp != 1)
						throw new Exception("Invalid unit");
					if (addend2.IsInteger)
					{
						switch (unit.Unit)
						{
							case "month":
							case "months":
							case "mon":
							case "mons":
								return new ExpressionResult(datetime1.AddMonths((int)addend2.GetInteger), addend1.Units);
							case "year":
							case "years":
							case "yr":
							case "yrs":
							case "y":
								return new ExpressionResult(datetime1.AddYears((int)addend2.GetInteger), addend1.Units);
						}
					}

					addend2 = UnitConvertOp(addend2, new ExpressionUnits("ticks"));
					return new ExpressionResult(datetime1 + TimeSpan.FromTicks((long)addend2.GetInteger), addend1.Units);
				}
				throw new ArgumentException("Operation failed");
			}

			if ((addend2.IsTimeSpan) && (!addend1.IsTimeSpan))
				Swap(ref addend1, ref addend2);
			if (addend1.IsTimeSpan)
			{
				var timespan1 = addend1.GetTimeSpan;
				if (addend2.IsTimeSpan)
					return new ExpressionResult(timespan1 + addend2.GetTimeSpan, addend1.Units);
				if (addend2.IsFloat)
				{
					if (!addend2.Units.HasUnits)
						addend2 = new ExpressionResult(addend2.Value, "minutes");
					var unit = addend2.Units.Single();
					if (unit.Exp != 1)
						throw new Exception("Invalid unit");
					addend2 = UnitConvertOp(addend2, new ExpressionUnits("ticks"));
					return new ExpressionResult(timespan1 + TimeSpan.FromTicks((long)addend2.GetInteger), addend1.Units);
				}
				throw new ArgumentException("Operation failed");
			}

			if (!addend1.Units.Equals(addend2.Units))
				addend2 = UnitConvertOp(addend2, addend1.Units);

			if ((addend2.IsCharacter) && (addend1.IsComplex))
				Swap(ref addend1, ref addend2);
			if ((addend1.IsCharacter) && (addend2.IsComplex))
				return new ExpressionResult((char)((long)addend1.GetChar + addend2.GetInteger), addend1.Units);

			if ((addend1.IsString) || (addend2.IsString))
				return new ExpressionResult(addend1.GetString + addend2.GetString, addend1.Units);

			if ((addend1.IsInteger) && (addend2.IsInteger))
				return new ExpressionResult(addend1.GetInteger + addend2.GetInteger, addend1.Units);

			if ((addend1.IsFloat) && (addend2.IsFloat))
				return new ExpressionResult(addend1.GetFloat + addend2.GetFloat, addend1.Units);

			return new ExpressionResult(addend1.GetComplex + addend2.GetComplex, addend1.Units);
		}

		public static ExpressionResult operator -(ExpressionResult minuend, ExpressionResult subtrahend)
		{
			if ((minuend.Value == null) || (subtrahend.Value == null))
				throw new Exception("NULL value");

			if (minuend.IsDateTime)
			{
				var datetime1 = minuend.GetDateTime;
				if (subtrahend.IsDateTime)
					return new ExpressionResult(datetime1 - subtrahend.GetDateTime, minuend.Units);
				if (subtrahend.IsTimeSpan)
					return new ExpressionResult(datetime1 - subtrahend.GetTimeSpan, minuend.Units);
				if (subtrahend.IsFloat)
				{
					if (!subtrahend.Units.HasUnits)
						subtrahend = new ExpressionResult(subtrahend.Value, "days");
					var unit = subtrahend.Units.Single();
					if (unit.Exp != 1)
						throw new Exception("Invalid unit");
					if (subtrahend.IsInteger)
					{
						switch (unit.Unit)
						{
							case "month":
							case "months":
							case "mon":
							case "mons":
								return new ExpressionResult(datetime1.AddMonths(-(int)subtrahend.GetInteger), minuend.Units);
							case "year":
							case "years":
							case "yr":
							case "yrs":
							case "y":
								return new ExpressionResult(datetime1.AddYears(-(int)subtrahend.GetInteger), minuend.Units);
						}
					}

					subtrahend = UnitConvertOp(subtrahend, new ExpressionUnits("ticks"));
					return new ExpressionResult(datetime1 - TimeSpan.FromTicks((long)subtrahend.GetInteger), minuend.Units);
				}
				throw new ArgumentException("Operation failed");
			}

			if (minuend.IsTimeSpan)
			{
				var timespan1 = minuend.GetTimeSpan;
				if (subtrahend.IsTimeSpan)
					return new ExpressionResult(timespan1 - subtrahend.GetTimeSpan, minuend.Units);
				if (subtrahend.IsFloat)
				{
					if (!subtrahend.Units.HasUnits)
						subtrahend = new ExpressionResult(subtrahend.Value, "minutes");
					var unit = subtrahend.Units.Single();
					if (unit.Exp != 1)
						throw new Exception("Invalid unit");
					subtrahend = UnitConvertOp(subtrahend, new ExpressionUnits("ticks"));
					return new ExpressionResult(timespan1 - TimeSpan.FromTicks((long)subtrahend.GetInteger), minuend.Units);
				}
				throw new ArgumentException("Operation failed");
			}

			if (!minuend.Units.Equals(subtrahend.Units))
				subtrahend = UnitConvertOp(subtrahend, minuend.Units);

			if ((subtrahend.IsCharacter) && (minuend.IsComplex))
				Swap(ref minuend, ref subtrahend);
			if ((minuend.IsCharacter) && (subtrahend.IsComplex))
				return new ExpressionResult((char)((long)minuend.GetChar - subtrahend.GetInteger), minuend.Units);

			if ((minuend.IsInteger) && (subtrahend.IsInteger))
				return new ExpressionResult(minuend.GetInteger - subtrahend.GetInteger, minuend.Units);

			if ((minuend.IsFloat) && (subtrahend.IsFloat))
				return new ExpressionResult(minuend.GetFloat - subtrahend.GetFloat, minuend.Units);

			return new ExpressionResult(minuend.GetComplex - subtrahend.GetComplex, minuend.Units);
		}

		public static ExpressionResult ShiftLeft(ExpressionResult val1, ExpressionResult val2)
		{
			if (val2.Units.HasUnits)
				throw new Exception("Shift value cannot have units");
			return new ExpressionResult(val1.GetInteger << (int)val2.GetInteger, val1.Units);
		}

		public static ExpressionResult ShiftRight(ExpressionResult val1, ExpressionResult val2)
		{
			if (val2.Units.HasUnits)
				throw new Exception("Shift value cannot have units");
			return new ExpressionResult(val1.GetInteger >> (int)val2.GetInteger, val1.Units);
		}

		public static ExpressionResult EqualsOp(ExpressionResult val1, ExpressionResult val2, bool ignoreCase)
		{
			if (!val1.Units.Equals(val2.Units))
			{
				var conversion1 = ExpressionUnitsConversion.GetBaseConversion(val1.Units);
				var conversion2 = ExpressionUnitsConversion.GetBaseConversion(val2.Units);
				if (!conversion1.toUnits.Equals(conversion2.toUnits))
					return new ExpressionResult(false); // Units don't match
				val2 = UnitConvertOp(val2, val1.Units);
			}

			if ((val1.Value == null) && (val2.Value == null))
				return new ExpressionResult(true);
			if ((val1.Value == null) || (val2.Value == null))
				return new ExpressionResult(false);

			if ((val1.IsInteger) && (val2.IsInteger))
				return new ExpressionResult(val1.GetInteger == val2.GetInteger);

			if ((val1.IsFloat) && (val2.IsFloat))
				return new ExpressionResult(val1.GetFloat == val2.GetFloat);

			if ((val1.IsComplex) && (val2.IsComplex))
				return new ExpressionResult(val1.GetComplex == val2.GetComplex);

			if ((val1.IsString) && (val2.IsString))
				return new ExpressionResult(val1.GetString.Equals(val2.GetString, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));

			return new ExpressionResult(val1.Value.Equals(val2.Value));
		}

		public static ExpressionResult LessThanOp(ExpressionResult val1, ExpressionResult val2, bool ignoreCase)
		{
			if ((val1.Value == null) || (val2.Value == null))
				throw new Exception("NULL value");

			if (!val1.Units.Equals(val2.Units))
				val2 = UnitConvertOp(val2, val1.Units);

			if ((val1.IsString) && (val2.IsString))
				return new ExpressionResult(String.Compare(val1.GetString, val2.GetString, ignoreCase) < 0);

			if ((val1.IsInteger) && (val2.IsInteger))
				return new ExpressionResult(val1.GetInteger < val2.GetInteger);

			return new ExpressionResult(val1.GetFloat < val2.GetFloat);
		}

		public static ExpressionResult GreaterThanOp(ExpressionResult val1, ExpressionResult val2, bool ignoreCase)
		{
			if ((val1.Value == null) || (val2.Value == null))
				throw new Exception("NULL value");

			if (!val1.Units.Equals(val2.Units))
				val2 = UnitConvertOp(val2, val1.Units);

			if ((val1.IsString) && (val2.IsString))
				return new ExpressionResult(String.Compare(val1.GetString, val2.GetString, ignoreCase) > 0);

			if ((val1.IsInteger) && (val2.IsInteger))
				return new ExpressionResult(val1.GetInteger > val2.GetInteger);

			return new ExpressionResult(val1.GetFloat > val2.GetFloat);
		}

		public static ExpressionResult Is(ExpressionResult val1, ExpressionResult val2)
		{
			if ((val1.Units.HasUnits) || (val2.Units.HasUnits))
				throw new Exception("Is cannot have units");
			return new ExpressionResult(val1.Value?.GetType().Name.Equals(val2.Value as string) ?? false);
		}

		public static ExpressionResult operator &(ExpressionResult val1, ExpressionResult val2)
		{
			if (!val1.Units.Equals(val2.Units))
				val2 = UnitConvertOp(val2, val1.Units);
			return new ExpressionResult(val1.GetInteger & val2.GetInteger, val1.Units);
		}

		public static ExpressionResult operator ^(ExpressionResult val1, ExpressionResult val2)
		{
			if (!val1.Units.Equals(val2.Units))
				val2 = UnitConvertOp(val2, val1.Units);
			return new ExpressionResult(val1.GetInteger ^ val2.GetInteger, val1.Units);
		}

		public static ExpressionResult operator |(ExpressionResult val1, ExpressionResult val2)
		{
			if (!val1.Units.Equals(val2.Units))
				val2 = UnitConvertOp(val2, val1.Units);
			return new ExpressionResult(val1.GetInteger | val2.GetInteger, val1.Units);
		}

		public static ExpressionResult AndOp(ExpressionResult val1, ExpressionResult val2)
		{
			if (!val1.Units.Equals(val2.Units))
				val2 = UnitConvertOp(val2, val1.Units);
			return new ExpressionResult(val1.GetBool && val2.GetBool);
		}

		public static ExpressionResult OrOp(ExpressionResult val1, ExpressionResult val2)
		{
			if (!val1.Units.Equals(val2.Units))
				val2 = UnitConvertOp(val2, val1.Units);
			return new ExpressionResult(val1.GetBool || val2.GetBool);
		}

		public static ExpressionResult NullCoalesceOp(ExpressionResult val1, ExpressionResult val2) => val1.Value != null ? val1 : val2;

		public static ExpressionResult UnitConvertOp(ExpressionResult value, ExpressionUnits units)
		{
			if (value.IsTimeSpan)
				value = new ExpressionResult(value.GetTimeSpan.Ticks, "ticks");
			var conversion = ExpressionUnitsConversion.GetConversion(value.Units, units);
			var mult = new ExpressionResult(conversion.mult, conversion.toUnits / conversion.fromUnits);
			var add = new ExpressionResult(conversion.add, conversion.toUnits);
			return value * mult + add;
		}

		public ExpressionResult SetUnits(ExpressionResult units) => new ExpressionResult(Value, units.Units);

		public ExpressionResult ToWords()
		{
			var num = GetInteger;
			var negative = num < 0;
			if (negative)
				num = -num;
			var ones = new List<string> { null, "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", null, "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };
			var tens = new List<string> { null, "ten", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };
			var magnitudes = new List<string> { null, "thousand", "million", "billion", "trillion", "quadrillion", "quintillion", "sextillion", "septillion", "octillion", "nonillion", "decillion", "undecillion", "duodecillion", "tredecillion", "quattuordecillion", "quindecillion", "sexdecillion", "septdecillion", "octodecillion", "novemdecillion", "vigintillion", "unvigintillion", "duovigintillion", "trevigintillion", "quattuorvigintillion", "quinvigintillion", "sexvigintillion", "septvigintillion", "octovigintillion", "novemvigintillion", "trigintillion", "untrigintillion", "duotrigintillion", "googol", "tretrigintillion", "quattuortrigintillion", "quintrigintillion", "sextrigintillion", "septtrigintillion", "octotrigintillion", "novemtrigintillion", "quadragintillion", "unquadragintillion", "duoquadragintillion", "trequadragintillion", "quattuorquadragintillion", "quinquadragintillion", "sexquadragintillion", "septquadragintillion", "octoquadragintillion", "novemquadragintillion", "quinquagintillion", "unquinquagintillion", "duoquinquagintillion", "trequinquagintillion", "quattuorquinquagintillion", "quinquinquagintillion", "sexquinquagintillion", "septquinquagintillion", "octoquinquagintillion", "novemquinquagintillion", "sexagintillion", "unsexagintillion", "duosexagintillion", "tresexagintillion", "quattuorsexagintillion", "quinsexagintillion", "sexsexagintillion", "septsexagintillion", "octosexagintillion", "novemsexagintillion", "septuagintillion", "unseptuagintillion", "duoseptuagintillion", "treseptuagintillion", "quattuorseptuagintillion", "quinseptuagintillion", "sexseptuagintillion", "septseptuagintillion", "octoseptuagintillion", "novemseptuagintillion", "octogintillion", "unoctogintillion", "duooctogintillion", "treoctogintillion", "quattuoroctogintillion", "quinoctogintillion", "sexoctogintillion", "septoctogintillion", "octooctogintillion", "novemoctogintillion", "nonagintillion", "unnonagintillion", "duononagintillion", "trenonagintillion", "quattuornonagintillion", "quinnonagintillion", "sexnonagintillion", "septnonagintillion", "octononagintillion", "novemnonagintillion", "centillion" };
			var magnitude = new List<string>();
			while (num > 0)
			{
				var strs = new List<string>();

				var current = (int)(num % 1000);
				num /= 1000;

				while (current != 0)
				{
					var hundred = current / 100;
					if (hundred != 0)
					{
						strs.Add(ones[hundred]);
						strs.Add("hundred");
						current %= 100;
					}

					if ((current < ones.Count) && (ones[current] != null))
					{
						strs.Add(ones[current]);
						current = 0;
					}

					var ten = current / 10;
					if (ten != 0)
					{
						strs.Add(tens[ten]);
						current %= 10;
					}
				}

				magnitude.Add(String.Join(" ", strs));
			}

			var result = new List<string>();
			if (negative)
				result.Add("negative");

			for (var ctr = magnitude.Count - 1; ctr >= 0; --ctr)
			{
				if (String.IsNullOrEmpty(magnitude[ctr]))
					continue;
				result.Add(magnitude[ctr]);
				if (magnitudes[ctr] != null)
					result.Add(magnitudes[ctr]);
			}

			if (result.Count == 0)
				result.Add("zero");

			var resultStr = String.Join(" ", result);
			resultStr = Char.ToUpperInvariant(resultStr[0]) + resultStr.Substring(1);
			return new ExpressionResult(resultStr);
		}

		public ExpressionResult FromWords()
		{
			var words = GetString;
			var ones = new List<string> { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", null, "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };
			var tens = new List<string> { null, "ten", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };
			var magnitudes = new List<string> { null, "thousand", "million", "billion", "trillion", "quadrillion", "quintillion", "sextillion", "septillion", "octillion", "nonillion", "decillion", "undecillion", "duodecillion", "tredecillion", "quattuordecillion", "quindecillion", "sexdecillion", "septdecillion", "octodecillion", "novemdecillion", "vigintillion", "unvigintillion", "duovigintillion", "trevigintillion", "quattuorvigintillion", "quinvigintillion", "sexvigintillion", "septvigintillion", "octovigintillion", "novemvigintillion", "trigintillion", "untrigintillion", "duotrigintillion", "googol", "tretrigintillion", "quattuortrigintillion", "quintrigintillion", "sextrigintillion", "septtrigintillion", "octotrigintillion", "novemtrigintillion", "quadragintillion", "unquadragintillion", "duoquadragintillion", "trequadragintillion", "quattuorquadragintillion", "quinquadragintillion", "sexquadragintillion", "septquadragintillion", "octoquadragintillion", "novemquadragintillion", "quinquagintillion", "unquinquagintillion", "duoquinquagintillion", "trequinquagintillion", "quattuorquinquagintillion", "quinquinquagintillion", "sexquinquagintillion", "septquinquagintillion", "octoquinquagintillion", "novemquinquagintillion", "sexagintillion", "unsexagintillion", "duosexagintillion", "tresexagintillion", "quattuorsexagintillion", "quinsexagintillion", "sexsexagintillion", "septsexagintillion", "octosexagintillion", "novemsexagintillion", "septuagintillion", "unseptuagintillion", "duoseptuagintillion", "treseptuagintillion", "quattuorseptuagintillion", "quinseptuagintillion", "sexseptuagintillion", "septseptuagintillion", "octoseptuagintillion", "novemseptuagintillion", "octogintillion", "unoctogintillion", "duooctogintillion", "treoctogintillion", "quattuoroctogintillion", "quinoctogintillion", "sexoctogintillion", "septoctogintillion", "octooctogintillion", "novemoctogintillion", "nonagintillion", "unnonagintillion", "duononagintillion", "trenonagintillion", "quattuornonagintillion", "quinnonagintillion", "sexnonagintillion", "septnonagintillion", "octononagintillion", "novemnonagintillion", "centillion" };

			words = Regex.Replace(Regex.Replace(words.ToLowerInvariant(), "[^0-9.a-z]", " "), "\\s+", " ").Trim();
			var tokens = words.Split(' ').Where(str => !String.IsNullOrWhiteSpace(str)).ToList();

			double current = 0;
			var result = new ExpressionResult(BigInteger.Zero);
			bool negative = false;
			using (var itr = tokens.GetEnumerator())
			{
				while (true)
				{
					var token = itr.MoveNext() ? itr.Current : null;

					if (token == "negative")
					{
						negative = !negative;
						continue;
					}

					double tokenValue;
					if ((token != null) && (Double.TryParse(token, out tokenValue)))
					{
						current += tokenValue;
						continue;
					}

					var onesIndex = token == null ? -1 : ones.IndexOf(token);
					if (onesIndex != -1)
					{
						current += onesIndex;
						continue;
					}

					var tensIndex = token == null ? -1 : tens.IndexOf(token);
					if (tensIndex != -1)
					{
						current += tensIndex * 10;
						continue;
					}

					if (token == "hundred")
					{
						current *= 100;
						continue;
					}

					var magnitudesIndex = token == null ? 0 : magnitudes.IndexOf(token);
					if ((token == null) || (magnitudesIndex != -1))
					{
						var value = new ExpressionResult(current);
						var magFactor = new ExpressionResult(1000);
						for (var ctr = 0; ctr < magnitudesIndex; ++ctr)
							value *= magFactor;
						result += value;
						current = 0;
						if (token == null)
							break;
						continue;
					}

					throw new Exception("Invalid input");
				}
			}

			if (negative)
				result = -result;
			return result;
		}

		ExpressionResult ToRad()
		{
			if (!Units.HasUnits)
				return new ExpressionResult(Value, "rad");
			return UnitConvertOp(this, new ExpressionUnits("rad"));
		}

		public ExpressionResult Abs()
		{
			if (IsInteger)
				return new ExpressionResult(BigInteger.Abs(GetInteger), Units);
			if (IsFloat)
				return new ExpressionResult(Math.Abs(GetFloat), Units);
			return new ExpressionResult(Complex.Abs(GetComplex), Units);
		}

		public static ExpressionResult Acos(ExpressionResult value)
		{
			if (value.Units.HasUnits)
				throw new ArgumentException("Cannot have units.");
			if (value.IsFloat)
				return new ExpressionResult(Math.Acos(value.GetFloat), "rad");
			return new ExpressionResult(Complex.Acos(value.GetComplex), "rad");
		}

		public static ExpressionResult Asin(ExpressionResult value)
		{
			if (value.Units.HasUnits)
				throw new ArgumentException("Cannot have units.");
			if (value.IsFloat)
				return new ExpressionResult(Math.Asin(value.GetFloat), "rad");
			return new ExpressionResult(Complex.Asin(value.GetComplex), "rad");
		}

		public static ExpressionResult Atan(ExpressionResult value)
		{
			if (value.Units.HasUnits)
				throw new ArgumentException("Cannot have units.");
			if (value.IsFloat)
				return new ExpressionResult(Math.Atan(value.GetFloat), "rad");
			return new ExpressionResult(Complex.Atan(value.GetComplex), "rad");
		}

		public ExpressionResult Conjugate() => new ExpressionResult(Complex.Conjugate(GetComplex));

		public static ExpressionResult Cosh(ExpressionResult value)
		{
			value = value.ToRad();
			if (value.IsFloat)
				return new ExpressionResult(Math.Cosh(value.GetFloat));
			return new ExpressionResult(Complex.Cosh(value.GetComplex));
		}

		public static ExpressionResult Cos(ExpressionResult value)
		{
			value = value.ToRad();
			if (value.IsFloat)
				return new ExpressionResult(Math.Cos(value.GetFloat));
			return new ExpressionResult(Complex.Cos(value.GetComplex));
		}

		public ExpressionResult Ln()
		{
			if (IsFloat)
				return new ExpressionResult(Math.Log(GetFloat), Units);
			return new ExpressionResult(Complex.Log(GetComplex), Units);
		}

		public ExpressionResult Log()
		{
			if (IsFloat)
				return new ExpressionResult(Math.Log10(GetFloat), Units);
			return new ExpressionResult(Complex.Log10(GetComplex), Units);
		}

		public static ExpressionResult Log(ExpressionResult val, ExpressionResult newBase)
		{
			if (newBase.Units.HasUnits)
				throw new Exception("Log cannot have units.");
			if ((val.IsFloat) && (newBase.IsFloat))
				return new ExpressionResult(Math.Log(val.GetFloat, newBase.GetFloat), val.Units);
			return new ExpressionResult(Complex.Log(val.GetComplex, newBase.GetFloat), val.Units);
		}

		public ExpressionResult Reciprocal()
		{
			if (IsFloat)
				return new ExpressionResult(1.0 / GetFloat, Units);
			return new ExpressionResult(Complex.Reciprocal(GetComplex), Units);
		}

		public static ExpressionResult Root(ExpressionResult val, ExpressionResult rootObj)
		{
			if (rootObj.Units.HasUnits)
				throw new Exception("Root cannot have units.");

			var root = (int)rootObj.GetInteger;
			if (val.IsFloat)
			{
				var f = val.GetFloat;
				if (root % 2 == 1) // Odd roots
					return new ExpressionResult(f >= 0 ? Math.Pow(f, 1.0 / root) : -Math.Pow(-f, 1.0 / root), val.Units);
				else if (f >= 0) // Even roots, val >= 0
					return new ExpressionResult(Math.Pow(f, 1.0 / root), val.Units);
			}

			var complexVal = val.GetComplex;
			var phase = complexVal.Phase;
			var magnitude = complexVal.Magnitude;
			var nthRootOfMagnitude = Math.Pow(magnitude, 1.0 / root);
			var options = Enumerable.Range(0, root).Select(k => RoundComplex(Complex.FromPolarCoordinates(nthRootOfMagnitude, phase / root + k * 2 * Math.PI / root))).ToList();
			return new ExpressionResult(options.OrderBy(complex => Math.Abs(complex.Imaginary)).ThenByDescending(complex => complex.Real).ThenByDescending(complex => complex.Imaginary).FirstOrDefault(), val.Units);
		}

		public static ExpressionResult Sinh(ExpressionResult value)
		{
			value = value.ToRad();
			if (value.IsFloat)
				return new ExpressionResult(Math.Sinh(value.GetFloat));
			return new ExpressionResult(Complex.Sinh(value.GetComplex));
		}

		public static ExpressionResult Sin(ExpressionResult value)
		{
			value = value.ToRad();
			if (value.IsFloat)
				return new ExpressionResult(Math.Sin(value.GetFloat));
			return new ExpressionResult(Complex.Sin(value.GetComplex));
		}

		public static ExpressionResult Tanh(ExpressionResult value)
		{
			value = value.ToRad();
			if (value.IsFloat)
				return new ExpressionResult(Math.Tanh(value.GetFloat));
			return new ExpressionResult(Complex.Tanh(value.GetComplex));
		}

		public static ExpressionResult Tan(ExpressionResult value)
		{
			value = value.ToRad();
			if (value.IsFloat)
				return new ExpressionResult(Math.Tan(value.GetFloat));
			return new ExpressionResult(Complex.Tan(value.GetComplex));
		}

		public ExpressionResult GetFileName()
		{
			if (Units.HasUnits)
				throw new Exception("Can't do FileName with units.");
			return new ExpressionResult(Path.GetFileName(GetString));
		}

		public static ExpressionResult FromPolar(ExpressionResult val1, ExpressionResult val2) => new ExpressionResult(Complex.FromPolarCoordinates(val1.GetFloat, val2.GetFloat), val1.Units);
		public ExpressionResult GetImaginary() => new ExpressionResult(GetComplex.Imaginary, Units);
		public ExpressionResult Magnitude() => new ExpressionResult(GetComplex.Magnitude, Units);
		public ExpressionResult Phase() => new ExpressionResult(GetComplex.Phase, Units);
		public ExpressionResult Real() => new ExpressionResult(GetComplex.Real, Units);
		public static ExpressionResult StrFormat(ExpressionResult format, params ExpressionResult[] paramList) => new ExpressionResult(String.Format(format.GetString, paramList.Select(arg => arg.Value ?? "").ToArray()));
		public ExpressionResult Type() => new ExpressionResult(Value.GetType());

		static BigInteger CalcGCF(BigInteger value1, BigInteger value2)
		{
			while (value2 != 0)
			{
				var newValue = value1 % value2;
				value1 = value2;
				value2 = newValue;
			}
			return value1;
		}

		public static ExpressionResult GCF(List<ExpressionResult> inputs)
		{
			if (inputs.Count == 0)
				throw new Exception("Must provide inputs");
			if (inputs.Any(input => input.Units.HasUnits))
				throw new Exception("Inputs cannot have units");

			var factors = inputs.Select(factor => factor.Abs().GetInteger).ToList();
			if (factors.Any(factor => factor == 0))
				throw new Exception("Factors cannot be 0");

			var gcf = factors[0];
			for (var ctr = 1; ctr < factors.Count; ++ctr)
				gcf = CalcGCF(gcf, factors[ctr]);
			return new ExpressionResult(gcf);
		}

		public static ExpressionResult LCM(List<ExpressionResult> inputs)
		{
			if (inputs.Count == 0)
				throw new Exception("Must provide inputs");
			if (inputs.Any(input => input.Units.HasUnits))
				throw new Exception("Inputs cannot have units");

			var factors = inputs.Select(factor => factor.Abs().GetInteger).ToList();
			if (factors.Any(factor => factor == 0))
				throw new Exception("Factors cannot be 0");

			var lcm = factors[0];
			for (var ctr = 1; ctr < factors.Count; ++ctr)
				lcm *= factors[ctr] / CalcGCF(lcm, factors[ctr]);
			return new ExpressionResult(lcm);
		}

		public static ExpressionResult Reduce(ExpressionResult numerator, ExpressionResult denominator)
		{
			if ((numerator.Units.HasUnits) || (numerator.Units.HasUnits))
				throw new Exception("Inputs cannot have units");
			var num = numerator.Abs().GetInteger;
			var den = denominator.Abs().GetInteger;
			if ((num == 0) || (den == 0))
				throw new Exception("Factors cannot be 0");

			var gcf = CalcGCF(num, den);
			return new ExpressionResult($"{num / gcf}/{den / gcf}");
		}

		public ExpressionResult ValidRE()
		{
			try
			{
				new Regex(GetString);
				return new ExpressionResult(true);
			}
			catch { return new ExpressionResult(false); }
		}

		public object GetResult()
		{
			object result = null;
			if (IsComplex)
				result = GetString;
			else if (IsDateTime)
				result = $"'{GetDateTime.ToString("o")}'";
			else if (IsTimeSpan)
				result = $"'{GetTimeSpan.ToString("g")}'";
			else
				result = Value;

			var unitsStr = Units.ToString();
			if (unitsStr != null)
				result = $"{result} {unitsStr}";

			return result;
		}

		public override string ToString() => GetResult().ToString();

		public int CompareTo(object obj)
		{
			var result = obj as ExpressionResult;
			if (result == null)
				throw new ArgumentException("Invalid comparison");
			return GetFloat.CompareTo(result.GetFloat);
		}
	}
}
