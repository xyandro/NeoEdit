using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using NeoEdit.Common.Expressions.Parser;

namespace NeoEdit.Common.Expressions
{
	class ExpressionEvaluator : ExpressionBaseVisitor<ExpressionResult>
	{
		readonly string expression;
		readonly Dictionary<string, ExpressionResult> dict;
		readonly List<ExpressionResult> values;
		internal ExpressionEvaluator(string expression, Dictionary<string, object> dict, params object[] values)
		{
			this.expression = expression;
			this.dict = dict == null ? null : dict.ToDictionary(pair => pair.Key, pair => new ExpressionResult(pair.Value));
			this.values = values == null ? null : values.Select(value => new ExpressionResult(value)).ToList();
		}

		ExpressionResult UnaryOp(string op, ExpressionResult val)
		{
			switch (op)
			{
				case "~": return ~val;
				case "+": return +val;
				case "-": return -val;
				case "!": return !val;
				default: throw new ArgumentException(String.Format("Invalid operation: {0}", op));
			}
		}

		ExpressionResult UnaryOpEnd(string op, ExpressionResult val)
		{
			switch (op)
			{
				case "!": return ExpressionResult.Factorial(val);
				default: throw new ArgumentException(String.Format("Invalid operation: {0}", op));
			}
		}

		ExpressionResult BinaryOp(string op, ExpressionResult val1, ExpressionResult val2)
		{
			switch (op)
			{
				case ".": return ExpressionResult.DotOp(val1, val2);
				case "^": return ExpressionResult.Exp(val1, val2);
				case "*": return val1 * val2;
				case "/": return val1 / val2;
				case "%": return val1 % val2;
				case "+": return val1 + val2;
				case "-": return val1 - val2;
				case "<<": return ExpressionResult.ShiftLeft(val1, val2);
				case ">>": return ExpressionResult.ShiftRight(val1, val2);
				case "<": return ExpressionResult.LessThanOp(val1, val2, false);
				case "i<": return ExpressionResult.LessThanOp(val1, val2, true);
				case ">": return ExpressionResult.GreaterThanOp(val1, val2, false);
				case "i>": return ExpressionResult.GreaterThanOp(val1, val2, true);
				case "<=": return !ExpressionResult.GreaterThanOp(val1, val2, false);
				case "i<=": return !ExpressionResult.GreaterThanOp(val1, val2, true);
				case ">=": return !ExpressionResult.LessThanOp(val1, val2, false);
				case "i>=": return !ExpressionResult.LessThanOp(val1, val2, true);
				case "is": return ExpressionResult.Is(val1, val2);
				case "==": return ExpressionResult.EqualsOp(val1, val2, false);
				case "i==": return ExpressionResult.EqualsOp(val1, val2, true);
				case "!=": return !ExpressionResult.EqualsOp(val1, val2, false);
				case "i!=": return !ExpressionResult.EqualsOp(val1, val2, true);
				case "&": return val1 & val2;
				case "^^": return val1 ^ val2;
				case "|": return val1 | val2;
				case "&&": return ExpressionResult.AndOp(val1, val2);
				case "||": return ExpressionResult.OrOp(val1, val2);
				case "??": return ExpressionResult.NullCoalesceOp(val1, val2);
				default: throw new ArgumentException(String.Format("Invalid operation: {0}", op));
			}
		}

		public override ExpressionResult VisitExpr(ExpressionParser.ExprContext context)
		{
			if ((context.DEBUG() != null) && (Debugger.IsAttached))
				Debugger.Break();
			return Visit(context.form());
		}

		ExpressionResult GetShortForm(string op)
		{
			var values = new Queue<ExpressionResult>(this.values);
			ExpressionResult result = null;
			bool first = true;
			while (values.Any())
			{
				var val2 = values.Dequeue();
				result = first ? val2 : BinaryOp(op, result, val2);
				first = false;
			}
			return result;
		}

		public override ExpressionResult VisitMethod(ExpressionParser.MethodContext context)
		{
			var method = context.method.Text;
			var paramList = context.e().Select(c => Visit(c)).ToList();

			switch (method.ToLowerInvariant())
			{
				case "abs": return paramList[0].Abs();
				case "acos": return paramList[0].Acos();
				case "asin": return paramList[0].Asin();
				case "atan": return paramList[0].Atan();
				case "conjugate": return paramList[0].Conjugate();
				case "cos": return paramList[0].Cos();
				case "cosh": return paramList[0].Cosh();
				case "eval": return new NEExpression(paramList[0].GetString).InternalEvaluate(null);
				case "filename": return paramList[0].GetFileName();
				case "frompolar": return ExpressionResult.FromPolar(paramList[0], paramList[1]);
				case "fromwords": return paramList[0].FromWords();
				case "imaginary": return paramList[0].GetImaginary();
				case "ln": return paramList[0].Ln();
				case "log": return paramList.Count == 1 ? paramList[0].Log() : ExpressionResult.Log(paramList[0], paramList[2]);
				case "magnitude": return paramList[0].Magnitude();
				case "max": return paramList.Max();
				case "min": return paramList.Min();
				case "phase": return paramList[0].Phase();
				case "real": return paramList[0].Real();
				case "reciprocal": return paramList[0].Reciprocal();
				case "root": return ExpressionResult.Root(paramList[0], paramList[1]);
				case "sin": return paramList[0].Sin();
				case "sinh": return paramList[0].Sinh();
				case "sqrt": return ExpressionResult.Root(paramList[0], new ExpressionResult(2));
				case "strformat": return ExpressionResult.StrFormat(paramList[0], paramList.Skip(1).ToArray());
				case "tan": return paramList[0].Tan();
				case "tanh": return paramList[0].Tanh();
				case "towords": return paramList[0].ToWords();
				case "type": return paramList[0].Type();
				case "validre": return paramList[0].ValidRE();
				default: throw new ArgumentException(String.Format("Invalid method: {0}", method));
			}
		}

		public override ExpressionResult VisitConstant(ExpressionParser.ConstantContext context)
		{
			var constant = context.constant.Text;
			switch (constant.ToLowerInvariant())
			{
				case "pi": return new ExpressionResult(Math.PI);
				case "e": return new ExpressionResult(Math.E);
				case "i": return new ExpressionResult(Complex.ImaginaryOne);
				default: throw new ArgumentException(String.Format("Invalid constant: {0}", constant));
			}
		}

		public override ExpressionResult VisitShortForm(ExpressionParser.ShortFormContext context) { return GetShortForm(context.op.Text); }
		public override ExpressionResult VisitDefaultOpForm(ExpressionParser.DefaultOpFormContext context) { return GetShortForm("&&"); }
		public override ExpressionResult VisitDot(ExpressionParser.DotContext context) { return BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2)); }
		public override ExpressionResult VisitUnary(ExpressionParser.UnaryContext context) { return UnaryOp(context.op.Text, Visit(context.val)); }
		public override ExpressionResult VisitUnaryEnd(ExpressionParser.UnaryEndContext context) { return UnaryOpEnd(context.op.Text, Visit(context.val)); }
		public override ExpressionResult VisitExp(ExpressionParser.ExpContext context) { return BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2)); }
		public override ExpressionResult VisitMult(ExpressionParser.MultContext context) { return BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2)); }
		public override ExpressionResult VisitAdd(ExpressionParser.AddContext context) { return BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2)); }
		public override ExpressionResult VisitShift(ExpressionParser.ShiftContext context) { return BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2)); }
		public override ExpressionResult VisitRelational(ExpressionParser.RelationalContext context) { return BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2)); }
		public override ExpressionResult VisitEquality(ExpressionParser.EqualityContext context) { return BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2)); }
		public override ExpressionResult VisitBitwiseAnd(ExpressionParser.BitwiseAndContext context) { return BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2)); }
		public override ExpressionResult VisitBitwiseXor(ExpressionParser.BitwiseXorContext context) { return BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2)); }
		public override ExpressionResult VisitBitwiseOr(ExpressionParser.BitwiseOrContext context) { return BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2)); }
		public override ExpressionResult VisitLogicalAnd(ExpressionParser.LogicalAndContext context) { return BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2)); }
		public override ExpressionResult VisitLogicalOr(ExpressionParser.LogicalOrContext context) { return BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2)); }
		public override ExpressionResult VisitNullCoalesce(ExpressionParser.NullCoalesceContext context) { return BinaryOp(context.op.Text, Visit(context.val1), Visit(context.val2)); }
		public override ExpressionResult VisitTernary(ExpressionParser.TernaryContext context) { return Visit(context.condition).True ? Visit(context.trueval) : Visit(context.falseval); }
		public override ExpressionResult VisitExpression(ExpressionParser.ExpressionContext context) { return Visit(context.val); }
		public override ExpressionResult VisitParam(ExpressionParser.ParamContext context) { return values[int.Parse(context.val.Text.Trim('[', ']'))]; }
		public override ExpressionResult VisitString(ExpressionParser.StringContext context) { return new ExpressionResult(context.val.Text.Substring(1, context.val.Text.Length - 2)); }
		public override ExpressionResult VisitChar(ExpressionParser.CharContext context) { return new ExpressionResult(context.val.Text[1]); }
		public override ExpressionResult VisitTrue(ExpressionParser.TrueContext context) { return new ExpressionResult(true); }
		public override ExpressionResult VisitFalse(ExpressionParser.FalseContext context) { return new ExpressionResult(false); }
		public override ExpressionResult VisitNull(ExpressionParser.NullContext context) { return new ExpressionResult(null); }
		public override ExpressionResult VisitFloat(ExpressionParser.FloatContext context)
		{
			if (context.val.Text.Contains("."))
				return new ExpressionResult(double.Parse(context.val.Text));
			return new ExpressionResult(BigInteger.Parse(context.val.Text));
		}
		public override ExpressionResult VisitHex(ExpressionParser.HexContext context) { return new ExpressionResult(long.Parse(context.val.Text.Substring(2), NumberStyles.HexNumber)); }
		public override ExpressionResult VisitVariable(ExpressionParser.VariableContext context) { return dict.ContainsKey(context.val.Text) ? dict[context.val.Text] : null; }
	}
}
