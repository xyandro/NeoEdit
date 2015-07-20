using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using NeoEdit.Common.Expressions.Parser;

namespace NeoEdit.Common.Expressions
{
	class ExpressionEvaluator : ExpressionBaseVisitor<object>
	{
		readonly string expression;
		readonly Dictionary<string, object> dict;
		readonly List<object> values;
		internal ExpressionEvaluator(string expression, Dictionary<string, object> dict, params object[] values)
		{
			this.expression = expression;
			this.dict = dict;
			this.values = values.Select(value => CheckUnset(value)).ToList();
		}

		object CheckUnset(object val)
		{
			return (val != null) && (val.GetType().FullName == "MS.Internal.NamedObject") && (val.ToString() == "{DependencyProperty.UnsetValue}") ? null : val;
		}

		bool IsInteger(object obj)
		{
			return (obj is sbyte) || (obj is byte) || (obj is short) || (obj is ushort) || (obj is int) || (obj is uint) || (obj is long) || (obj is ulong) || (obj is BigInteger);
		}

		bool IsFloat(object obj)
		{
			return (IsInteger(obj)) || (obj is float) || (obj is double) || (obj is decimal);
		}

		bool IsComplex(object obj)
		{
			return (IsFloat(obj)) || (obj is Complex);
		}

		bool IsCharacter(object obj)
		{
			return (obj is char) || (obj is string);
		}

		Complex RoundComplex(Complex complex)
		{
			var real = Math.Round(complex.Real, 10);
			var imaginary = Math.Round(complex.Imaginary, 10);
			if (Math.Abs(real) < 1e-10)
				real = 0;
			if (Math.Abs(imaginary) < 1e-10)
				imaginary = 0;
			return new Complex(real, imaginary);
		}

		object Simplify(object value)
		{
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

			return value;
		}

		bool GetBool(object val)
		{
			if (val == null)
				throw new Exception("NULL value");
			return (bool)Convert.ChangeType(val, typeof(bool));
		}

		BigInteger GetInteger(object val)
		{
			if (val == null)
				throw new Exception("NULL value");
			if (val is BigInteger)
				return (BigInteger)val;
			if (val is Complex)
			{
				var complex = (Complex)val;
				if (complex.Imaginary != 0)
					throw new Exception("Can't convert complex to double");
				val = complex.Real;
			}
			return new BigInteger((long)Convert.ChangeType(val, typeof(long)));
		}

		double GetFloat(object val)
		{
			if (val == null)
				throw new Exception("NULL value");
			if (val is Complex)
			{
				var complex = (Complex)val;
				if (complex.Imaginary != 0)
					throw new Exception("Can't convert complex to double");
				return complex.Real;
			}
			if (val is BigInteger)
				return (double)(BigInteger)val;
			return (double)Convert.ChangeType(val, typeof(double));
		}

		Complex GetComplex(object val)
		{
			if (val == null)
				throw new Exception("NULL value");
			if (val is Complex)
				return (Complex)val;
			if (val is BigInteger)
				return (Complex)(BigInteger)val;
			return new Complex((double)Convert.ChangeType(val, typeof(double)), 0);
		}

		char GetChar(object val)
		{
			if (val == null)
				throw new Exception("NULL value");
			return (char)Convert.ChangeType(val, typeof(char));
		}

		string GetString(object val)
		{
			if (val == null)
				throw new Exception("NULL value");
			if (val is Complex)
			{
				var complex = (Complex)val;
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
			return val.ToString();
		}

		object DotOp(object obj, string fieldName)
		{
			if (obj == null)
				return null;
			var field = obj.GetType().GetProperty(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (field == null)
				return null;
			return field.GetValue(obj);
		}

		object NegateOp(object obj)
		{
			if (IsInteger(obj))
				return -GetInteger(obj);
			if (IsFloat(obj))
				return -GetFloat(obj);
			if (IsComplex(obj))
				return -GetComplex(obj);
			throw new Exception("Invalid operation");
		}

		object UnaryOp(string op, object val)
		{
			switch (op)
			{
				case "~": return ~GetInteger(val);
				case "+": return val;
				case "-": return NegateOp(val);
				case "!": return !GetBool(val);
				default: throw new ArgumentException(String.Format("Invalid operation: {0}", op));
			}
		}

		BigInteger Factorial(BigInteger num)
		{
			BigInteger result = 1;
			for (var ctr = 2; ctr <= num; ++ctr)
				result *= ctr;
			return result;
		}

		object UnaryOpEnd(string op, object val)
		{
			switch (op)
			{
				case "!": return Factorial(GetInteger(val));
				default: throw new ArgumentException(String.Format("Invalid operation: {0}", op));
			}
		}

		void Swap(ref object obj1, ref object obj2)
		{
			var tmp = obj1;
			obj1 = obj2;
			obj2 = tmp;
		}

		object ExpOp(object val1, object val2)
		{
			if ((IsInteger(val1)) && (IsInteger(val2)))
				return BigInteger.Pow(GetInteger(val1), (int)GetInteger(val2));
			if ((IsFloat(val1)) && (IsFloat(val2)))
			{
				var val = Math.Pow(GetFloat(val1), GetFloat(val2));
				if (!double.IsNaN(val))
					return val;
			}

			return Complex.Pow(GetComplex(val1), GetComplex(val2));
		}

		object MultOp(object val1, object val2)
		{
			if ((val1 == null) || (val2 == null))
				throw new Exception("NULL value");

			if ((IsCharacter(val2)) && (IsComplex(val1)))
				Swap(ref val1, ref val2);
			if ((IsCharacter(val1)) && (IsComplex(val2)))
			{
				var str = GetString(val1);
				var count = (int)GetInteger(val2);
				var sb = new StringBuilder(str.Length * count);
				for (var ctr = 0; ctr < count; ++ctr)
					sb.Append(str);
				return sb.ToString();
			}

			if ((IsInteger(val1)) && (IsInteger(val2)))
				return GetInteger(val1) * GetInteger(val2);

			if ((IsFloat(val1)) && (IsFloat(val2)))
				return GetFloat(val1) * GetFloat(val2);

			return GetComplex(val1) * GetComplex(val2);
		}

		object DivOp(object val1, object val2)
		{
			if ((val1 == null) || (val2 == null))
				throw new Exception("NULL value");

			if ((IsInteger(val1)) && (IsInteger(val2)))
			{
				var int1 = GetInteger(val1);
				var int2 = GetInteger(val2);
				if ((int2 != 0) && ((int1 % int2) == 0))
					return int1 / int2;
			}

			if ((IsFloat(val1)) && (IsFloat(val2)))
				return GetFloat(val1) / GetFloat(val2);

			return GetComplex(val1) / GetComplex(val2);
		}

		object ModOp(object val1, object val2)
		{
			if ((val1 == null) || (val2 == null))
				throw new Exception("NULL value");

			if ((IsInteger(val1)) && (IsInteger(val2)))
				return GetInteger(val1) % GetInteger(val2);

			return GetFloat(val1) % GetFloat(val2);
		}

		object AddOp(object val1, object val2)
		{
			if ((val1 == null) || (val2 == null))
			{
				var val = val1 ?? val2;
				if (IsCharacter(val))
					return val;
				throw new Exception("NULL value");
			}

			if ((val2 is char) && (IsComplex(val1)))
				Swap(ref val1, ref val2);
			if ((val1 is char) && (IsComplex(val2)))
				return (char)((long)GetChar(val1) + GetInteger(val2));

			if ((IsCharacter(val1)) || (IsCharacter(val2)))
				return GetString(val1) + GetString(val2);

			if ((IsInteger(val1)) && (IsInteger(val2)))
				return GetInteger(val1) + GetInteger(val2);

			if ((IsFloat(val1)) && (IsFloat(val2)))
				return GetFloat(val1) + GetFloat(val2);

			return GetComplex(val1) + GetComplex(val2);
		}

		object SubOp(object val1, object val2)
		{
			if ((val1 == null) || (val2 == null))
				throw new Exception("NULL value");

			if ((val2 is char) && (IsComplex(val1)))
				Swap(ref val1, ref val2);
			if ((val1 is char) && (IsComplex(val2)))
				return (char)((long)GetChar(val1) - GetInteger(val2));

			if ((IsInteger(val1)) && (IsInteger(val2)))
				return GetInteger(val1) - GetInteger(val2);

			if ((IsFloat(val1)) && (IsFloat(val2)))
				return GetFloat(val1) - GetFloat(val2);

			return GetComplex(val1) - GetComplex(val2);
		}

		bool EqualsOp(object val1, object val2, bool ignoreCase)
		{
			if ((val1 == null) && (val2 == null))
				return true;
			if ((val1 == null) || (val2 == null))
				return false;

			if ((IsInteger(val1)) && (IsInteger(val2)))
				return GetInteger(val1) == GetInteger(val2);

			if ((IsFloat(val1)) && (IsFloat(val2)))
				return GetFloat(val1) == GetFloat(val2);

			if ((IsComplex(val1)) && (IsComplex(val2)))
				return GetComplex(val1) == GetComplex(val2);

			if ((IsCharacter(val1)) && (IsCharacter(val2)))
				return GetString(val1).Equals(GetString(val2), ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

			return val1.Equals(val2);
		}

		bool LessThanOp(object val1, object val2, bool ignoreCase)
		{
			if ((val1 == null) || (val2 == null))
				throw new Exception("NULL value");

			if ((IsCharacter(val1)) && (IsCharacter(val2)))
				return String.Compare(GetString(val1), GetString(val2), ignoreCase) < 0;

			if ((IsInteger(val1)) && (IsInteger(val2)))
				return GetInteger(val1) < GetInteger(val2);

			return GetFloat(val1) < GetFloat(val2);
		}

		bool GreaterThanOp(object val1, object val2, bool ignoreCase)
		{
			if ((val1 == null) || (val2 == null))
				throw new Exception("NULL value");

			if ((IsCharacter(val1)) && (IsCharacter(val2)))
				return String.Compare(GetString(val1), GetString(val2), ignoreCase) > 0;

			if ((IsInteger(val1)) && (IsInteger(val2)))
				return GetInteger(val1) > GetInteger(val2);

			return GetFloat(val1) > GetFloat(val2);
		}

		object BinaryOp(string op, object val1, object val2)
		{
			switch (op)
			{
				case ".": return DotOp(val1, (val2 ?? "").ToString());
				case "^": return ExpOp(val1, val2);
				case "*": return MultOp(val1, val2);
				case "/": return DivOp(val1, val2);
				case "%": return ModOp(val1, val2);
				case "+": return AddOp(val1, val2);
				case "-": return SubOp(val1, val2);
				case "<<": return GetInteger(val1) << (int)GetInteger(val2);
				case ">>": return GetInteger(val1) >> (int)GetInteger(val2);
				case "<": return LessThanOp(val1, val2, false);
				case "i<": return LessThanOp(val1, val2, true);
				case ">": return GreaterThanOp(val1, val2, false);
				case "i>": return GreaterThanOp(val1, val2, true);
				case "<=": return !GreaterThanOp(val1, val2, false);
				case "i<=": return !GreaterThanOp(val1, val2, true);
				case ">=": return !LessThanOp(val1, val2, false);
				case "i>=": return !LessThanOp(val1, val2, true);
				case "is": return val1 == null ? false : val1.GetType().Name == (val2 ?? "").ToString();
				case "==": return EqualsOp(val1, val2, false);
				case "i==": return EqualsOp(val1, val2, true);
				case "!=": return !EqualsOp(val1, val2, false);
				case "i!=": return !EqualsOp(val1, val2, true);
				case "&": return GetInteger(val1) & GetInteger(val2);
				case "^^": return GetInteger(val1) ^ GetInteger(val2);
				case "|": return GetInteger(val1) | GetInteger(val2);
				case "&&": return GetBool(val1) && GetBool(val2);
				case "||": return GetBool(val1) || GetBool(val2);
				case "??": return val1 ?? val2;
				default: throw new ArgumentException(String.Format("Invalid operation: {0}", op));
			}
		}

		bool ValidRE(string re)
		{
			try { new Regex(re); return true; }
			catch { return false; }
		}

		string WordsMethod(BigInteger num)
		{
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
			return resultStr;
		}

		public override object VisitExpr(ExpressionParser.ExprContext context)
		{
			if ((context.DEBUG() != null) && (Debugger.IsAttached))
				Debugger.Break();
			var val = Simplify(Visit(context.form()));
			if (val is Complex)
				return GetString(val);
			return val;
		}

		object GetShortForm(string op)
		{
			var values = new Queue<object>(this.values);
			object result = null;
			bool first = true;
			while (values.Any())
			{
				var val2 = values.Dequeue();
				result = first ? val2 : BinaryOp(op, result, val2);
				first = false;
			}
			return result;
		}

		object AbsMethod(object val)
		{
			if (IsInteger(val))
				return BigInteger.Abs(GetInteger(val));
			if (IsFloat(val))
				return Math.Abs(GetFloat(val));
			return Complex.Abs(GetComplex(val));
		}

		object AcosMethod(object val)
		{
			if (IsFloat(val))
				return Math.Acos(GetFloat(val));
			return Complex.Abs(GetComplex(val));
		}

		object AsinMethod(object val)
		{
			if (IsFloat(val))
				return Math.Asin(GetFloat(val));
			return Complex.Asin(GetComplex(val));
		}

		object AtanMethod(object val)
		{
			if (IsFloat(val))
				return Math.Atan(GetFloat(val));
			return Complex.Atan(GetComplex(val));
		}

		object CoshMethod(object val)
		{
			if (IsFloat(val))
				return Math.Cosh(GetFloat(val));
			return Complex.Cosh(GetComplex(val));
		}

		object CosMethod(object val)
		{
			if (IsFloat(val))
				return Math.Cos(GetFloat(val));
			return Complex.Cos(GetComplex(val));
		}

		object LnMethod(object val)
		{
			if (IsFloat(val))
				return Math.Log(GetFloat(val));
			return Complex.Log(GetComplex(val));
		}

		object LogMethod(object val)
		{
			if (IsFloat(val))
				return Math.Log10(GetFloat(val));
			return Complex.Log10(GetComplex(val));
		}

		object LogMethod(object val, object newBase)
		{
			if ((IsFloat(val)) && (IsFloat(newBase)))
				return Math.Log(GetFloat(val), GetFloat(newBase));
			return Complex.Log(GetComplex(val), GetFloat(newBase));
		}

		object ReciprocalMethod(object val)
		{
			if (IsFloat(val))
				return 1.0 / GetFloat(val);
			return Complex.Reciprocal(GetComplex(val));
		}

		object RootMethod(object val, object rootObj)
		{
			var root = (int)GetInteger(rootObj);
			if (IsFloat(val))
			{
				var f = GetFloat(val);
				if (root % 2 == 1) // Odd roots
					return f >= 0 ? Math.Pow(f, 1.0 / root) : -Math.Pow(-f, 1.0 / root);
				else if (f >= 0) // Even roots, val >= 0
					return Math.Pow(f, 1.0 / root);
			}

			var complexVal = GetComplex(val);
			var phase = complexVal.Phase;
			var magnitude = complexVal.Magnitude;
			var nthRootOfMagnitude = Math.Pow(magnitude, 1.0 / root);
			var options = Enumerable.Range(0, root).Select(k => RoundComplex(Complex.FromPolarCoordinates(nthRootOfMagnitude, phase / root + k * 2 * Math.PI / root))).ToList();
			return options.OrderBy(complex => Math.Abs(complex.Imaginary)).ThenByDescending(complex => complex.Real).ThenByDescending(complex => complex.Imaginary).FirstOrDefault();
		}

		object SinhMethod(object val)
		{
			if (IsFloat(val))
				return Math.Sinh(GetFloat(val));
			return Complex.Sinh(GetComplex(val));
		}

		object SinMethod(object val)
		{
			if (IsFloat(val))
				return Math.Sin(GetFloat(val));
			return Complex.Sin(GetComplex(val));
		}

		object TanhMethod(object val)
		{
			if (IsFloat(val))
				return Math.Tanh(GetFloat(val));
			return Complex.Tanh(GetComplex(val));
		}

		object TanMethod(object val)
		{
			if (IsFloat(val))
				return Math.Tan(GetFloat(val));
			return Complex.Tan(GetComplex(val));
		}

		public override object VisitMethod(ExpressionParser.MethodContext context)
		{
			var method = context.method.Text;
			var paramList = context.e().Select(c => Visit(c)).ToList();

			switch (method.ToLowerInvariant())
			{
				case "abs": return AbsMethod(paramList[0]);
				case "acos": return AcosMethod(paramList[0]);
				case "asin": return AsinMethod(paramList[0]);
				case "atan": return AtanMethod(paramList[0]);
				case "conjugate": return Complex.Conjugate(GetComplex(paramList[0]));
				case "cos": return CosMethod(paramList[0]);
				case "cosh": return CoshMethod(paramList[0]);
				case "eval": return new NEExpression(GetString(paramList[0])).Evaluate();
				case "filename": return Path.GetFileName(GetString(paramList[0]));
				case "frompolar": return Complex.FromPolarCoordinates(GetFloat(paramList[0]), GetFloat(paramList[1]));
				case "imaginary": return GetComplex(paramList[0]).Imaginary;
				case "ln": return LnMethod(paramList[0]);
				case "log": return paramList.Count == 1 ? LogMethod(paramList[0]) : LogMethod(paramList[0], paramList[2]);
				case "magnitude": return GetComplex(paramList[0]).Magnitude;
				case "max": return paramList.Select(val => Simplify(val)).Max();
				case "min": return paramList.Select(val => Simplify(val)).Min();
				case "phase": return GetComplex(paramList[0]).Phase;
				case "real": return GetComplex(paramList[0]).Real;
				case "reciprocal": return ReciprocalMethod(paramList[0]);
				case "root": return RootMethod(paramList[0], paramList[1]);
				case "sin": return SinMethod(paramList[0]);
				case "sinh": return SinhMethod(paramList[0]);
				case "sqrt": return RootMethod(paramList[0], 2);
				case "strformat": return String.Format(GetString(paramList[0]), paramList.Skip(1).Select(arg => Simplify(arg) ?? "").ToArray());
				case "tan": return TanMethod(paramList[0]);
				case "tanh": return TanhMethod(paramList[0]);
				case "type": return paramList[0].GetType();
				case "validre": return ValidRE(GetString(paramList[0]));
				case "words": return WordsMethod(GetInteger(paramList[0]));
				default: throw new ArgumentException(String.Format("Invalid method: {0}", method));
			}
		}

		public override object VisitConstant(ExpressionParser.ConstantContext context)
		{
			var constant = context.constant.Text;
			switch (constant.ToLowerInvariant())
			{
				case "pi": return Math.PI;
				case "e": return Math.E;
				case "i": return Complex.ImaginaryOne;
				default: throw new ArgumentException(String.Format("Invalid constant: {0}", constant));
			}
		}

		public override object VisitShortForm(ExpressionParser.ShortFormContext context) { return GetShortForm(context.op.Text); }
		public override object VisitDefaultOpForm(ExpressionParser.DefaultOpFormContext context) { return GetShortForm("&&"); }
		public override object VisitDot(ExpressionParser.DotContext context) { return BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2)); }
		public override object VisitUnary(ExpressionParser.UnaryContext context) { return UnaryOp(context.op.Text, Visit(context.val)); }
		public override object VisitUnaryEnd(ExpressionParser.UnaryEndContext context) { return UnaryOpEnd(context.op.Text, Visit(context.val)); }
		public override object VisitExp(ExpressionParser.ExpContext context) { return BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2)); }
		public override object VisitMult(ExpressionParser.MultContext context) { return BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2)); }
		public override object VisitAdd(ExpressionParser.AddContext context) { return BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2)); }
		public override object VisitShift(ExpressionParser.ShiftContext context) { return BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2)); }
		public override object VisitRelational(ExpressionParser.RelationalContext context) { return BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2)); }
		public override object VisitEquality(ExpressionParser.EqualityContext context) { return BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2)); }
		public override object VisitBitwiseAnd(ExpressionParser.BitwiseAndContext context) { return BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2)); }
		public override object VisitBitwiseXor(ExpressionParser.BitwiseXorContext context) { return BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2)); }
		public override object VisitBitwiseOr(ExpressionParser.BitwiseOrContext context) { return BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2)); }
		public override object VisitLogicalAnd(ExpressionParser.LogicalAndContext context) { return BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2)); }
		public override object VisitLogicalOr(ExpressionParser.LogicalOrContext context) { return BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2)); }
		public override object VisitNullCoalesce(ExpressionParser.NullCoalesceContext context) { return BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2)); }
		public override object VisitTernary(ExpressionParser.TernaryContext context) { return GetBool(Visit(context.condition)) ? Visit(context.trueval) : Visit(context.falseval); }
		public override object VisitExpression(ExpressionParser.ExpressionContext context) { return Visit(context.val); }
		public override object VisitParam(ExpressionParser.ParamContext context) { return values[int.Parse(context.val.Text.Trim('[', ']'))]; }
		public override object VisitString(ExpressionParser.StringContext context) { return context.val.Text.Substring(1, context.val.Text.Length - 2); }
		public override object VisitChar(ExpressionParser.CharContext context) { return context.val.Text[1]; }
		public override object VisitTrue(ExpressionParser.TrueContext context) { return true; }
		public override object VisitFalse(ExpressionParser.FalseContext context) { return false; }
		public override object VisitNull(ExpressionParser.NullContext context) { return null; }
		public override object VisitFloat(ExpressionParser.FloatContext context)
		{
			if (context.val.Text.Contains("."))
				return Simplify(double.Parse(context.val.Text));
			return Simplify(BigInteger.Parse(context.val.Text));
		}
		public override object VisitHex(ExpressionParser.HexContext context) { return long.Parse(context.val.Text.Substring(2), NumberStyles.HexNumber); }
		public override object VisitVariable(ExpressionParser.VariableContext context) { return dict.ContainsKey(context.val.Text) ? dict[context.val.Text] : null; }
	}
}
