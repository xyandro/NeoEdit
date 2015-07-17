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

		bool IsNumeric(object obj)
		{
			return (obj is sbyte) || (obj is byte) || (obj is short) || (obj is ushort) || (obj is int) || (obj is uint) || (obj is long) || (obj is ulong) || (obj is float) || (obj is double) || (obj is decimal) || (obj is Complex);
		}

		bool IsCharacter(object obj)
		{
			return (obj is char) || (obj is string);
		}

		Complex RoundComplex(Complex complex)
		{
			var real = Math.Round(complex.Real, 10);
			var imaginary = Math.Round(complex.Imaginary, 12);
			if (Math.Abs(real) < 1e-10)
				real = 0;
			if (Math.Abs(imaginary) < 1e-10)
				imaginary = 0;
			return new Complex(real, imaginary);
		}

		object SimplifyComplex(object value)
		{
			if (!(value is Complex))
				return value;

			var complex = RoundComplex((Complex)value);
			if (complex.Imaginary == 0)
				return complex.Real;
			return complex;
		}

		Complex GetComplex(object val)
		{
			if (val == null)
				throw new Exception("NULL value");
			if (val is Complex)
				return (Complex)val;
			return new Complex((double)Convert.ChangeType(val, typeof(double)), 0);
		}

		double GetDouble(object val)
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
			return (double)Convert.ChangeType(val, typeof(double));
		}

		bool GetBool(object val)
		{
			if (val == null)
				throw new Exception("NULL value");
			return (bool)Convert.ChangeType(val, typeof(bool));
		}

		char GetChar(object val)
		{
			if (val == null)
				throw new Exception("NULL value");
			return (char)Convert.ChangeType(val, typeof(char));
		}

		long GetLong(object val)
		{
			if (val == null)
				throw new Exception("NULL value");
			if (val is Complex)
			{
				var complex = (Complex)val;
				if (complex.Imaginary != 0)
					throw new Exception("Can't convert complex to double");
				val = complex.Real;
			}
			return (long)Convert.ChangeType(val, typeof(long));
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

		object UnaryOp(string op, object val)
		{
			switch (op)
			{
				case "~": return ~GetLong(val);
				case "+": return GetComplex(val);
				case "-": return -GetComplex(val);
				case "!": return !GetBool(val);
				default: throw new ArgumentException(String.Format("Invalid operation: {0}", op));
			}
		}

		void Swap(ref object obj1, ref object obj2)
		{
			var tmp = obj1;
			obj1 = obj2;
			obj2 = tmp;
		}

		object MultOp(object val1, object val2)
		{
			if ((val1 == null) || (val2 == null))
				throw new Exception("NULL value");

			if ((IsCharacter(val2)) && (IsNumeric(val1)))
				Swap(ref val1, ref val2);
			if ((IsCharacter(val1)) && (IsNumeric(val2)))
			{
				var str = GetString(val1);
				var count = (int)GetLong(val2);
				var sb = new StringBuilder(str.Length * count);
				for (var ctr = 0; ctr < count; ++ctr)
					sb.Append(str);
				return sb.ToString();
			}

			return GetComplex(val1) * GetComplex(val2);
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

			if ((val2 is char) && (IsNumeric(val1)))
				Swap(ref val1, ref val2);
			if ((val1 is char) && (IsNumeric(val2)))
				return (char)((long)GetChar(val1) + GetLong(val2));

			if ((IsCharacter(val1)) || (IsCharacter(val2)))
				return GetString(val1) + GetString(val2);

			return GetComplex(val1) + GetComplex(val2);
		}

		object SubOp(object val1, object val2)
		{
			if ((val1 == null) || (val2 == null))
				throw new Exception("NULL value");

			if ((val2 is char) && (IsNumeric(val1)))
				Swap(ref val1, ref val2);
			if ((val1 is char) && (IsNumeric(val2)))
				return (char)((long)GetChar(val1) - GetLong(val2));

			return GetComplex(val1) - GetComplex(val2);
		}

		bool EqualsOp(object val1, object val2, bool ignoreCase)
		{
			if ((val1 == null) && (val2 == null))
				return true;
			if ((val1 == null) || (val2 == null))
				return false;

			if ((IsNumeric(val1)) && (IsNumeric(val2)))
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

			return GetDouble(val1) < GetDouble(val2);
		}

		bool GreaterThanOp(object val1, object val2, bool ignoreCase)
		{
			if ((val1 == null) || (val2 == null))
				throw new Exception("NULL value");

			if ((IsCharacter(val1)) && (IsCharacter(val2)))
				return String.Compare(GetString(val1), GetString(val2), ignoreCase) > 0;

			return GetDouble(val1) > GetDouble(val2);
		}

		object BinaryOp(string op, object val1, object val2)
		{
			switch (op)
			{
				case ".": return DotOp(val1, (val2 ?? "").ToString());
				case "^": return Complex.Pow(GetComplex(val1), GetComplex(val2));
				case "*": return MultOp(val1, val2);
				case "/": return GetComplex(val1) / GetComplex(val2);
				case "%": return GetDouble(val1) % GetDouble(val2);
				case "+": return AddOp(val1, val2);
				case "-": return SubOp(val1, val2);
				case "<<": return GetLong(val1) << (int)GetLong(val2);
				case ">>": return GetLong(val1) >> (int)GetLong(val2);
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
				case "&": return GetLong(val1) & GetLong(val2);
				case "^^": return GetLong(val1) ^ GetLong(val2);
				case "|": return GetLong(val1) | GetLong(val2);
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

		public override object VisitExpr(ExpressionParser.ExprContext context)
		{
			if ((context.DEBUG() != null) && (Debugger.IsAttached))
				Debugger.Break();
			var val = SimplifyComplex(Visit(context.form()));
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

		Complex GetRoot(Complex val, Complex root)
		{
			if ((root.Imaginary != 0) || (root.Real <= 1) || (root.Real - Math.Floor(root.Real) != 0))
				return Complex.Pow(val, Complex.Reciprocal(root));

			var power = (int)GetLong(root);
			var phase = val.Phase;
			var magnitude = val.Magnitude;
			var nthRootOfMagnitude = Math.Pow(magnitude, 1.0 / power);
			var options = Enumerable.Range(0, power).Select(k => RoundComplex(Complex.FromPolarCoordinates(nthRootOfMagnitude, phase / power + k * 2 * Math.PI / power))).ToList();
			return options.OrderBy(complex => Math.Abs(complex.Imaginary)).ThenByDescending(complex => complex.Real).ThenByDescending(complex => complex.Imaginary).FirstOrDefault();
		}

		public override object VisitMethod(ExpressionParser.MethodContext context)
		{
			var method = context.method.Text;
			var paramList = context.e().Select(c => Visit(c)).ToList();

			switch (method.ToLowerInvariant())
			{
				case "abs": return Complex.Abs(GetComplex(paramList[0]));
				case "acos": return Complex.Acos(GetComplex(paramList[0]));
				case "asin": return Complex.Asin(GetComplex(paramList[0]));
				case "atan": return Complex.Atan(GetComplex(paramList[0]));
				case "conjugate": return Complex.Conjugate(GetComplex(paramList[0]));
				case "cos": return Complex.Cos(GetComplex(paramList[0]));
				case "cosh": return Complex.Cosh(GetComplex(paramList[0]));
				case "eval": return new NEExpression(GetString(paramList[0])).Evaluate();
				case "filename": return Path.GetFileName(GetString(paramList[0]));
				case "frompolar": return Complex.FromPolarCoordinates(GetDouble(paramList[0]), GetDouble(paramList[1]));
				case "imaginary": return GetComplex(paramList[0]).Imaginary;
				case "ln": return Complex.Log(GetComplex(paramList[0]));
				case "log": return paramList.Count == 1 ? Complex.Log10(GetComplex(paramList[0])) : Complex.Log(GetComplex(paramList[0]), GetDouble(paramList[2]));
				case "magnitude": return GetComplex(paramList[0]).Magnitude;
				case "max": return paramList.Select(val => SimplifyComplex(val)).Max();
				case "min": return paramList.Select(val => SimplifyComplex(val)).Min();
				case "phase": return GetComplex(paramList[0]).Phase;
				case "real": return GetComplex(paramList[0]).Real;
				case "reciprocal": return Complex.Reciprocal(GetComplex(paramList[0]));
				case "root": return GetRoot(GetComplex(paramList[0]), GetComplex(paramList[1]));
				case "sin": return Complex.Sin(GetComplex(paramList[0]));
				case "sinh": return Complex.Sinh(GetComplex(paramList[0]));
				case "sqrt": return GetRoot(GetComplex(paramList[0]), 2);
				case "strformat": return String.Format(GetString(paramList[0]), paramList.Skip(1).Select(arg => SimplifyComplex(arg) ?? "").ToArray());
				case "tan": return Complex.Tan(GetComplex(paramList[0]));
				case "tanh": return Complex.Tanh(GetComplex(paramList[0]));
				case "type": return paramList[0].GetType();
				case "validre": return ValidRE(GetString(paramList[0]));
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
		public override object VisitFloat(ExpressionParser.FloatContext context) { return double.Parse(context.val.Text); }
		public override object VisitHex(ExpressionParser.HexContext context) { return long.Parse(context.val.Text.Substring(2), NumberStyles.HexNumber); }
		public override object VisitVariable(ExpressionParser.VariableContext context) { return dict.ContainsKey(context.val.Text) ? dict[context.val.Text] : null; }
	}
}
