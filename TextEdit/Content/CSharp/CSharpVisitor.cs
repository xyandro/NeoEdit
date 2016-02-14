using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using NeoEdit.Common;
using NeoEdit.Common.Parsing;
using NeoEdit.TextEdit.Content.CSharp.Parser;

namespace NeoEdit.TextEdit.Content.CSharp
{
	class CSharpVisitor : CSharpParserBaseVisitor<object>
	{
		public static ParserNode Parse(string input)
		{
			var tree = ParserHelper.Parse<CSharpLexer, CSharpParser, CSharpParser.CsharpContext>(input, parser => parser.csharp());
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

		const string ROOT = "Root";
		const string BASE = "Base";
		const string BLOCK = "Block";
		const string CASES = "Cases";
		const string CONDITION = "Condition";
		const string DECLARATION = "Declaration";
		const string EMPTY = "Empty";
		const string EXPRESSION = "Expression";
		const string GLOBALATTR = "GlobalAttr";
		const string INDEXER = "Indexer";
		const string LABEL = "Label";
		const string METHOD = "Method";
		const string MODIFIER = "Modifier";
		const string NAME = "Name";
		const string PROPERTY = "Property";
		const string USINGS = "Usings";
		const string METHODPARAM = "MethodParam";


		readonly ParserNode Root;
		ParserNode Parent => stack.Peek();
		readonly Stack<ParserNode> stack = new Stack<ParserNode>();
		readonly string input;
		CSharpVisitor(string input)
		{
			this.input = input;
			stack.Push(Root = new ParserNode { Type = ROOT, Start = 0, End = input.Length });
		}

		ParserNode AddNode(ParserRuleContext context, params object[] useTypes)
		{
			var node = new ParserNode { Parent = Parent, LocationParserRule = context };

			foreach (var useType in useTypes)
			{
				var type = useType as string;
				if (type != null)
					node.Type = type;
				var token = useType as IToken;
				if (token != null)
					node.AddAttr(ParserNode.TYPE, input, token);
				var terminalNode = useType as ITerminalNode;
				if (terminalNode != null)
					node.AddAttr(ParserNode.TYPE, input, terminalNode);
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

		public override object VisitSaveblock(CSharpParser.SaveblockContext context) => AddNode(context, BLOCK);
		public override object VisitContentExpression(CSharpParser.ContentExpressionContext context) => AddNode(context, EXPRESSION);
		public override object VisitNamespace(CSharpParser.NamespaceContext context) => AddNode(context, context.NAMESPACE());
		public override object VisitClass(CSharpParser.ClassContext context) => AddNode(context, context.CLASS());
		public override object VisitEnum(CSharpParser.EnumContext context) => AddNode(context, context.ENUM());
		public override object VisitMethod(CSharpParser.MethodContext context) => AddNode(context, METHOD);
		public override object VisitIndexer(CSharpParser.IndexerContext context) => AddNode(context, INDEXER);
		public override object VisitDelegate(CSharpParser.DelegateContext context) => AddNode(context, context.DELEGATE());
		public override object VisitEvent(CSharpParser.EventContext context) => AddNode(context, context.EVENT());
		public override object VisitProperty(CSharpParser.PropertyContext context) => AddNode(context, PROPERTY);
		public override object VisitSwitch(CSharpParser.SwitchContext context) => AddNode(context, context.SWITCH());
		public override object VisitReturn(CSharpParser.ReturnContext context) => AddNode(context, context.RETURN());
		public override object VisitThrow(CSharpParser.ThrowContext context) => AddNode(context, context.THROW());
		public override object VisitDeclaration(CSharpParser.DeclarationContext context) => AddNode(context, DECLARATION);
		public override object VisitIf(CSharpParser.IfContext context) => AddNode(context, context.type);
		public override object VisitLabel(CSharpParser.LabelContext context) => AddNode(context, LABEL);
		public override object VisitGoto(CSharpParser.GotoContext context) => AddNode(context, context.GOTO());
		public override object VisitFixed(CSharpParser.FixedContext context) => AddNode(context, context.FIXED());
		public override object VisitChecked(CSharpParser.CheckedContext context) => AddNode(context, context.CHECKED() ?? context.UNCHECKED());
		public override object VisitUsing(CSharpParser.UsingContext context) => AddNode(context, context.USING());
		public override object VisitLock(CSharpParser.LockContext context) => AddNode(context, context.LOCK());
		public override object VisitFor(CSharpParser.ForContext context) => AddNode(context, context.FOR());
		public override object VisitForeach(CSharpParser.ForeachContext context) => AddNode(context, context.FOREACH());
		public override object VisitWhile(CSharpParser.WhileContext context) => AddNode(context, context.WHILE());
		public override object VisitDo(CSharpParser.DoContext context) => AddNode(context, context.DO());
		public override object VisitBreak(CSharpParser.BreakContext context) => AddNode(context, context.BREAK());
		public override object VisitContinue(CSharpParser.ContinueContext context) => AddNode(context, context.CONTINUE());
		public override object VisitTry(CSharpParser.TryContext context) => AddNode(context, context.TRY());
		public override object VisitGlobalAttr(CSharpParser.GlobalAttrContext context) => AddNode(context, GLOBALATTR);
		public override object VisitUsingns(CSharpParser.UsingnsContext context) => AddNode(context, context.USING());
		public override object VisitPropertyaccess(CSharpParser.PropertyaccessContext context) => AddNode(context, context.GET() ?? context.SET());
		public override object VisitSavesemicolon(CSharpParser.SavesemicolonContext context) => AddNode(context, EMPTY);
		public override object VisitCases(CSharpParser.CasesContext context) => AddNode(context, CASES);
		public override object VisitMethodcallparam(CSharpParser.MethodcallparamContext context) => AddNode(context, METHODPARAM);

		public override object VisitSavename(CSharpParser.SavenameContext context) => AddAttribute(NAME, context);
		public override object VisitSavebase(CSharpParser.SavebaseContext context) => AddAttribute(BASE, context);
		public override object VisitModifier(CSharpParser.ModifierContext context) => AddAttribute(MODIFIER, context);
		public override object VisitSaveconditionexpression(CSharpParser.SaveconditionexpressionContext context) => AddAttribute(CONDITION, context);

		public override object VisitUsings(CSharpParser.UsingsContext context)
		{
			var node = AddNode(context, USINGS);

			// Remove "USINGS" node if there's only one child
			var children = node.List(ParserNode.ParserNodeListType.Children).ToList();
			if (children.Count == 1)
			{
				var child = children.Single();
				child.Parent = node.Parent;
				node.Parent = null;
			}
			return node;
		}
	}
}
