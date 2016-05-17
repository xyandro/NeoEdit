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
	class ExpressionEvaluator : ExpressionParserBaseVisitor<object>
	{
		readonly string expression;
		readonly NEVariables variables;
		readonly int row;
		readonly List<object> values;
		internal ExpressionEvaluator(string expression, NEVariables variables, int row, params object[] values)
		{
			this.expression = expression;
			this.variables = variables;
			this.row = row;
			this.values = values?.Select(val => RemoveUnset(val)).ToList();
		}

		object RemoveUnset(object val) => (val == null) || (val.GetType().FullName != "MS.Internal.NamedObject") || (val.ToString() != "{DependencyProperty.UnsetValue}") ? val : null;

		NumericValue GetNumeric(object val)
		{
			if (val is NumericValue)
				return val as NumericValue;
			return new NumericValue(val);
		}

		string GetString(object val)
		{
			if (val == null)
				return "";
			if (val is string)
				return (string)val;
			return val?.ToString();
		}

		bool GetBoolean(object val)
		{
			if (val == null)
				return false;
			if (val is bool)
				return (bool)val;
			if (val is NumericValue)
				return (val as NumericValue).IntValue != 0;
			if (val is string)
				return bool.Parse((string)val);
			throw new Exception("Invalid boolean format");
		}

		List<object> GetList(object val)
		{
			if (val == null)
				return new List<object>();
			if (val is List<object>)
				return val as List<object>;
			return new List<object> { val };
		}

		object UnaryOp(string op, object val)
		{
			switch (op)
			{
				case "~": return ~GetNumeric(val);
				case "+": return +GetNumeric(val);
				case "-": return -GetNumeric(val);
				case "!": return !GetBoolean(val);
				default: throw new ArgumentException($"Invalid operation: {op}");
			}
		}

		object UnaryOpEnd(string op, object val)
		{
			switch (op)
			{
				case "!": return GetNumeric(val).Factorial();
				default: throw new ArgumentException($"Invalid operation: {op}");
			}
		}

		public static object DotOp(object obj, object fileName)
		{
			string fieldNameStr = (fileName ?? "").ToString();
			var field = obj?.GetType().GetProperty(fieldNameStr, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (field == null)
				return null;
			return field.GetValue(obj);
		}

		public static string Repeat(string val, int count)
		{
			var result = new StringBuilder(val.Length * count);
			for (var ctr = 0; ctr < count; ++ctr)
				result.Append(val);
			return result.ToString();
		}

		public static bool Is(object obj, string type) => obj?.GetType().Name.Equals(type) ?? false;

		static string IncrementChars(string str, int amount) => new string(str.Select(ch => (char)(ch + amount)).ToArray());

		object BinaryOp(string op, object val1, object val2)
		{
			switch (op)
			{
				case ".": return DotOp(val1, val2);
				case "^": return GetNumeric(val1).Exp(GetNumeric(val2));
				case "*": return GetNumeric(val1) * GetNumeric(val2);
				case "/": return GetNumeric(val1) / GetNumeric(val2);
				case "t*": return Repeat(GetString(val1), GetNumeric(val2).IntValue);
				case "//": return GetNumeric(val1).IntDiv(GetNumeric(val2));
				case "%": return GetNumeric(val1) % GetNumeric(val2);
				case "+": return GetNumeric(val1) + GetNumeric(val2);
				case "-": return GetNumeric(val1) - GetNumeric(val2);
				case "t+": return GetString(val1) + GetString(val2);
				case "t++": return IncrementChars(GetString(val1), GetNumeric(val2).IntValue);
				case "t--": return IncrementChars(GetString(val1), -GetNumeric(val2).IntValue);
				case "<<": return GetNumeric(val1).ShiftLeft(GetNumeric(val2));
				case ">>": return GetNumeric(val1).ShiftRight(GetNumeric(val2));
				case "<": return GetNumeric(val1) < GetNumeric(val2);
				case ">": return GetNumeric(val1) > GetNumeric(val2);
				case "<=": return GetNumeric(val1) <= GetNumeric(val2);
				case ">=": return GetNumeric(val1) >= GetNumeric(val2);
				case "is": return Is(val1, GetString(val2));
				case "o=":
				case "o==": return val1 == val2;
				case "o!=":
				case "o<>": return val1 != val2;
				case "=":
				case "==": return GetNumeric(val1) == GetNumeric(val2);
				case "!=":
				case "<>": return GetNumeric(val1) != GetNumeric(val2);
				case "t<": return string.Compare(GetString(val1), GetString(val2)) < 0;
				case "t>": return string.Compare(GetString(val1), GetString(val2)) > 0;
				case "t<=": return string.Compare(GetString(val1), GetString(val2)) <= 0;
				case "t>=": return string.Compare(GetString(val1), GetString(val2)) >= 0;
				case "ti<": return string.Compare(GetString(val1), GetString(val2), true) < 0;
				case "ti>": return string.Compare(GetString(val1), GetString(val2), true) > 0;
				case "ti<=": return string.Compare(GetString(val1), GetString(val2), true) <= 0;
				case "ti>=": return string.Compare(GetString(val1), GetString(val2), true) >= 0;
				case "t=":
				case "t==": return string.Compare(GetString(val1), GetString(val2)) == 0;
				case "t!=":
				case "t<>": return string.Compare(GetString(val1), GetString(val2)) != 0;
				case "ti=":
				case "ti==": return string.Compare(GetString(val1), GetString(val2), true) == 0;
				case "ti!=":
				case "ti<>": return string.Compare(GetString(val1), GetString(val2), true) != 0;
				case "&": return GetNumeric(val1) & GetNumeric(val2);
				case "^^": return GetNumeric(val1) ^ GetNumeric(val2);
				case "|": return GetNumeric(val1) | GetNumeric(val2);
				case "&&": return GetBoolean(val1) && GetBoolean(val2);
				case "||": return GetBoolean(val1) || GetBoolean(val2);
				case "??": return val1 ?? val2;
				case "=>": return GetNumeric(val1).ConvertUnits(GetNumeric(val2).Units);
				default: throw new ArgumentException($"Invalid operation: {op}");
			}
		}

		public override object VisitExpr(ExpressionParser.ExprContext context)
		{
			if ((context.DEBUG() != null) && (Debugger.IsAttached))
				Debugger.Break();
			return Simplify(Visit(context.form()));
		}

		static object Simplify(object value)
		{
			if (value is List<object>)
			{
				var list = value as List<object>;
				if (list.Count == 1)
					value = list[0];
				else
					value = string.Join(",", list);
			}
			if (value is NumericValue)
				value = (value as NumericValue).GetResult();
			if (value is BigInteger)
			{
				var bi = (BigInteger)value;
				if ((bi >= sbyte.MinValue) && (bi <= sbyte.MaxValue))
					value = (sbyte)bi;
				else if ((bi >= byte.MinValue) && (bi <= byte.MaxValue))
					value = (byte)bi;
				else if ((bi >= short.MinValue) && (bi <= short.MaxValue))
					value = (short)bi;
				else if ((bi >= ushort.MinValue) && (bi <= ushort.MaxValue))
					value = (ushort)bi;
				else if ((bi >= int.MinValue) && (bi <= int.MaxValue))
					value = (int)bi;
				else if ((bi >= uint.MinValue) && (bi <= uint.MaxValue))
					value = (uint)bi;
				else if ((bi >= long.MinValue) && (bi <= long.MaxValue))
					value = (long)bi;
				else if ((bi >= ulong.MinValue) && (bi <= ulong.MaxValue))
					value = (ulong)bi;
			}
			return value;
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

		public bool ValidRE(string re)
		{
			try
			{
				new Regex(re);
				return true;
			}
			catch { return false; }
		}

		string GetFileName(string str) => string.IsNullOrEmpty(str) ? null : Path.GetFileName(str);

		NumericValue FromDate(string str) => new NumericValue(DateTimeOffset.Parse(str).ToOffset(TimeSpan.Zero).Ticks, "ticks");

		string ToDate(NumericValue str, NumericValue timeZone = null)
		{
			var ticks = str.ConvertUnits(new ExpressionUnits("ticks")).LongValue;
			var tz = ReferenceEquals(timeZone, null) ? DateTimeOffset.Now.Offset : TimeSpan.FromTicks(timeZone.ConvertUnits(new ExpressionUnits("ticks")).LongValue);
			var date = new DateTimeOffset(ticks, TimeSpan.Zero).ToOffset(tz);
			return date.ToString("o");
		}

		public override object VisitMethod(ExpressionParser.MethodContext context)
		{
			var method = context.method.Text;
			var paramList = context.e().Select(c => Visit(c)).ToList();

			switch (method.ToLowerInvariant())
			{
				case "abs": return GetNumeric(paramList[0]).Abs();
				case "acos": return GetNumeric(paramList[0]).Acos();
				case "asin": return GetNumeric(paramList[0]).Asin();
				case "atan": return GetNumeric(paramList[0]).Atan();
				case "cos": return GetNumeric(paramList[0]).Cos();
				case "count": return GetList(paramList[0]).Count;
				case "date": return new NumericValue(new DateTimeOffset(DateTime.Now.Date, DateTimeOffset.Now.Offset).UtcTicks, "ticks");
				case "eval": return new NEExpression(GetString(paramList[0])).InternalEvaluate(variables, row);
				case "factor": return GetNumeric(paramList[0]).Factor();
				case "filename": return GetFileName(GetString(paramList[0]));
				case "fromdate": return FromDate(GetString(paramList[0]));
				case "fromwords": return NumericValue.FromWords(GetString(paramList[0]));
				case "gcf": return NumericValue.GCF(paramList.Select(val => GetNumeric(val)).ToList());
				case "lcm": return NumericValue.LCM(paramList.Select(val => GetNumeric(val)).ToList());
				case "len": return paramList.SelectMany(param => GetList(param)).Select(param => GetString(param).Length).Cast<object>().ToList();
				case "ln": return GetNumeric(paramList[0]).Ln();
				case "log": return paramList.Count == 1 ? GetNumeric(paramList[0]).Log() : GetNumeric(paramList[0]).Log(GetNumeric(paramList[2]));
				case "now": return new NumericValue(DateTimeOffset.Now.UtcTicks, "ticks");
				case "max": return paramList.SelectMany(param => GetList(param)).Select(param => GetNumeric(param)).Max();
				case "min": return paramList.SelectMany(param => GetList(param)).Select(param => GetNumeric(param)).Min();
				case "reciprocal": return GetNumeric(paramList[0]).Reciprocal();
				case "reduce": return NumericValue.Reduce(GetNumeric(paramList[0]), GetNumeric(paramList[1]));
				case "root": return GetNumeric(paramList[0]).Root(GetNumeric(paramList[1]));
				case "sin": return GetNumeric(paramList[0]).Sin();
				case "sqrt": return GetNumeric(paramList[0]).Root(GetNumeric(2));
				case "strformat": return string.Format(GetString(paramList[0]), paramList.Skip(1).Select(param => Simplify(param)).ToArray());
				case "strmax": return paramList.SelectMany(param => GetList(param)).Select(param => GetString(param)).Max();
				case "strmin": return paramList.SelectMany(param => GetList(param)).Select(param => GetString(param)).Min();
				case "sum": return NumericValue.Sum(paramList.SelectMany(param => GetList(param)).Select(param => GetNumeric(param)));
				case "tan": return GetNumeric(paramList[0]).Tan();
				case "todate": return ToDate(GetNumeric(paramList[0]), paramList.Count > 1 ? GetNumeric(paramList[1]) : null);
				case "toutcdate": return ToDate(GetNumeric(paramList[0]), new NumericValue(0, "ticks"));
				case "towords": return GetNumeric(paramList[0]).ToWords();
				case "type": return paramList[0].GetType();
				case "utcdate": return new NumericValue(DateTimeOffset.Now.UtcDateTime.Date.Ticks, "ticks");
				case "validre": return ValidRE(GetString(paramList[0]));
				default: throw new ArgumentException($"Invalid method: {method}");
			}
		}

		public override object VisitConstant(ExpressionParser.ConstantContext context)
		{
			var constant = context.constant.Text;
			switch (constant.ToLowerInvariant())
			{
				case "e": return Math.E;
				case "pi": return Math.PI;
				default: throw new ArgumentException($"Invalid constant: {constant}");
			}
		}

		static readonly Dictionary<string, string> escapeChars = new Dictionary<string, string>
		{
			[@"\\"] = "\\",
			[@"\'"] = "\'",
			[@"\"""] = "\"",
			[@"\0"] = "\0",
			[@"\a"] = "\a",
			[@"\b"] = "\b",
			[@"\f"] = "\f",
			[@"\n"] = "\n",
			[@"\r"] = "\r",
			[@"\t"] = "\t",
			[@"\v"] = "\v",
		};

		public override object VisitShortForm(ExpressionParser.ShortFormContext context) => GetShortForm(context.op.Text);
		public override object VisitDefaultOpForm(ExpressionParser.DefaultOpFormContext context) => GetShortForm("&&");
		public override object VisitParens(ExpressionParser.ParensContext context) => Visit(context.val);
		public override object VisitAddUnits(ExpressionParser.AddUnitsContext context) => GetNumeric(Visit(context.val1)).SetUnits(GetNumeric(Visit(context.unitsVal)));
		public override object VisitDot(ExpressionParser.DotContext context) => BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2));
		public override object VisitUnary(ExpressionParser.UnaryContext context) => UnaryOp(context.op.Text, Visit(context.val));
		public override object VisitUnaryEnd(ExpressionParser.UnaryEndContext context) => UnaryOpEnd(context.op.Text, Visit(context.val));
		public override object VisitExp(ExpressionParser.ExpContext context) => BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2));
		public override object VisitMult(ExpressionParser.MultContext context) => BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2));
		public override object VisitAdd(ExpressionParser.AddContext context) => BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2));
		public override object VisitShift(ExpressionParser.ShiftContext context) => BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2));
		public override object VisitRelational(ExpressionParser.RelationalContext context) => BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2));
		public override object VisitEquality(ExpressionParser.EqualityContext context) => BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2));
		public override object VisitBitwiseAnd(ExpressionParser.BitwiseAndContext context) => BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2));
		public override object VisitBitwiseXor(ExpressionParser.BitwiseXorContext context) => BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2));
		public override object VisitBitwiseOr(ExpressionParser.BitwiseOrContext context) => BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2));
		public override object VisitLogicalAnd(ExpressionParser.LogicalAndContext context) => BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2));
		public override object VisitLogicalOr(ExpressionParser.LogicalOrContext context) => BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2));
		public override object VisitNullCoalesce(ExpressionParser.NullCoalesceContext context) => BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2));
		public override object VisitUnitConversion(ExpressionParser.UnitConversionContext context) => BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2));
		public override object VisitTernary(ExpressionParser.TernaryContext context) => GetBoolean(Visit(context.condition)) ? Visit(context.trueval) : Visit(context.falseval);
		public override object VisitParam(ExpressionParser.ParamContext context) => values[int.Parse(context.val.Text.Trim('[', ']'))];
		public override object VisitVarParam(ExpressionParser.VarParamContext context) => variables.GetValues(context.val.Text.Trim('[', ']'));
		public override object VisitNormalstring(ExpressionParser.NormalstringContext context) => Visit(context.val);
		public override object VisitStrcontent(ExpressionParser.StrcontentContext context) => context.children?.Select(child => GetString(Visit(child))).ToJoinedString() ?? "";
		public override object VisitStrchars(ExpressionParser.StrcharsContext context) => context.val.Text;
		public override object VisitStrescape(ExpressionParser.StrescapeContext context) => escapeChars[context.val.Text];
		public override object VisitStrunicode(ExpressionParser.StrunicodeContext context) => char.ConvertFromUtf32(Convert.ToInt32(context.val.Text.Substring(2), 16));
		public override object VisitVerbatimstring(ExpressionParser.VerbatimstringContext context) => Visit(context.val);
		public override object VisitVstrcontent(ExpressionParser.VstrcontentContext context) => context.children?.Select(child => GetString(Visit(child))).ToJoinedString() ?? "";
		public override object VisitVstrchars(ExpressionParser.VstrcharsContext context) => context.val.Text;
		public override object VisitVstrquote(ExpressionParser.VstrquoteContext context) => "\"";
		public override object VisitInterpolatedstring(ExpressionParser.InterpolatedstringContext context) => Visit(context.val);
		public override object VisitIstrcontent(ExpressionParser.IstrcontentContext context) => context.children?.Select(child => GetString(Visit(child))).ToJoinedString() ?? "";
		public override object VisitIstrchars(ExpressionParser.IstrcharsContext context) => context.val.Text;
		public override object VisitIstrliteral(ExpressionParser.IstrliteralContext context) => context.val.Text[0].ToString();
		public override object VisitIstrinter(ExpressionParser.IstrinterContext context) => Visit(context.val);
		public override object VisitVerbatiminterpolatedstring(ExpressionParser.VerbatiminterpolatedstringContext context) => Visit(context.val);
		public override object VisitVistrcontent(ExpressionParser.VistrcontentContext context) => context.children?.Select(child => GetString(Visit(child))).ToJoinedString() ?? "";
		public override object VisitVistrchars(ExpressionParser.VistrcharsContext context) => context.val.Text;
		public override object VisitVistrliteral(ExpressionParser.VistrliteralContext context) => context.val.Text[0].ToString();
		public override object VisitVistrinter(ExpressionParser.VistrinterContext context) => Visit(context.val);
		public override object VisitTrue(ExpressionParser.TrueContext context) => true;
		public override object VisitFalse(ExpressionParser.FalseContext context) => false;
		public override object VisitNull(ExpressionParser.NullContext context) => null;
		public override object VisitInteger(ExpressionParser.IntegerContext context) => GetNumeric(BigInteger.Parse(context.val.Text.Replace(",", "")));
		public override object VisitFloat(ExpressionParser.FloatContext context) => GetNumeric(double.Parse(context.val.Text.Replace(",", "")));
		public override object VisitHex(ExpressionParser.HexContext context) => GetNumeric(BigInteger.Parse("0" + context.val.Text.Substring(2), NumberStyles.HexNumber));
		public override object VisitVariable(ExpressionParser.VariableContext context) => variables.GetValue(context.val.Text, row);
		public override object VisitUnitExp(ExpressionParser.UnitExpContext context) => GetNumeric(Visit(context.base1)).Exp(GetNumeric(int.Parse(context.power.Text)));
		public override object VisitUnitMult(ExpressionParser.UnitMultContext context) => BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2));
		public override object VisitUnitParen(ExpressionParser.UnitParenContext context) => Visit(context.units());
		public override object VisitUnit(ExpressionParser.UnitContext context) => new NumericValue(1, context.val.Text);
	}
}
