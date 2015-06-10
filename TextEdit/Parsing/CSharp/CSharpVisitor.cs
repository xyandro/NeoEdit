using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Tree;
using NeoEdit.TextEdit.Parsing.CSharp.Parser;

[assembly: AssemblyFlags(AssemblyNameFlags.None)]

namespace NeoEdit.TextEdit.Parsing.CSharp
{
	class CSharpVisitor : CSharpParserBaseVisitor<object>
	{
		public static ParserNode Parse(string input)
		{
			var inputStream = new AntlrInputStream(input);
			var lexer = new CSharpLexer(inputStream);
			var tokens = new CommonTokenStream(lexer);
			var parser = new CSharpParser(tokens);

			CSharpParser.CsharpContext tree;
			try
			{
				tree = parser.csharp();
			}
			catch
			{
				tokens.Reset();
				parser.Reset();
				parser.Interpreter.PredictionMode = PredictionMode.Ll;
				tree = parser.csharp();
			}

			var visitor = new CSharpVisitor(input);
			visitor.Visit(tree);
			return visitor.Root;
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


		readonly ParserNode Root;
		ParserNode Parent { get { return stack.Peek(); } }
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

		public override object VisitSaveblock(CSharpParser.SaveblockContext context) { return AddNode(context, BLOCK); }
		public override object VisitContentExpression(CSharpParser.ContentExpressionContext context) { return AddNode(context, EXPRESSION); }
		public override object VisitNamespace(CSharpParser.NamespaceContext context) { return AddNode(context, context.NAMESPACE()); }
		public override object VisitClass(CSharpParser.ClassContext context) { return AddNode(context, context.CLASS()); }
		public override object VisitEnum(CSharpParser.EnumContext context) { return AddNode(context, context.ENUM()); }
		public override object VisitMethod(CSharpParser.MethodContext context) { return AddNode(context, METHOD); }
		public override object VisitIndexer(CSharpParser.IndexerContext context) { return AddNode(context, INDEXER); }
		public override object VisitDelegate(CSharpParser.DelegateContext context) { return AddNode(context, context.DELEGATE()); }
		public override object VisitEvent(CSharpParser.EventContext context) { return AddNode(context, context.EVENT()); }
		public override object VisitProperty(CSharpParser.PropertyContext context) { return AddNode(context, PROPERTY); }
		public override object VisitSwitch(CSharpParser.SwitchContext context) { return AddNode(context, context.SWITCH()); }
		public override object VisitReturn(CSharpParser.ReturnContext context) { return AddNode(context, context.RETURN()); }
		public override object VisitThrow(CSharpParser.ThrowContext context) { return AddNode(context, context.THROW()); }
		public override object VisitDeclaration(CSharpParser.DeclarationContext context) { return AddNode(context, DECLARATION); }
		public override object VisitIf(CSharpParser.IfContext context) { return AddNode(context, context.type); }
		public override object VisitLabel(CSharpParser.LabelContext context) { return AddNode(context, LABEL); }
		public override object VisitGoto(CSharpParser.GotoContext context) { return AddNode(context, context.GOTO()); }
		public override object VisitFixed(CSharpParser.FixedContext context) { return AddNode(context, context.FIXED()); }
		public override object VisitChecked(CSharpParser.CheckedContext context) { return AddNode(context, context.CHECKED() ?? context.UNCHECKED()); }
		public override object VisitUsing(CSharpParser.UsingContext context) { return AddNode(context, context.USING()); }
		public override object VisitLock(CSharpParser.LockContext context) { return AddNode(context, context.LOCK()); }
		public override object VisitFor(CSharpParser.ForContext context) { return AddNode(context, context.FOR()); }
		public override object VisitForeach(CSharpParser.ForeachContext context) { return AddNode(context, context.FOREACH()); }
		public override object VisitWhile(CSharpParser.WhileContext context) { return AddNode(context, context.WHILE()); }
		public override object VisitDo(CSharpParser.DoContext context) { return AddNode(context, context.DO()); }
		public override object VisitBreak(CSharpParser.BreakContext context) { return AddNode(context, context.BREAK()); }
		public override object VisitContinue(CSharpParser.ContinueContext context) { return AddNode(context, context.CONTINUE()); }
		public override object VisitTry(CSharpParser.TryContext context) { return AddNode(context, context.TRY()); }
		public override object VisitGlobalAttr(CSharpParser.GlobalAttrContext context) { return AddNode(context, GLOBALATTR); }
		public override object VisitUsingns(CSharpParser.UsingnsContext context) { return AddNode(context, context.USING()); }
		public override object VisitPropertyaccess(CSharpParser.PropertyaccessContext context) { return AddNode(context, context.GET() ?? context.SET()); }
		public override object VisitSavesemicolon(CSharpParser.SavesemicolonContext context) { return AddNode(context, EMPTY); }
		public override object VisitCases(CSharpParser.CasesContext context) { return AddNode(context, CASES); }

		public override object VisitSavename(CSharpParser.SavenameContext context) { return AddAttribute(NAME, context); }
		public override object VisitSavebase(CSharpParser.SavebaseContext context) { return AddAttribute(BASE, context); }
		public override object VisitModifier(CSharpParser.ModifierContext context) { return AddAttribute(MODIFIER, context); }
		public override object VisitSaveconditionexpression(CSharpParser.SaveconditionexpressionContext context) { return AddAttribute(CONDITION, context); }

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
