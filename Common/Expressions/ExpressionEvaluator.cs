using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NeoEdit.Common.Expressions.Parser;

namespace NeoEdit.Common.Expressions
{
	class ExpressionEvaluator : ExpressionBaseVisitor<object>
	{
		readonly string expression;
		readonly Dictionary<string, object> dict;
		readonly object[] values;
		public ExpressionEvaluator(string expression, Dictionary<string, object> dict, params object[] values)
		{
			this.expression = expression;
			this.dict = dict;
			this.values = values;
		}

		bool IsIntType(Type type)
		{
			return (type == typeof(sbyte)) || (type == typeof(byte)) || (type == typeof(char)) || (type == typeof(short)) || (type == typeof(ushort)) || (type == typeof(int)) || (type == typeof(uint)) || (type == typeof(long)) || (type == typeof(ulong));
		}

		bool IsFloatType(Type type)
		{
			return (type == typeof(float)) || (type == typeof(double)) || (type == typeof(decimal));
		}

		Type GetType(string type)
		{
			switch (type)
			{
				case "bool": return typeof(bool);
				case "long": return typeof(long);
				case "double": return typeof(double);
				case "string": return typeof(string);
				default: throw new ArgumentException(String.Format("Invalid cast: {0}", type));
			}
		}

		object ToType(object val, Type type)
		{
			if (val == null)
				return type.IsValueType ? Activator.CreateInstance(type) : null;
			return Convert.ChangeType(val, type);
		}

		T ToType<T>(object val)
		{
			if (val == null)
				return typeof(T) == typeof(string) ? (T)(object)"" : default(T);
			return (T)Convert.ChangeType(val, typeof(T));
		}

		object OpByType(object val1, object val2, Func<bool, bool, object> boolFunc = null, Func<long, long, object> longFunc = null, Func<double, double, object> doubleFunc = null, Func<string, string, object> stringFunc = null)
		{
			if ((val1 == null) && (val2 == null))
				return null;

			var type1 = val1 == null ? null : val1.GetType();
			var type2 = val2 == null ? null : val2.GetType();

			if ((stringFunc != null) && ((type1 == typeof(string)) || (type2 == typeof(string))))
				return stringFunc(ToType<string>(val1), ToType<string>(val2));
			else if ((boolFunc != null) && ((type1 == typeof(bool)) || (type2 == typeof(bool))))
				return boolFunc(ToType<bool>(val1), ToType<bool>(val2));
			else if ((doubleFunc != null) && ((IsFloatType(type1)) || (IsFloatType(type2))))
				return doubleFunc(ToType<double>(val1), ToType<double>(val2));
			else if ((longFunc != null) && ((IsIntType(type1)) || (IsIntType(type2))))
				return longFunc(ToType<long>(val1), ToType<long>(val2));

			throw new ArgumentException("Invalid operation");
		}

		object OpByType(object val, Func<bool, object> boolFunc = null, Func<long, object> longFunc = null, Func<double, object> doubleFunc = null, Func<string, object> stringFunc = null)
		{
			if (val == null)
				return null;

			var type = val.GetType();

			if ((stringFunc != null) && (type == typeof(string)))
				return stringFunc(ToType<string>(val));
			else if ((boolFunc != null) && (type == typeof(bool)))
				return boolFunc(ToType<bool>(val));
			else if ((doubleFunc != null) && (IsFloatType(type)))
				return doubleFunc(ToType<double>(val));
			else if ((longFunc != null) && (IsIntType(type)))
				return longFunc(ToType<long>(val));

			throw new ArgumentException("Invalid operation");
		}

		object GetDotOp(object obj, string fieldName)
		{
			if (obj == null)
				return null;
			var field = obj.GetType().GetProperty(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (field == null)
				return null;
			return field.GetValue(obj);
		}

		object DoUnaryOp(string op, object val)
		{
			switch (op)
			{
				case "+": return OpByType(val, longFunc: a => +a, doubleFunc: a => +a);
				case "-": return OpByType(val, longFunc: a => -a, doubleFunc: a => -a);
				case "!": return !ToType<bool>(val);
				default: throw new ArgumentException(String.Format("Invalid operation: {0}", op));
			}
		}

		object DoBinaryOp(string op, object val1, object val2)
		{
			switch (op)
			{
				case ".": return GetDotOp(val1, (val2 ?? "").ToString());
				case "*": return OpByType(val1, val2, longFunc: (a, b) => a * b, doubleFunc: (a, b) => a * b);
				case "/": return OpByType(val1, val2, longFunc: (a, b) => a / b, doubleFunc: (a, b) => a / b);
				case "%": return OpByType(val1, val2, longFunc: (a, b) => a % b, doubleFunc: (a, b) => a % b);
				case "+": return OpByType(val1, val2, longFunc: (a, b) => a + b, doubleFunc: (a, b) => a + b, stringFunc: (a, b) => (a ?? "") + (b ?? ""));
				case "-": return OpByType(val1, val2, longFunc: (a, b) => a - b, doubleFunc: (a, b) => a - b);
				case "<<": return ToType<long>(val1) << ToType<int>(val2);
				case ">>": return ToType<long>(val1) >> ToType<int>(val2);
				case "<": return OpByType(val1, val2, longFunc: (a, b) => a < b, doubleFunc: (a, b) => a < b, stringFunc: (a, b) => String.Compare(a, b) < 0);
				case "i<": return OpByType(val1, val2, longFunc: (a, b) => a < b, doubleFunc: (a, b) => a < b, stringFunc: (a, b) => String.Compare(a, b, true) < 0);
				case ">": return OpByType(val1, val2, longFunc: (a, b) => a > b, doubleFunc: (a, b) => a > b, stringFunc: (a, b) => String.Compare(a, b) > 0);
				case "i>": return OpByType(val1, val2, longFunc: (a, b) => a > b, doubleFunc: (a, b) => a > b, stringFunc: (a, b) => String.Compare(a, b, true) > 0);
				case "<=": return OpByType(val1, val2, longFunc: (a, b) => a <= b, doubleFunc: (a, b) => a <= b, stringFunc: (a, b) => String.Compare(a, b) <= 0);
				case "i<=": return OpByType(val1, val2, longFunc: (a, b) => a <= b, doubleFunc: (a, b) => a <= b, stringFunc: (a, b) => String.Compare(a, b, true) <= 0);
				case ">=": return OpByType(val1, val2, longFunc: (a, b) => a >= b, doubleFunc: (a, b) => a >= b, stringFunc: (a, b) => String.Compare(a, b) >= 0);
				case "i>=": return OpByType(val1, val2, longFunc: (a, b) => a >= b, doubleFunc: (a, b) => a >= b, stringFunc: (a, b) => String.Compare(a, b, true) >= 0);
				case "is": return val1 == null ? false : val1.GetType().Name == (val2 ?? "").ToString();
				case "==": return OpByType(val1, val2, boolFunc: (a, b) => a == b, longFunc: (a, b) => a == b, doubleFunc: (a, b) => a == b, stringFunc: (a, b) => String.Compare(a, b) == 0);
				case "i==": return OpByType(val1, val2, boolFunc: (a, b) => a == b, longFunc: (a, b) => a == b, doubleFunc: (a, b) => a == b, stringFunc: (a, b) => String.Compare(a, b, true) == 0);
				case "!=": return OpByType(val1, val2, boolFunc: (a, b) => a != b, longFunc: (a, b) => a != b, doubleFunc: (a, b) => a != b, stringFunc: (a, b) => String.Compare(a, b) != 0);
				case "i!=": return OpByType(val1, val2, boolFunc: (a, b) => a != b, longFunc: (a, b) => a != b, doubleFunc: (a, b) => a != b, stringFunc: (a, b) => String.Compare(a, b, true) != 0);
				case "&": return ToType<long>(val1) & ToType<long>(val2);
				case "^": return ToType<long>(val1) ^ ToType<long>(val2);
				case "|": return ToType<long>(val1) | ToType<long>(val2);
				case "&&": return ToType<bool>(val1) && ToType<bool>(val2);
				case "||": return ToType<bool>(val1) || ToType<bool>(val2);
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

		public override object VisitShortFormExpression(ExpressionParser.ShortFormExpressionContext context)
		{
			var op = context.op.Text;
			var values = new Queue<object>(this.values);
			object val1 = null;
			bool first = true;
			while (values.Any())
			{
				if (first)
				{
					val1 = values.Dequeue();
					first = false;
					continue;
				}

				var val2 = values.Dequeue();
				val1 = DoBinaryOp(op, val1, val2);
			}
			return val1;
		}

		public override object VisitMethod(ExpressionParser.MethodContext context)
		{
			var method = context.method.Text;
			var paramList = context.e().Select(c => Visit(c)).ToList();

			switch (method)
			{
				case "Type": return paramList.Single().GetType();
				case "ValidRE": return ValidRE(ToType<string>(paramList.Single()));
				case "Eval": return new NEExpression(ToType<string>(paramList.Single())).Evaluate();
				case "FileName": return Path.GetFileName(ToType<string>(paramList.Single()));
				case "StrFormat": return String.Format(paramList.Select(arg => arg == null ? "" : arg.ToString()).FirstOrDefault() ?? "", paramList.Skip(1).Select(arg => arg ?? "").ToArray());
				default: throw new ArgumentException(String.Format("Invalid method: {0}", method));
			}
		}

		public override object VisitDot(ExpressionParser.DotContext context) { return DoBinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2)); }
		public override object VisitUnary(ExpressionParser.UnaryContext context) { return DoUnaryOp(context.op.Text, Visit(context.val)); }
		public override object VisitCast(ExpressionParser.CastContext context) { return ToType(Visit(context.val), GetType(context.type.Text)); }
		public override object VisitMult(ExpressionParser.MultContext context) { return DoBinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2)); }
		public override object VisitAdd(ExpressionParser.AddContext context) { return DoBinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2)); }
		public override object VisitShift(ExpressionParser.ShiftContext context) { return DoBinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2)); }
		public override object VisitRelational(ExpressionParser.RelationalContext context) { return DoBinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2)); }
		public override object VisitEquality(ExpressionParser.EqualityContext context) { return DoBinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2)); }
		public override object VisitLogicalAnd(ExpressionParser.LogicalAndContext context) { return DoBinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2)); }
		public override object VisitLogicalXor(ExpressionParser.LogicalXorContext context) { return DoBinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2)); }
		public override object VisitLogicalOr(ExpressionParser.LogicalOrContext context) { return DoBinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2)); }
		public override object VisitConditionalAnd(ExpressionParser.ConditionalAndContext context) { return DoBinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2)); }
		public override object VisitConditionalOr(ExpressionParser.ConditionalOrContext context) { return DoBinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2)); }
		public override object VisitTernary(ExpressionParser.TernaryContext context) { return ToType<bool>(Visit(context.condition)) ? Visit(context.trueval) : Visit(context.falseval); }
		public override object VisitExpression(ExpressionParser.ExpressionContext context) { return Visit(context.val); }
		public override object VisitParam(ExpressionParser.ParamContext context) { return values[int.Parse(context.val.Text.Trim('[', ']'))]; }
		public override object VisitString(ExpressionParser.StringContext context) { return context.val.Text.Substring(1, context.val.Text.Length - 2); }
		public override object VisitTrue(ExpressionParser.TrueContext context) { return true; }
		public override object VisitFalse(ExpressionParser.FalseContext context) { return false; }
		public override object VisitNull(ExpressionParser.NullContext context) { return null; }
		public override object VisitFloat(ExpressionParser.FloatContext context) { return context.val.Text.Contains(".") ? (object)double.Parse(context.val.Text) : (object)long.Parse(context.val.Text); }
		public override object VisitHex(ExpressionParser.HexContext context) { return long.Parse(context.val.Text.Substring(2), NumberStyles.HexNumber); }
		public override object VisitVariable(ExpressionParser.VariableContext context) { return dict.ContainsKey(context.val.Text) ? dict[context.val.Text] : null; }
	}
}
