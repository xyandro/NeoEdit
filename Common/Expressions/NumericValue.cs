using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;

namespace NeoEdit.Expressions
{
	class NumericValue : IComparable
	{
		public object Value { get; private set; }
		public ExpressionUnits Units { get; }

		public int IntValue => (int)GetInteger;
		public long LongValue => (long)GetInteger;

		bool IsInteger => Value is BigInteger;

		BigInteger GetInteger
		{
			get
			{
				if (!(Value is BigInteger))
					throw new Exception("Invalid value");
				return (BigInteger)Value;
			}
		}

		double GetFloat
		{
			get
			{
				if (Value is BigInteger)
					return (double)(BigInteger)Value;
				return (double)Value;
			}
		}

		public NumericValue(object value) : this(value, new ExpressionUnits()) { }
		public NumericValue(object value, string unit) : this(value, new ExpressionUnits(unit)) { }

		public NumericValue(object input, ExpressionUnits units)
		{
			if (input == null)
				Value = BigInteger.Zero;
			else if ((input is BigInteger) || (input is double))
				Value = input;
			else if (input is bool)
				Value = (bool)input ? BigInteger.One : BigInteger.Zero;
			else if (input is sbyte)
				Value = new BigInteger((sbyte)input);
			else if (input is byte)
				Value = new BigInteger((byte)input);
			else if (input is short)
				Value = new BigInteger((short)input);
			else if (input is ushort)
				Value = new BigInteger((ushort)input);
			else if (input is int)
				Value = new BigInteger((int)input);
			else if (input is uint)
				Value = new BigInteger((uint)input);
			else if (input is long)
				Value = new BigInteger((long)input);
			else if (input is ulong)
				Value = new BigInteger((ulong)input);
			else if (input is float)
				Value = (double)(float)input;
			else if (input is decimal)
				Value = (double)(decimal)input;
			else if (input is string)
			{
				var str = (string)input;
				if (str.IsNumeric())
					Value = BigInteger.Parse(str);
				else
					Value = double.Parse(str);
			}
			else
				throw new Exception("Value is not numeric");

			Units = units;

			if (Value is double)
			{
				var d = (double)Value;
				if ((!double.IsInfinity(d)) && (!double.IsNaN(d)) && (Math.Floor(d) == d))
					Value = new BigInteger(d);
			}
		}

		public static NumericValue operator ~(NumericValue obj) => new NumericValue(~(BigInteger)obj.Value, obj.Units);

		public static NumericValue operator +(NumericValue obj) => obj;

		public static NumericValue operator -(NumericValue obj) => obj.IsInteger ? new NumericValue(-obj.GetInteger, obj.Units) : new NumericValue(-obj.GetFloat, obj.Units);

		public void VerifyNoUnits(string message = null)
		{
			if (Units.HasUnits)
				throw new Exception(message ?? "Units not allowed");
		}

		public NumericValue Factorial()
		{
			VerifyNoUnits();

			var value = GetInteger;
			BigInteger result = 1;
			for (var ctr = 2; ctr <= value; ++ctr)
				result *= ctr;
			return new NumericValue(result);
		}

		public NumericValue Exp(NumericValue exponentVal)
		{
			exponentVal.VerifyNoUnits("Exponent cannot have units.");

			if (!exponentVal.IsInteger)
			{
				VerifyNoUnits("Base can only have units for positive integer exponents.");
				return new NumericValue(Math.Pow(GetFloat, exponentVal.GetFloat));
			}

			var exponent = exponentVal.IntValue;
			object result;
			if ((exponent >= 0) && (IsInteger))
				result = BigInteger.Pow(GetInteger, exponent);
			else
				result = Math.Pow(GetFloat, exponent);
			return new NumericValue(result, Units ^ exponent);
		}

		public static NumericValue operator *(NumericValue factor1, NumericValue factor2)
		{
			var units = factor1.Units * factor2.Units;

			if ((factor1.IsInteger) && (factor2.IsInteger))
				return new NumericValue(factor1.GetInteger * factor2.GetInteger, units);

			return new NumericValue(factor1.GetFloat * factor2.GetFloat, units);
		}

		public static NumericValue operator /(NumericValue dividend, NumericValue divisor)
		{
			var units = dividend.Units / divisor.Units;

			if ((dividend.IsInteger) && (divisor.IsInteger))
			{
				var int1 = dividend.GetInteger;
				var int2 = divisor.GetInteger;
				if ((int2 != 0) && ((int1 % int2) == 0))
					return new NumericValue(int1 / int2, units);
			}

			return new NumericValue(dividend.GetFloat / divisor.GetFloat, units);
		}

		public NumericValue IntDiv(NumericValue divisor) => new NumericValue(GetInteger / divisor.GetInteger, Units / divisor.Units);

		public string IntDivWithRemainder(NumericValue divisor)
		{
			var quotient = GetInteger / divisor.GetInteger;
			var remainder = GetInteger % divisor.GetInteger;
			var units = Units / divisor.Units;
			return $"{quotient}r{remainder} {units}";
		}

		public static NumericValue operator %(NumericValue dividend, NumericValue divisor)
		{
			var units = dividend.Units / divisor.Units;

			if ((dividend.IsInteger) && (divisor.IsInteger))
				return new NumericValue(dividend.GetInteger % divisor.GetInteger, units);

			return new NumericValue(dividend.GetFloat % divisor.GetFloat, units);
		}

		static NumericValue GetSameUnits(NumericValue value, NumericValue unitsValue) => value.Units.Equals(unitsValue.Units) ? value : value.ConvertUnits(unitsValue.Units);

		public static NumericValue operator +(NumericValue addend1, NumericValue addend2)
		{
			addend2 = GetSameUnits(addend2, addend1);

			if ((addend1.IsInteger) && (addend2.IsInteger))
				return new NumericValue(addend1.GetInteger + addend2.GetInteger, addend1.Units);

			return new NumericValue(addend1.GetFloat + addend2.GetFloat, addend1.Units);
		}

		public static NumericValue operator -(NumericValue minuend, NumericValue subtrahend)
		{
			subtrahend = GetSameUnits(subtrahend, minuend);

			if ((minuend.IsInteger) && (subtrahend.IsInteger))
				return new NumericValue(minuend.GetInteger - subtrahend.GetInteger, minuend.Units);

			return new NumericValue(minuend.GetFloat - subtrahend.GetFloat, minuend.Units);
		}

		public NumericValue ShiftLeft(NumericValue val2)
		{
			val2.VerifyNoUnits();
			return new NumericValue(GetInteger << val2.IntValue, Units);
		}

		public NumericValue ShiftRight(NumericValue val2)
		{
			val2.VerifyNoUnits();
			return new NumericValue(GetInteger >> val2.IntValue, Units);
		}

		public static bool operator ==(NumericValue val1, NumericValue val2)
		{
			if (!val1.Units.Equals(val2.Units))
			{
				var conversion1 = ExpressionUnitsConversion.GetBaseConversion(val1.Units);
				var conversion2 = ExpressionUnitsConversion.GetBaseConversion(val2.Units);
				if (!conversion1.toUnits.Equals(conversion2.toUnits))
					return false; // Units don't match
				val2 = GetSameUnits(val2, val1);
			}

			return (val1.IsInteger) && (val2.IsInteger) ? val1.GetInteger == val2.GetInteger : val1.GetFloat == val2.GetFloat;
		}

		public static bool operator !=(NumericValue val1, NumericValue val2)
		{
			if (!val1.Units.Equals(val2.Units))
			{
				var conversion1 = ExpressionUnitsConversion.GetBaseConversion(val1.Units);
				var conversion2 = ExpressionUnitsConversion.GetBaseConversion(val2.Units);
				if (!conversion1.toUnits.Equals(conversion2.toUnits))
					return true; // Units don't match
				val2 = GetSameUnits(val2, val1);
			}

			return (val1.IsInteger) && (val2.IsInteger) ? val1.GetInteger != val2.GetInteger : val1.GetFloat != val2.GetFloat;
		}

		public override bool Equals(object obj) => base.Equals(obj);

		public override int GetHashCode() => base.GetHashCode();

		public static bool operator <(NumericValue val1, NumericValue val2)
		{
			val2 = GetSameUnits(val2, val1);
			return (val1.IsInteger) && (val2.IsInteger) ? val1.GetInteger < val2.GetInteger : val1.GetFloat < val2.GetFloat;
		}

		public static bool operator <=(NumericValue val1, NumericValue val2)
		{
			val2 = GetSameUnits(val2, val1);
			return (val1.IsInteger) && (val2.IsInteger) ? val1.GetInteger <= val2.GetInteger : val1.GetFloat <= val2.GetFloat;
		}

		public static bool operator >(NumericValue val1, NumericValue val2)
		{
			val2 = GetSameUnits(val2, val1);
			return (val1.IsInteger) && (val2.IsInteger) ? val1.GetInteger > val2.GetInteger : val1.GetFloat > val2.GetFloat;
		}

		public static bool operator >=(NumericValue val1, NumericValue val2)
		{
			val2 = GetSameUnits(val2, val1);
			return (val1.IsInteger) && (val2.IsInteger) ? val1.GetInteger >= val2.GetInteger : val1.GetFloat >= val2.GetFloat;
		}

		public static NumericValue operator &(NumericValue val1, NumericValue val2)
		{
			val1.VerifyNoUnits();
			val2.VerifyNoUnits();
			return new NumericValue(val1.GetInteger & val2.GetInteger);
		}

		public static NumericValue operator ^(NumericValue val1, NumericValue val2)
		{
			val1.VerifyNoUnits();
			val2.VerifyNoUnits();
			return new NumericValue(val1.GetInteger ^ val2.GetInteger);
		}

		public static NumericValue operator |(NumericValue val1, NumericValue val2)
		{
			val1.VerifyNoUnits();
			val2.VerifyNoUnits();
			return new NumericValue(val1.GetInteger | val2.GetInteger);
		}

		public NumericValue ConvertUnits(ExpressionUnits units)
		{
			var conversion = ExpressionUnitsConversion.GetConversion(Units, units);
			var mult = new NumericValue(conversion.mult, conversion.toUnits / conversion.fromUnits);
			var add = new NumericValue(conversion.add, conversion.toUnits);
			return this * mult + add;
		}

		public NumericValue StripUnits()
		{
			if (!Units.HasUnits)
				return this;
			return new NumericValue(Value);
		}

		public NumericValue SetUnits(NumericValue units)
		{
			if (Units.HasUnits)
				throw new Exception("Already has units");
			return new NumericValue(Value, units.Units);
		}

		public string ToWords()
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

				magnitude.Add(string.Join(" ", strs));
			}

			var result = new List<string>();
			if (negative)
				result.Add("negative");

			for (var ctr = magnitude.Count - 1; ctr >= 0; --ctr)
			{
				if (string.IsNullOrEmpty(magnitude[ctr]))
					continue;
				result.Add(magnitude[ctr]);
				if (magnitudes[ctr] != null)
					result.Add(magnitudes[ctr]);
			}

			if (result.Count == 0)
				result.Add("zero");

			var resultStr = string.Join(" ", result);
			resultStr = char.ToUpperInvariant(resultStr[0]) + resultStr.Substring(1);
			return resultStr;
		}

		public static NumericValue Multiple(NumericValue number, NumericValue multiple)
		{
			number = GetSameUnits(number, multiple);

			if ((number.IsInteger) && (multiple.IsInteger))
				return new NumericValue((number.GetInteger + multiple.GetInteger - 1) / multiple.GetInteger * multiple.GetInteger, multiple.Units);

			return new NumericValue(Math.Ceiling(number.GetFloat / multiple.GetFloat) * multiple.GetFloat, multiple.Units);
		}

		static ThreadSafeRandom random = new ThreadSafeRandom();
		public static NumericValue Random(NumericValue minValue, NumericValue maxValue) => new NumericValue(random.Next((int)minValue.GetInteger, (int)maxValue.GetInteger + 1));

		public static NumericValue FromWords(string words)
		{
			var ones = new List<string> { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", null, "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };
			var tens = new List<string> { null, "ten", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };
			var magnitudes = new List<string> { null, "thousand", "million", "billion", "trillion", "quadrillion", "quintillion", "sextillion", "septillion", "octillion", "nonillion", "decillion", "undecillion", "duodecillion", "tredecillion", "quattuordecillion", "quindecillion", "sexdecillion", "septdecillion", "octodecillion", "novemdecillion", "vigintillion", "unvigintillion", "duovigintillion", "trevigintillion", "quattuorvigintillion", "quinvigintillion", "sexvigintillion", "septvigintillion", "octovigintillion", "novemvigintillion", "trigintillion", "untrigintillion", "duotrigintillion", "googol", "tretrigintillion", "quattuortrigintillion", "quintrigintillion", "sextrigintillion", "septtrigintillion", "octotrigintillion", "novemtrigintillion", "quadragintillion", "unquadragintillion", "duoquadragintillion", "trequadragintillion", "quattuorquadragintillion", "quinquadragintillion", "sexquadragintillion", "septquadragintillion", "octoquadragintillion", "novemquadragintillion", "quinquagintillion", "unquinquagintillion", "duoquinquagintillion", "trequinquagintillion", "quattuorquinquagintillion", "quinquinquagintillion", "sexquinquagintillion", "septquinquagintillion", "octoquinquagintillion", "novemquinquagintillion", "sexagintillion", "unsexagintillion", "duosexagintillion", "tresexagintillion", "quattuorsexagintillion", "quinsexagintillion", "sexsexagintillion", "septsexagintillion", "octosexagintillion", "novemsexagintillion", "septuagintillion", "unseptuagintillion", "duoseptuagintillion", "treseptuagintillion", "quattuorseptuagintillion", "quinseptuagintillion", "sexseptuagintillion", "septseptuagintillion", "octoseptuagintillion", "novemseptuagintillion", "octogintillion", "unoctogintillion", "duooctogintillion", "treoctogintillion", "quattuoroctogintillion", "quinoctogintillion", "sexoctogintillion", "septoctogintillion", "octooctogintillion", "novemoctogintillion", "nonagintillion", "unnonagintillion", "duononagintillion", "trenonagintillion", "quattuornonagintillion", "quinnonagintillion", "sexnonagintillion", "septnonagintillion", "octononagintillion", "novemnonagintillion", "centillion" };

			words = Regex.Replace(Regex.Replace(words.ToLowerInvariant(), "[^0-9.a-z]", " "), "\\s+", " ").Trim();
			var tokens = words.Split(' ').Where(str => !string.IsNullOrWhiteSpace(str)).ToList();

			double current = 0;
			var result = new NumericValue(BigInteger.Zero);
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
					if ((token != null) && (double.TryParse(token, out tokenValue)))
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
						var value = new NumericValue(current);
						var magFactor = new NumericValue(1000);
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

		NumericValue ToRad()
		{
			if (!Units.HasUnits)
				return new NumericValue(Value, "rad");
			return ConvertUnits(new ExpressionUnits("rad"));
		}

		public NumericValue Abs()
		{
			if (IsInteger)
				return new NumericValue(BigInteger.Abs(GetInteger), Units);
			return new NumericValue(Math.Abs(GetFloat), Units);
		}

		public NumericValue Ln() => new NumericValue(Math.Log(GetFloat), Units);

		public NumericValue Log() => new NumericValue(Math.Log10(GetFloat), Units);

		public NumericValue Log(NumericValue newBase)
		{
			VerifyNoUnits();
			return new NumericValue(Math.Log(GetFloat, newBase.GetFloat), Units);
		}

		public NumericValue Reciprocal() => new NumericValue(1.0 / GetFloat, Units);

		public NumericValue Root(NumericValue rootObj)
		{
			VerifyNoUnits();

			var root = (int)rootObj.GetInteger;
			var f = GetFloat;
			if (root % 2 == 1) // Odd roots
				return new NumericValue(f >= 0 ? Math.Pow(f, 1.0 / root) : -Math.Pow(-f, 1.0 / root), Units);
			else if (f >= 0) // Even roots, val >= 0
				return new NumericValue(Math.Pow(f, 1.0 / root), Units);

			throw new Exception("Invalid root");
		}

		public static NumericValue GCF(List<NumericValue> inputs)
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
				gcf = Helpers.GCF(gcf, factors[ctr]);
			return new NumericValue(gcf);
		}

		public static NumericValue LCM(List<NumericValue> inputs)
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
				lcm *= factors[ctr] / Helpers.GCF(lcm, factors[ctr]);
			return new NumericValue(lcm);
		}

		public static string Reduce(NumericValue numerator, NumericValue denominator)
		{
			if ((numerator.Units.HasUnits) || (numerator.Units.HasUnits))
				throw new Exception("Inputs cannot have units");
			var num = numerator.Abs().GetInteger;
			var den = denominator.Abs().GetInteger;
			if ((num == 0) || (den == 0))
				throw new Exception("Factors cannot be 0");

			var gcf = Helpers.GCF(num, den);
			return $"{num / gcf}/{den / gcf}";
		}

		public string Factor()
		{
			if (Units.HasUnits)
				throw new Exception("Input cannot have units");

			var num = GetInteger;
			if (num == 0)
				return "0";

			var factors = new List<BigInteger>();
			if (num < 0)
			{
				num = -num;
				factors.Add(-1);
			}
			var factor = new BigInteger(2);
			while (num > 1)
			{
				var val = num / factor;
				if (val * factor != num)
				{
					++factor;
					continue;
				}
				factors.Add(factor);
				num = val;
			}

			return string.Join("*", factors);
		}

		public NumericValue Sin() => new NumericValue(Math.Sin(ToRad().GetFloat));

		public NumericValue Cos() => new NumericValue(Math.Cos(ToRad().GetFloat));

		public NumericValue Tan() => new NumericValue(Math.Tan(ToRad().GetFloat));

		public NumericValue Asin()
		{
			VerifyNoUnits();
			return new NumericValue(Math.Asin(GetFloat), "rad");
		}

		public NumericValue Acos()
		{
			VerifyNoUnits();
			return new NumericValue(Math.Acos(GetFloat), "rad");
		}

		public NumericValue Atan()
		{
			VerifyNoUnits();
			return new NumericValue(Math.Atan(GetFloat), "rad");
		}

		public static NumericValue Atan2(NumericValue xOfs, NumericValue yOfs)
		{
			xOfs.VerifyNoUnits();
			yOfs.VerifyNoUnits();
			return new NumericValue(Math.Atan2(xOfs.GetFloat, yOfs.GetFloat), "rad");
		}

		public object GetResult() => Units.HasUnits ? $"{Value} {Units}" : Value;

		public override string ToString() => GetResult().ToString();

		public int CompareTo(object obj)
		{
			if (!(obj is NumericValue))
				throw new ArgumentException("Invalid comparison");
			var result = obj as NumericValue;
			return GetFloat.CompareTo(result.GetFloat);
		}
	}
}
