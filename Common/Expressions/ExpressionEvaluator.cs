using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
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

		bool IsIntType(object obj)
		{
			return (obj is sbyte) || (obj is byte) || (obj is short) || (obj is ushort) || (obj is int) || (obj is uint) || (obj is long) || (obj is ulong);
		}

		bool IsFloatType(object obj)
		{
			return (obj is float) || (obj is double) || (obj is decimal);
		}

		bool IsNumericType(object obj)
		{
			return (IsIntType(obj)) || (IsFloatType(obj));
		}

		bool IsCharacterType(object obj)
		{
			return (obj is char) || (obj is string);
		}

		Type GetType(string type)
		{
			switch (type)
			{
				case "bool": return typeof(bool);
				case "char": return typeof(char);
				case "sbyte": return typeof(sbyte);
				case "byte": return typeof(byte);
				case "short": return typeof(short);
				case "ushort": return typeof(ushort);
				case "int": return typeof(int);
				case "uint": return typeof(uint);
				case "long": return typeof(long);
				case "ulong": return typeof(ulong);
				case "float": return typeof(float);
				case "double": return typeof(double);
				case "string": return typeof(string);
				default: throw new ArgumentException(String.Format("Invalid cast: {0}", type));
			}
		}

		object CastValue(object val, Type type)
		{
			if (val == null)
			{
				if (!type.IsValueType)
					return null;
				throw new Exception("NULL value");
			}
			return Convert.ChangeType(val, type);
		}

		T ToType<T>(object val)
		{
			return (T)CastValue(val, typeof(T));
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

		object PosOp(object val)
		{
			if (val == null)
				throw new Exception("NULL value");

			if (IsNumericType(val))
			{
				if (IsFloatType(val))
					return ToType<double>(val);
				return ToType<long>(val);
			}

			throw new Exception("Invalid operation");
		}

		object NegOp(object val)
		{
			if (val == null)
				throw new Exception("NULL value");

			if (IsNumericType(val))
			{
				if (IsFloatType(val))
					return -ToType<double>(val);
				return -ToType<long>(val);
			}

			throw new Exception("Invalid operation");
		}

		object UnaryOp(string op, object val)
		{
			switch (op)
			{
				case "~": return ~ToType<long>(val);
				case "+": return PosOp(val);
				case "-": return NegOp(val);
				case "!": return !ToType<bool>(val);
				default: throw new ArgumentException(String.Format("Invalid operation: {0}", op));
			}
		}

		object ExpOp(object val1, object val2)
		{
			if ((val1 == null) || (val2 == null))
				throw new Exception("NULL value");

			if ((IsNumericType(val1)) && (IsNumericType(val2)))
			{
				if ((IsFloatType(val1)) || (IsFloatType(val2)))
					return Math.Pow(ToType<double>(val1), ToType<double>(val2));
				return Convert.ToInt64(Math.Pow(ToType<long>(val1), ToType<long>(val2)));
			}

			throw new Exception("Invalid operation");
		}

		object MultOp(object val1, object val2)
		{
			if ((val1 == null) || (val2 == null))
				throw new Exception("NULL value");

			if ((IsCharacterType(val1)) && (IsIntType(val2)))
			{
				var str = val1.ToString();
				var count = ToType<int>(val2);
				var sb = new StringBuilder(str.Length * count);
				for (var ctr = 0; ctr < count; ++ctr)
					sb.Append(str);
				return sb.ToString();
			}

			if ((IsNumericType(val1)) && (IsNumericType(val2)))
			{
				if ((IsFloatType(val1)) || (IsFloatType(val2)))
					return ToType<double>(val1) * ToType<double>(val2);
				return ToType<long>(val1) * ToType<long>(val2);
			}

			throw new Exception("Invalid operation");
		}

		object DivOp(object val1, object val2)
		{
			if ((val1 == null) || (val2 == null))
				throw new Exception("NULL value");

			if ((IsNumericType(val1)) && (IsNumericType(val2)))
			{
				if ((IsFloatType(val1)) || (IsFloatType(val2)))
					return ToType<double>(val1) / ToType<double>(val2);
				return ToType<long>(val1) / ToType<long>(val2);
			}

			throw new Exception("Invalid operation");
		}

		object ModOp(object val1, object val2)
		{
			if ((val1 == null) || (val2 == null))
				throw new Exception("NULL value");

			if ((IsIntType(val1)) && (IsIntType(val2)))
				return ToType<long>(val1) / ToType<long>(val2);

			throw new Exception("Invalid operation");
		}

		object AddOp(object val1, object val2)
		{
			if ((val1 == null) || (val2 == null))
			{
				var val = val1 ?? val2;
				if (IsCharacterType(val))
					return val;
				throw new Exception("NULL value");
			}

			if ((val1 is char) && (IsIntType(val2)))
				return (char)((int)ToType<char>(val1) + ToType<int>(val2));

			if ((IsCharacterType(val1)) || (IsCharacterType(val2)))
				return val1.ToString() + val2.ToString();

			if ((IsNumericType(val1)) && (IsNumericType(val2)))
			{
				if ((IsFloatType(val1)) || (IsFloatType(val2)))
					return ToType<double>(val1) + ToType<double>(val2);
				return ToType<long>(val1) + ToType<long>(val2);
			}

			throw new Exception("Invalid operation");
		}

		object SubOp(object val1, object val2)
		{
			if ((val1 == null) || (val2 == null))
				throw new Exception("NULL value");

			if ((val1 is char) && (IsIntType(val2)))
				return (char)((int)ToType<char>(val1) - ToType<int>(val2));

			if ((IsNumericType(val1)) && (IsNumericType(val2)))
			{
				if ((IsFloatType(val1)) || (IsFloatType(val2)))
					return ToType<double>(val1) - ToType<double>(val2);
				return ToType<long>(val1) - ToType<long>(val2);
			}

			throw new Exception("Invalid operation");
		}

		bool EqualsOp(object val1, object val2, bool ignoreCase)
		{
			if ((val1 == null) && (val2 == null))
				return true;
			if ((val1 == null) || (val2 == null))
				return false;

			if ((IsNumericType(val1)) && (IsNumericType(val2)))
			{
				if ((IsFloatType(val1)) || (IsFloatType(val2)))
					return ToType<double>(val1) == ToType<double>(val2);
				return ToType<long>(val1) == ToType<long>(val2);
			}

			if ((IsCharacterType(val1)) && (IsCharacterType(val2)))
				return val1.ToString().Equals(val2.ToString(), ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

			return val1.Equals(val2);
		}

		bool LessThanOp(object val1, object val2, bool ignoreCase)
		{
			if ((val1 == null) || (val2 == null))
				throw new Exception("NULL value");

			if ((IsCharacterType(val1)) && (IsCharacterType(val2)))
				return String.Compare(val1.ToString(), val2.ToString(), ignoreCase) < 0;

			if ((IsNumericType(val1)) && (IsNumericType(val2)))
			{
				if ((IsFloatType(val1)) || (IsFloatType(val2)))
					return ToType<double>(val1) < ToType<double>(val2);
				return ToType<long>(val1) < ToType<long>(val2);
			}

			throw new Exception("Invalid operation");
		}

		bool GreaterThanOp(object val1, object val2, bool ignoreCase)
		{
			if ((val1 == null) || (val2 == null))
				throw new Exception("NULL value");

			if ((IsCharacterType(val1)) && (IsCharacterType(val2)))
				return String.Compare(val1.ToString(), val2.ToString(), ignoreCase) > 0;

			if ((IsNumericType(val1)) && (IsNumericType(val2)))
			{
				if ((IsFloatType(val1)) || (IsFloatType(val2)))
					return ToType<double>(val1) > ToType<double>(val2);
				return ToType<long>(val1) > ToType<long>(val2);
			}

			throw new Exception("Invalid operation");
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
				case "<<": return ToType<long>(val1) << ToType<int>(val2);
				case ">>": return ToType<long>(val1) >> ToType<int>(val2);
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
				case "&": return ToType<long>(val1) & ToType<long>(val2);
				case "^^": return ToType<long>(val1) ^ ToType<long>(val2);
				case "|": return ToType<long>(val1) | ToType<long>(val2);
				case "&&": return ToType<bool>(val1) && ToType<bool>(val2);
				case "||": return ToType<bool>(val1) || ToType<bool>(val2);
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
			return Visit(context.form());
		}

		object GetShortForm(string op)
		{
			var values = new Queue<object>(this.values);
			object result = null;
			bool first = true;
			while (values.Any())
			{
				if (first)
				{
					result = values.Dequeue();
					first = false;
					continue;
				}

				var val2 = values.Dequeue();
				result = BinaryOp(op, result, val2);
			}
			return result;
		}

		public override object VisitMethod(ExpressionParser.MethodContext context)
		{
			var method = context.method.Text;
			var paramList = context.e().Select(c => Visit(c)).ToList();

			switch (method)
			{
				case "Type": return paramList.Single().GetType();
				case "ValidRE": return ValidRE(paramList.Single().ToString());
				case "Eval": return new NEExpression(paramList.Single().ToString()).Evaluate();
				case "FileName": return Path.GetFileName((paramList.Single() ?? "").ToString());
				case "StrFormat": return String.Format(paramList.Select(arg => arg == null ? "" : arg.ToString()).FirstOrDefault() ?? "", paramList.Skip(1).Select(arg => arg ?? "").ToArray());
				case "Min": return paramList.Min();
				case "Max": return paramList.Max();
				default: throw new ArgumentException(String.Format("Invalid method: {0}", method));
			}
		}

		public override object VisitConstant(ExpressionParser.ConstantContext context)
		{
			var constant = context.constant.Text;
			switch (constant)
			{
				case "pi": return Math.PI;
				case "e": return Math.E;
				default: throw new ArgumentException(String.Format("Invalid constant: {0}", constant));
			}
		}

		public override object VisitShortForm(ExpressionParser.ShortFormContext context) { return GetShortForm(context.op.Text); }
		public override object VisitDefaultOpForm(ExpressionParser.DefaultOpFormContext context) { return GetShortForm("&&"); }
		public override object VisitDot(ExpressionParser.DotContext context) { return BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2)); }
		public override object VisitUnary(ExpressionParser.UnaryContext context) { return UnaryOp(context.op.Text, Visit(context.val)); }
		public override object VisitCast(ExpressionParser.CastContext context) { return CastValue(Visit(context.val), GetType(context.type.Text)); }
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
		public override object VisitTernary(ExpressionParser.TernaryContext context) { return ToType<bool>(Visit(context.condition)) ? Visit(context.trueval) : Visit(context.falseval); }
		public override object VisitExpression(ExpressionParser.ExpressionContext context) { return Visit(context.val); }
		public override object VisitParam(ExpressionParser.ParamContext context) { return values[int.Parse(context.val.Text.Trim('[', ']'))]; }
		public override object VisitString(ExpressionParser.StringContext context) { return context.val.Text.Substring(1, context.val.Text.Length - 2); }
		public override object VisitChar(ExpressionParser.CharContext context) { return context.val.Text[1]; }
		public override object VisitTrue(ExpressionParser.TrueContext context) { return true; }
		public override object VisitFalse(ExpressionParser.FalseContext context) { return false; }
		public override object VisitNull(ExpressionParser.NullContext context) { return null; }
		public override object VisitFloat(ExpressionParser.FloatContext context) { return context.val.Text.Contains(".") ? (object)double.Parse(context.val.Text) : (object)long.Parse(context.val.Text); }
		public override object VisitHex(ExpressionParser.HexContext context) { return long.Parse(context.val.Text.Substring(2), NumberStyles.HexNumber); }
		public override object VisitVariable(ExpressionParser.VariableContext context) { return dict.ContainsKey(context.val.Text) ? dict[context.val.Text] : null; }
	}
}
