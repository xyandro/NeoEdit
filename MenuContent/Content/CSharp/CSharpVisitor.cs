using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using NeoEdit;
using NeoEdit.Parsing;
using NeoEdit.Content.CSharp.Parser;

namespace NeoEdit.Content.CSharp
{
	class CSharpVisitor : CSharpParserBaseVisitor<object>
	{
		public static ParserNode Parse(string input, bool strict)
		{
			var tree = ParserHelper.Parse<CSharpLexer, CSharpParser, CSharpParser.CsharpContext>(input, parser => parser.csharp(), strict);
			var visitor = new CSharpVisitor(input);
			visitor.Visit(tree);
			return visitor.Root;
		}

		public static string Comment(TextData data, Range range)
		{
			var startLine = data.GetOffsetLine(range.Start);
			var startIndex = data.GetOffsetIndex(range.Start, startLine);
			var endLine = data.GetOffsetLine(range.End);
			var endIndex = data.GetOffsetIndex(range.End, endLine);
			var result = "";
			for (var line = startLine; line <= endLine; ++line)
			{
				var lineOffset = data.GetOffset(line, 0);
				var start = line == startLine ? startIndex : 0;
				var contentEnd = line == endLine ? endIndex : data.GetLineLength(line);
				var end = line == endLine ? endIndex : contentEnd + data.GetEndingLength(line);
				var str = data.GetString(lineOffset + start, end - start);
				if (start != contentEnd)
					str = $"//{str}";
				result += str;
			}
			return result;
		}

		public static string Uncomment(TextData data, Range range)
		{
			var startLine = data.GetOffsetLine(range.Start);
			var startIndex = data.GetOffsetIndex(range.Start, startLine);
			var endLine = data.GetOffsetLine(range.End);
			var endIndex = data.GetOffsetIndex(range.End, endLine);
			var result = "";
			for (var line = startLine; line <= endLine; ++line)
			{
				var lineOffset = data.GetOffset(line, 0);
				var start = line == startLine ? startIndex : 0;
				var contentEnd = line == endLine ? endIndex : data.GetLineLength(line);
				var end = line == endLine ? endIndex : contentEnd + data.GetEndingLength(line);
				var str = data.GetString(lineOffset + start, end - start);
				if (str.StartsWith("//"))
					str = str.Substring(2);
				result += str;
			}
			return result;
		}

		readonly ParserNode Root;
		ParserNode Parent => stack.Peek();
		readonly Stack<ParserNode> stack = new Stack<ParserNode>();
		readonly string input;
		CSharpVisitor(string input)
		{
			this.input = input;
			stack.Push(Root = new ParserNode { Type = "Root", Start = 0, End = input.Length });
		}

		ParserNode AddNode(ParserRuleContext context, Dictionary<string, object> attributes)
		{
			var node = new ParserNode { Parent = Parent, LocationParserRule = context };

			foreach (var pair in attributes)
			{
				if (pair.Value == null)
				{ }
				else if (pair.Value is string)
					node.AddAttr(pair.Key, pair.Value as string);
				else if (pair.Value is IToken)
					node.AddAttr(pair.Key, input, pair.Value as IToken);
				else if (pair.Value is ITerminalNode)
					node.AddAttr(pair.Key, input, pair.Value as ITerminalNode);
				else if (pair.Value is ParserRuleContext)
					node.AddAttr(pair.Key, input, pair.Value as ParserRuleContext);
				else if (pair.Value is IEnumerable<ParserRuleContext>)
				{
					foreach (var value in pair.Value as IEnumerable<ParserRuleContext>)
						node.AddAttr(pair.Key, input, value as ParserRuleContext);
				}
				else
					throw new Exception($"Unknown attribute: {pair.Key}");
			}

			stack.Push(node);
			VisitChildren(context);
			stack.Pop();
			return node;
		}

		object AddAttribute(string name, ParserRuleContext context)
		{
			if (context != null)
				Parent.AddAttr(name, input, context);
			return null;
		}

		public override object VisitExternalias([NotNull] CSharpParser.ExternaliasContext context) => AddNode(context, new Dictionary<string, object>
		{
			["Type"] = "ExternAlias",
		});
		public override object VisitUsingns([NotNull] CSharpParser.UsingnsContext context) => AddNode(context, new Dictionary<string, object>
		{
			["Type"] = "UsingNS",
		});
		public override object VisitNamespace([NotNull] CSharpParser.NamespaceContext context) => AddNode(context, new Dictionary<string, object>
		{
			["Type"] = "Namespace",
			["Name"] = context.Name,
		});
		public override object VisitClass([NotNull] CSharpParser.ClassContext context) => AddNode(context, new Dictionary<string, object>
		{
			["Type"] = context.NodeType.Text.ToProper(),
			["Name"] = context.Name,
			["Base"] = context.type().Where(type => type != context.Name),
			["Attribute"] = context.attribute(),
			["Modifier"] = context.modifier(),
			["Where"] = context.where(),
		});
		public override object VisitProperty([NotNull] CSharpParser.PropertyContext context) => AddNode(context, new Dictionary<string, object>
		{
			["Type"] = "Property",
			["Name"] = (object)context.Name ?? context.THIS(),
			["Return"] = context.Return,
			["Attribute"] = context.attribute(),
			["Modifier"] = context.modifier(),
		});
		public override object VisitAccessor([NotNull] CSharpParser.AccessorContext context) => AddNode(context, new Dictionary<string, object>
		{
			["Type"] = "Accessor",
			["Name"] = context.Name,
			["Attribute"] = context.attribute(),
			["Modifier"] = context.modifier(),
		});
		public override object VisitMethod([NotNull] CSharpParser.MethodContext context) => AddNode(context, new Dictionary<string, object>
		{
			["Type"] = "Method",
			["Name"] = (object)context.Name ?? context.Operator,
			["Return"] = context.Return,
			["Attribute"] = context.attribute(),
			["Modifier"] = context.modifier(),
			["Where"] = context.where(),
			["Param"] = context.paramlist().param(),
			["ParamName"] = context.paramlist().param().Select(param => param.Name),
		});
		public override object VisitField([NotNull] CSharpParser.FieldContext context) => AddNode(context, new Dictionary<string, object>
		{
			["Type"] = "Field",
			["Name"] = context.vardecl().Select(vardecl => vardecl.type()),
			["Return"] = context.Return,
			["Attribute"] = context.attribute(),
			["Modifier"] = context.modifier(),
		});
		public override object VisitEvent([NotNull] CSharpParser.EventContext context) => AddNode(context, new Dictionary<string, object>
		{
			["Type"] = "Event",
			["Name"] = context.Name,
			["Return"] = context.Return,
			["Attribute"] = context.attribute(),
			["Modifier"] = context.modifier(),
		});
		public override object VisitEnum([NotNull] CSharpParser.EnumContext context) => AddNode(context, new Dictionary<string, object>
		{
			["Type"] = "Enum",
			["Name"] = context.Name,
			["Return"] = context.Return,
			["Attribute"] = context.attribute(),
			["Modifier"] = context.modifier(),
		});
		public override object VisitEnumvalue([NotNull] CSharpParser.EnumvalueContext context) => AddNode(context, new Dictionary<string, object>
		{
			["Type"] = "EnumValue",
			["Name"] = context.Name,
			["Attribute"] = context.attribute(),
		});
		public override object VisitGlobalattribute([NotNull] CSharpParser.GlobalattributeContext context) => AddNode(context, new Dictionary<string, object>
		{
			["Type"] = "GlobalAttribute",
		});
		public override object VisitVariable([NotNull] CSharpParser.VariableContext context) => AddNode(context, new Dictionary<string, object>
		{
			["Type"] = "Variable",
			["Name"] = context.vardecl().Select(vardecl => vardecl.Name),
			["Return"] = context.Return,
			["Modifier"] = context.modifier(),
		});
		public override object VisitGotoLabel([NotNull] CSharpParser.GotoLabelContext context) => AddNode(context, new Dictionary<string, object>
		{
			["Type"] = "GotoLabel",
			["Name"] = context.Name,
		});
		public override object VisitBlock([NotNull] CSharpParser.BlockContext context) => AddNode(context, new Dictionary<string, object>
		{
			["Type"] = "Block",
		});
		public override object VisitContentExpression([NotNull] CSharpParser.ContentExpressionContext context) => AddNode(context, new Dictionary<string, object>
		{
			["Type"] = "Expression",
		});
		public override object VisitIf([NotNull] CSharpParser.IfContext context) => AddNode(context, new Dictionary<string, object>
		{
			["Type"] = "If",
			["Condition"] = context.Condition,
		});
		public override object VisitWhile([NotNull] CSharpParser.WhileContext context) => AddNode(context, new Dictionary<string, object>
		{
			["Type"] = "While",
			["Condition"] = context.Condition,
		});
		public override object VisitDo([NotNull] CSharpParser.DoContext context) => AddNode(context, new Dictionary<string, object>
		{
			["Type"] = "Do",
			["Condition"] = context.Condition,
		});
		public override object VisitFor([NotNull] CSharpParser.ForContext context) => AddNode(context, new Dictionary<string, object>
		{
			["Type"] = "For",
			["Initializer"] = context.forinitializer(),
			["Condition"] = context.expression(),
			["Iterator"] = context.foriterator(),
			["Variable"] = context.forinitializer()?.vardecl(),
			["VariableName"] = context.forinitializer()?.vardecl().Select(vardecl => vardecl.Name),
		});
		public override object VisitForeach([NotNull] CSharpParser.ForeachContext context) => AddNode(context, new Dictionary<string, object>
		{
			["Type"] = "ForEach",
			["Return"] = context.Return,
			["Variable"] = context.Variable,
			["List"] = context.List,
		});
		public override object VisitSwitch([NotNull] CSharpParser.SwitchContext context) => AddNode(context, new Dictionary<string, object>
		{
			["Type"] = "Switch",
			["Switch"] = context.Switch,
		});
		public override object VisitSwitchcase([NotNull] CSharpParser.SwitchcaseContext context) => AddNode(context, new Dictionary<string, object>
		{
			["Type"] = "SwitchCase",
			["Condition"] = context.expression(),
		});
		public override object VisitGoto([NotNull] CSharpParser.GotoContext context) => AddNode(context, new Dictionary<string, object>
		{
			["Type"] = "Goto",
			["Destination"] = context.gotodestination(),
		});
		public override object VisitChecked([NotNull] CSharpParser.CheckedContext context) => AddNode(context, new Dictionary<string, object>
		{
			["Type"] = context.Type.Text.ToProper(),
		});
		public override object VisitUnsafe([NotNull] CSharpParser.UnsafeContext context) => AddNode(context, new Dictionary<string, object>
		{
			["Type"] = "Unsafe",
		});
		public override object VisitLock([NotNull] CSharpParser.LockContext context) => AddNode(context, new Dictionary<string, object>
		{
			["Type"] = "Lock",
			["Lock"] = context.expression(),
		});
		public override object VisitUsing([NotNull] CSharpParser.UsingContext context) => AddNode(context, new Dictionary<string, object>
		{
			["Type"] = context.Type.Text.ToProper(),
			["Return"] = context.Return,
			["Variable"] = context.vardecl(),
			["VariableName"] = context.vardecl().Select(vardecl => vardecl.Name),
			["Expression"] = context.expression(),
		});
		public override object VisitTry([NotNull] CSharpParser.TryContext context) => AddNode(context, new Dictionary<string, object>
		{
			["Type"] = "Try",
		});
		public override object VisitBreak([NotNull] CSharpParser.BreakContext context) => AddNode(context, new Dictionary<string, object>
		{
			["Type"] = "Break",
		});
		public override object VisitContinue([NotNull] CSharpParser.ContinueContext context) => AddNode(context, new Dictionary<string, object>
		{
			["Type"] = "Continue",
		});
		public override object VisitReturn([NotNull] CSharpParser.ReturnContext context) => AddNode(context, new Dictionary<string, object>
		{
			["Type"] = "Return",
			["Return"] = context.expression(),
		});
		public override object VisitEmpty([NotNull] CSharpParser.EmptyContext context) => AddNode(context, new Dictionary<string, object>
		{
			["Type"] = "Empty",
		});
	}
}
