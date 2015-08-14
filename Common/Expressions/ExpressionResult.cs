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
		object value;

		public ExpressionResult(object _value)
		{
			value = _value;

			if ((value != null) && (value.GetType().FullName == "MS.Internal.NamedObject") && (value.ToString() == "{DependencyProperty.UnsetValue}"))
				value = null;

			if (value is Complex)
			{
				var complex = RoundComplex((Complex)value);
				value = complex.Imaginary == 0 ? complex.Real : complex;
			}

			if (value is double)
			{
				var d = (double)value;
				if ((!Double.IsInfinity(d)) && (!Double.IsNaN(d)) && (Math.Floor(d) == d))
					value = new BigInteger(d);
			}

			if (value is BigInteger)
			{
				var bigint = (BigInteger)value;
				if ((bigint >= long.MinValue) && (bigint <= long.MaxValue))
					value = (long)bigint;
			}
		}

		bool IsInteger { get { return (value is sbyte) || (value is byte) || (value is short) || (value is ushort) || (value is int) || (value is uint) || (value is long) || (value is ulong) || (value is BigInteger); } }
		bool IsFloat { get { return (IsInteger) || (value is float) || (value is double) || (value is decimal); } }
		bool IsComplex { get { return (IsFloat) || (value is Complex); } }
		bool IsCharacter { get { return value is char; } }
		bool IsString { get { return (IsCharacter) || (value is string); } }
		public bool True { get { return (value is bool) && ((bool)value); } }

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
				if (value == null)
					throw new Exception("NULL value");
				return (bool)Convert.ChangeType(value, typeof(bool));
			}
		}

		BigInteger GetInteger
		{
			get
			{
				if (value == null)
					throw new Exception("NULL value");
				if (value is BigInteger)
					return (BigInteger)value;
				if (value is Complex)
				{
					var complex = (Complex)value;
					if (complex.Imaginary != 0)
						throw new Exception("Can't convert complex to double");
					value = complex.Real;
				}
				return new BigInteger((long)Convert.ChangeType(value, typeof(long)));
			}
		}

		double GetFloat
		{
			get
			{
				if (value == null)
					throw new Exception("NULL value");
				if (value is Complex)
				{
					var complex = (Complex)value;
					if (complex.Imaginary != 0)
						throw new Exception("Can't convert complex to double");
					return complex.Real;
				}
				if (value is BigInteger)
					return (double)(BigInteger)value;
				return (double)Convert.ChangeType(value, typeof(double));
			}
		}

		Complex GetComplex
		{
			get
			{
				if (value == null)
					throw new Exception("NULL value");
				if (value is Complex)
					return (Complex)value;
				if (value is BigInteger)
					return (Complex)(BigInteger)value;
				return new Complex((double)Convert.ChangeType(value, typeof(double)), 0);
			}
		}

		char GetChar
		{
			get
			{
				if (value == null)
					throw new Exception("NULL value");
				return (char)Convert.ChangeType(value, typeof(char));
			}
		}

		public string GetString
		{
			get
			{
				if (value == null)
					throw new Exception("NULL value");
				if (value is Complex)
				{
					var complex = (Complex)value;
					if (complex.Imaginary == 0)
						return complex.Real.ToString();

					var result = "";
					if (complex.Real != 0)
						result += complex.Real.ToString();
					if ((complex.Real != 0) || (complex.Imaginary < 0))
						result += complex.Imaginary < 0 ? "-" : "+";
					var absImag = Math.Abs(complex.Imaginary);
					if (absImag != 1)
						result += absImag.ToString() + "*";
					result += "i";
					return result;
				}
				return value.ToString();
			}
		}

		public static ExpressionResult DotOp(ExpressionResult obj, ExpressionResult fileName)
		{
			string fieldNameStr = (fileName.value ?? "").ToString();
			if (obj.value == null)
				return null;
			var field = obj.value.GetType().GetProperty(fieldNameStr, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (field == null)
				return null;
			return new ExpressionResult(field.GetValue(obj.value));
		}

		public static ExpressionResult operator !(ExpressionResult obj)
		{
			return new ExpressionResult(!obj.GetBool);
		}

		public static ExpressionResult operator ~(ExpressionResult obj)
		{
			return new ExpressionResult(~obj.GetInteger);
		}

		public static ExpressionResult operator +(ExpressionResult obj)
		{
			return obj;
		}

		public static ExpressionResult operator -(ExpressionResult obj)
		{
			if (obj.IsInteger)
				return new ExpressionResult(-obj.GetInteger);
			if (obj.IsFloat)
				return new ExpressionResult(-obj.GetFloat);
			if (obj.IsComplex)
				return new ExpressionResult(-obj.GetComplex);
			throw new Exception("Invalid operation");
		}

		public static ExpressionResult Factorial(ExpressionResult obj)
		{
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
			if ((baseVal.IsInteger) && (exponentVal.IsInteger))
			{
				var exponent = (int)exponentVal.GetInteger;
				var value = BigInteger.Pow(baseVal.GetInteger, exponent);
				return new ExpressionResult(value);
			}

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
			if ((factor1.value == null) || (factor2.value == null))
				throw new Exception("NULL value");

			if ((factor2.IsString) && (factor1.IsComplex))
				Swap(ref factor1, ref factor2);
			if ((factor1.IsString) && (factor2.IsComplex))
			{
				var str = factor1.GetString;
				var count = (int)factor2.GetInteger;
				var sb = new StringBuilder(str.Length * count);
				for (var ctr = 0; ctr < count; ++ctr)
					sb.Append(str);
				return new ExpressionResult(sb.ToString());
			}

			if ((factor1.IsInteger) && (factor2.IsInteger))
				return new ExpressionResult(factor1.GetInteger * factor2.GetInteger);

			if ((factor1.IsFloat) && (factor2.IsFloat))
				return new ExpressionResult(factor1.GetFloat * factor2.GetFloat);

			return new ExpressionResult(factor1.GetComplex * factor2.GetComplex);
		}

		public static ExpressionResult operator /(ExpressionResult dividend, ExpressionResult divisor)
		{
			if ((dividend.value == null) || (divisor.value == null))
				throw new Exception("NULL value");

			if ((dividend.IsInteger) && (divisor.IsInteger))
			{
				var int1 = dividend.GetInteger;
				var int2 = divisor.GetInteger;
				if ((int2 != 0) && ((int1 % int2) == 0))
					return new ExpressionResult(int1 / int2);
			}

			if ((dividend.IsFloat) && (divisor.IsFloat))
				return new ExpressionResult(dividend.GetFloat / divisor.GetFloat);

			return new ExpressionResult(dividend.GetComplex / divisor.GetComplex);
		}

		public static ExpressionResult operator %(ExpressionResult dividend, ExpressionResult divisor)
		{
			if ((dividend.value == null) || (divisor.value == null))
				throw new Exception("NULL value");

			if ((dividend.IsInteger) && (divisor.IsInteger))
				return new ExpressionResult(dividend.GetInteger % divisor.GetInteger);

			return new ExpressionResult(dividend.GetFloat % divisor.GetFloat);
		}

		public static ExpressionResult operator +(ExpressionResult addend1, ExpressionResult addend2)
		{
			if ((addend1.value == null) || (addend2.value == null))
			{
				if (addend1.IsString)
					return addend1;
				if (addend2.IsString)
					return addend2;
				throw new Exception("NULL value");
			}

			if ((addend2.IsCharacter) && (addend1.IsComplex))
				Swap(ref addend1, ref addend2);
			if ((addend1.IsCharacter) && (addend2.IsComplex))
				return new ExpressionResult((char)((long)addend1.GetChar + addend2.GetInteger));

			if ((addend1.IsString) || (addend2.IsString))
				return new ExpressionResult(addend1.GetString + addend2.GetString);

			if ((addend1.IsInteger) && (addend2.IsInteger))
				return new ExpressionResult(addend1.GetInteger + addend2.GetInteger);

			if ((addend1.IsFloat) && (addend2.IsFloat))
				return new ExpressionResult(addend1.GetFloat + addend2.GetFloat);

			return new ExpressionResult(addend1.GetComplex + addend2.GetComplex);
		}

		public static ExpressionResult operator -(ExpressionResult minuend, ExpressionResult subtrahend)
		{
			if ((minuend.value == null) || (subtrahend.value == null))
				throw new Exception("NULL value");

			if ((subtrahend.IsCharacter) && (minuend.IsComplex))
				Swap(ref minuend, ref subtrahend);
			if ((minuend.IsCharacter) && (subtrahend.IsComplex))
				return new ExpressionResult((char)((long)minuend.GetChar - subtrahend.GetInteger));

			if ((minuend.IsInteger) && (subtrahend.IsInteger))
				return new ExpressionResult(minuend.GetInteger - subtrahend.GetInteger);

			if ((minuend.IsFloat) && (subtrahend.IsFloat))
				return new ExpressionResult(minuend.GetFloat - subtrahend.GetFloat);

			return new ExpressionResult(minuend.GetComplex - subtrahend.GetComplex);
		}

		public static ExpressionResult ShiftLeft(ExpressionResult val1, ExpressionResult val2)
		{
			return new ExpressionResult(val1.GetInteger << (int)val2.GetInteger);
		}

		public static ExpressionResult ShiftRight(ExpressionResult val1, ExpressionResult val2)
		{
			return new ExpressionResult(val1.GetInteger >> (int)val2.GetInteger);
		}

		public static ExpressionResult EqualsOp(ExpressionResult val1, ExpressionResult val2, bool ignoreCase)
		{
			if ((val1.value == null) && (val2.value == null))
				return new ExpressionResult(true);
			if ((val1.value == null) || (val2.value == null))
				return new ExpressionResult(false);

			if ((val1.IsInteger) && (val2.IsInteger))
				return new ExpressionResult(val1.GetInteger == val2.GetInteger);

			if ((val1.IsFloat) && (val2.IsFloat))
				return new ExpressionResult(val1.GetFloat == val2.GetFloat);

			if ((val1.IsComplex) && (val2.IsComplex))
				return new ExpressionResult(val1.GetComplex == val2.GetComplex);

			if ((val1.IsString) && (val2.IsString))
				return new ExpressionResult(val1.GetString.Equals(val2.GetString, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));

			return new ExpressionResult(val1.value.Equals(val2.value));
		}

		public static ExpressionResult LessThanOp(ExpressionResult val1, ExpressionResult val2, bool ignoreCase)
		{
			if ((val1.value == null) || (val2.value == null))
				throw new Exception("NULL value");

			if ((val1.IsString) && (val2.IsString))
				return new ExpressionResult(String.Compare(val1.GetString, val2.GetString, ignoreCase) < 0);

			if ((val1.IsInteger) && (val2.IsInteger))
				return new ExpressionResult(val1.GetInteger < val2.GetInteger);

			return new ExpressionResult(val1.GetFloat < val2.GetFloat);
		}

		public static ExpressionResult GreaterThanOp(ExpressionResult val1, ExpressionResult val2, bool ignoreCase)
		{
			if ((val1.value == null) || (val2.value == null))
				throw new Exception("NULL value");

			if ((val1.IsString) && (val2.IsString))
				return new ExpressionResult(String.Compare(val1.GetString, val2.GetString, ignoreCase) > 0);

			if ((val1.IsInteger) && (val2.IsInteger))
				return new ExpressionResult(val1.GetInteger > val2.GetInteger);

			return new ExpressionResult(val1.GetFloat > val2.GetFloat);
		}

		public static ExpressionResult Is(ExpressionResult val1, ExpressionResult val2)
		{
			return new ExpressionResult(val1.value == null ? false : val1.value.GetType().Name == (val2.value ?? "").ToString());
		}

		public static ExpressionResult operator &(ExpressionResult val1, ExpressionResult val2)
		{
			return new ExpressionResult(val1.GetInteger & val2.GetInteger);
		}

		public static ExpressionResult operator ^(ExpressionResult val1, ExpressionResult val2)
		{
			return new ExpressionResult(val1.GetInteger ^ val2.GetInteger);
		}

		public static ExpressionResult operator |(ExpressionResult val1, ExpressionResult val2)
		{
			return new ExpressionResult(val1.GetInteger | val2.GetInteger);
		}

		public static ExpressionResult AndOp(ExpressionResult val1, ExpressionResult val2)
		{
			return new ExpressionResult(val1.GetBool && val2.GetBool);
		}

		public static ExpressionResult OrOp(ExpressionResult val1, ExpressionResult val2)
		{
			return new ExpressionResult(val1.GetBool || val2.GetBool);
		}

		public static ExpressionResult NullCoalesceOp(ExpressionResult val1, ExpressionResult val2)
		{
			return val1.value != null ? val1 : val2;
		}

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

		public ExpressionResult Abs()
		{
			if (IsInteger)
				return new ExpressionResult(BigInteger.Abs(GetInteger));
			if (IsFloat)
				return new ExpressionResult(Math.Abs(GetFloat));
			return new ExpressionResult(Complex.Abs(GetComplex));
		}

		public ExpressionResult Acos()
		{
			if (IsFloat)
				return new ExpressionResult(Math.Acos(GetFloat));
			return new ExpressionResult(Complex.Abs(GetComplex));
		}

		public ExpressionResult Asin()
		{
			if (IsFloat)
				return new ExpressionResult(Math.Asin(GetFloat));
			return new ExpressionResult(Complex.Asin(GetComplex));
		}

		public ExpressionResult Atan()
		{
			if (IsFloat)
				return new ExpressionResult(Math.Atan(GetFloat));
			return new ExpressionResult(Complex.Atan(GetComplex));
		}

		public ExpressionResult Conjugate()
		{
			return new ExpressionResult(Complex.Conjugate(GetComplex));
		}

		public ExpressionResult Cosh()
		{
			if (IsFloat)
				return new ExpressionResult(Math.Cosh(GetFloat));
			return new ExpressionResult(Complex.Cosh(GetComplex));
		}

		public ExpressionResult Cos()
		{
			if (IsFloat)
				return new ExpressionResult(Math.Cos(GetFloat));
			return new ExpressionResult(Complex.Cos(GetComplex));
		}

		public ExpressionResult Ln()
		{
			if (IsFloat)
				return new ExpressionResult(Math.Log(GetFloat));
			return new ExpressionResult(Complex.Log(GetComplex));
		}

		public ExpressionResult Log()
		{
			if (IsFloat)
				return new ExpressionResult(Math.Log10(GetFloat));
			return new ExpressionResult(Complex.Log10(GetComplex));
		}

		public static ExpressionResult Log(ExpressionResult val, ExpressionResult newBase)
		{
			if ((val.IsFloat) && (newBase.IsFloat))
				return new ExpressionResult(Math.Log(val.GetFloat, newBase.GetFloat));
			return new ExpressionResult(Complex.Log(val.GetComplex, newBase.GetFloat));
		}

		public ExpressionResult Reciprocal()
		{
			if (IsFloat)
				return new ExpressionResult(1.0 / GetFloat);
			return new ExpressionResult(Complex.Reciprocal(GetComplex));
		}

		public static ExpressionResult Root(ExpressionResult val, ExpressionResult rootObj)
		{
			var root = (int)rootObj.GetInteger;
			if (val.IsFloat)
			{
				var f = val.GetFloat;
				if (root % 2 == 1) // Odd roots
					return new ExpressionResult(f >= 0 ? Math.Pow(f, 1.0 / root) : -Math.Pow(-f, 1.0 / root));
				else if (f >= 0) // Even roots, val >= 0
					return new ExpressionResult(Math.Pow(f, 1.0 / root));
			}

			var complexVal = val.GetComplex;
			var phase = complexVal.Phase;
			var magnitude = complexVal.Magnitude;
			var nthRootOfMagnitude = Math.Pow(magnitude, 1.0 / root);
			var options = Enumerable.Range(0, root).Select(k => RoundComplex(Complex.FromPolarCoordinates(nthRootOfMagnitude, phase / root + k * 2 * Math.PI / root))).ToList();
			return new ExpressionResult(options.OrderBy(complex => Math.Abs(complex.Imaginary)).ThenByDescending(complex => complex.Real).ThenByDescending(complex => complex.Imaginary).FirstOrDefault());
		}

		public ExpressionResult Sinh()
		{
			if (IsFloat)
				return new ExpressionResult(Math.Sinh(GetFloat));
			return new ExpressionResult(Complex.Sinh(GetComplex));
		}

		public ExpressionResult Sin()
		{
			if (IsFloat)
				return new ExpressionResult(Math.Sin(GetFloat));
			return new ExpressionResult(Complex.Sin(GetComplex));
		}

		public ExpressionResult Tanh()
		{
			if (IsFloat)
				return new ExpressionResult(Math.Tanh(GetFloat));
			return new ExpressionResult(Complex.Tanh(GetComplex));
		}

		public ExpressionResult Tan()
		{
			if (IsFloat)
				return new ExpressionResult(Math.Tan(GetFloat));
			return new ExpressionResult(Complex.Tan(GetComplex));
		}

		public ExpressionResult GetFileName()
		{
			return new ExpressionResult(Path.GetFileName(GetString));
		}

		public static ExpressionResult FromPolar(ExpressionResult val1, ExpressionResult val2)
		{
			return new ExpressionResult(Complex.FromPolarCoordinates(val1.GetFloat, val2.GetFloat));
		}

		public ExpressionResult GetImaginary()
		{
			return new ExpressionResult(GetComplex.Imaginary);
		}

		public ExpressionResult Magnitude()
		{
			return new ExpressionResult(GetComplex.Magnitude);
		}

		public ExpressionResult Phase()
		{
			return new ExpressionResult(GetComplex.Phase);
		}

		public ExpressionResult Real()
		{
			return new ExpressionResult(GetComplex.Real);
		}

		public static ExpressionResult StrFormat(ExpressionResult format, params ExpressionResult[] paramList)
		{
			return new ExpressionResult(String.Format(format.GetString, paramList.Select(arg => arg.value ?? "").ToArray()));
		}

		public ExpressionResult Type()
		{
			return new ExpressionResult(value.GetType());
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
			else
				result = value;

			return result;
		}

		public int CompareTo(object obj)
		{
			var result = obj as ExpressionResult;
			if (result == null)
				throw new ArgumentException("Invalid comparison");
			return GetFloat.CompareTo(result.GetFloat);
		}
	}
}
