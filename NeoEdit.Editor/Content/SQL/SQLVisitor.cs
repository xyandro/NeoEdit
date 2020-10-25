using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using NeoEdit.Common;
using NeoEdit.Common.Parsing;
using NeoEdit.Editor.Content.SQL.Parser;

namespace NeoEdit.Editor.Content.SQL
{
	class SQLVisitor : SQLParserBaseVisitor<ParserNode>
	{
		public static ParserNode Parse(string input, bool strict)
		{
			var tree = ParserHelper.Parse<SQLLexer, SQLParser, SQLParser.DocumentContext>(input, parser => parser.document(), strict);
			var visitor = new SQLVisitor(input);
			visitor.Visit(tree);
			return visitor.Root;
		}

		enum NodeTypes
		{
			Document,
			DDL,
			TSQL,
			Transaction,
			Cursor,
			Delete,
			Insert,
			Merge,
			Select,
			Truncate,
			Update,
			With,
			Selected,
			SelectTable,
			Selects,
			Source,
			Where,
			GroupBy,
			Having,
			OrderBy,
			Expr,
			Table,
			Alias,
			From,
			Join,
			Condition,
			JoinType,
			ApplyType,
			OrderByItem,
			Direction,
			GroupByItem,
		}

		readonly ParserNode Root;
		ParserNode Parent => stack.Peek();
		readonly Stack<ParserNode> stack = new Stack<ParserNode>();
		readonly string input;
		SQLVisitor(string input)
		{
			this.input = input;
			stack.Push(Root = new ParserNode { Type = nameof(NodeTypes.Document), Start = 0, End = input.Length });
		}

		ParserNode AddNode(ParserRuleContext context, NodeTypes type, Dictionary<NodeTypes, object> attributes = null)
		{
			var node = new ParserNode { Parent = Parent, LocationParserRule = context, Type = type.ToString() };

			attributes?.ForEach(pair => AddAttribute(node, pair.Key, pair.Value));

			stack.Push(node);
			VisitChildren(context);
			stack.Pop();
			return node;
		}

		ParserNode AddAttribute(ParserNode node, NodeTypes name, object value)
		{
			if (value == null)
			{ }
			else if (value is string)
				node.AddAttr(name.ToString(), value as string);
			else if (value is IToken)
				node.AddAttr(name.ToString(), input, value as IToken);
			else if (value is ITerminalNode)
				node.AddAttr(name.ToString(), input, value as ITerminalNode);
			else if (value is ParserRuleContext)
				node.AddAttr(name.ToString(), input, value as ParserRuleContext);
			else if (value is IEnumerable<ParserRuleContext>)
			{
				foreach (var value2 in value as IEnumerable<ParserRuleContext>)
					node.AddAttr(name.ToString(), input, value2);
			}
			else
				throw new Exception($"Unknown attribute: {name}");
			return node;
		}

		public override ParserNode VisitDocument([NotNull] SQLParser.DocumentContext context) => AddNode(context, NodeTypes.Document);
		public override ParserNode VisitDdl([NotNull] SQLParser.DdlContext context) => AddNode(context, NodeTypes.DDL);
		public override ParserNode VisitTsql([NotNull] SQLParser.TsqlContext context) => AddNode(context, NodeTypes.TSQL);
		public override ParserNode VisitTrans([NotNull] SQLParser.TransContext context) => AddNode(context, NodeTypes.Transaction);
		public override ParserNode VisitCursor([NotNull] SQLParser.CursorContext context) => AddNode(context, NodeTypes.Cursor);

		public override ParserNode VisitDelete([NotNull] SQLParser.DeleteContext context) => AddNode(context, NodeTypes.Delete);
		public override ParserNode VisitInsert([NotNull] SQLParser.InsertContext context) => AddNode(context, NodeTypes.Insert);
		public override ParserNode VisitMerge([NotNull] SQLParser.MergeContext context) => AddNode(context, NodeTypes.Merge);
		public override ParserNode VisitSelect([NotNull] SQLParser.SelectContext context) => AddNode(context, NodeTypes.Select);
		public override ParserNode VisitSelects([NotNull] SQLParser.SelectsContext context) => AddNode(context, NodeTypes.Selects);
		public override ParserNode VisitSelected([NotNull] SQLParser.SelectedContext context) => AddNode(context, NodeTypes.Selected, new Dictionary<NodeTypes, object> { [NodeTypes.Expr] = context.Selected });
		public override ParserNode VisitSourceclause([NotNull] SQLParser.SourceclauseContext context) => AddNode(context, NodeTypes.Source);
		public override ParserNode VisitFromclause([NotNull] SQLParser.FromclauseContext context) => AddNode(context, NodeTypes.From);
		public override ParserNode VisitJoinclause([NotNull] SQLParser.JoinclauseContext context) => AddNode(context, NodeTypes.Join, new Dictionary<NodeTypes, object> { [NodeTypes.JoinType] = context.JoinType, [NodeTypes.Condition] = context.Condition });
		public override ParserNode VisitApplyclause([NotNull] SQLParser.ApplyclauseContext context) => AddNode(context, NodeTypes.Join, new Dictionary<NodeTypes, object> { [NodeTypes.ApplyType] = context.ApplyType });
		public override ParserNode VisitWhereclause([NotNull] SQLParser.WhereclauseContext context) => AddNode(context, NodeTypes.Where, new Dictionary<NodeTypes, object> { [NodeTypes.Condition] = context.Condition });
		public override ParserNode VisitGroupbyclause([NotNull] SQLParser.GroupbyclauseContext context) => AddNode(context, NodeTypes.GroupBy);
		public override ParserNode VisitGroupbyitem([NotNull] SQLParser.GroupbyitemContext context) => AddNode(context, NodeTypes.GroupByItem, new Dictionary<NodeTypes, object> { [NodeTypes.Expr] = context.expr() });
		public override ParserNode VisitHavingclause([NotNull] SQLParser.HavingclauseContext context) => AddNode(context, NodeTypes.Having, new Dictionary<NodeTypes, object> { [NodeTypes.Condition] = context.Condition });
		public override ParserNode VisitOrderbyclause([NotNull] SQLParser.OrderbyclauseContext context) => AddNode(context, NodeTypes.OrderBy);
		public override ParserNode VisitOrderbyitem([NotNull] SQLParser.OrderbyitemContext context) => AddNode(context, NodeTypes.OrderByItem, new Dictionary<NodeTypes, object> { [NodeTypes.Expr] = context.Expr, [NodeTypes.Direction] = context.Direction });
		public override ParserNode VisitTruncate([NotNull] SQLParser.TruncateContext context) => AddNode(context, NodeTypes.Truncate);
		public override ParserNode VisitUpdate([NotNull] SQLParser.UpdateContext context) => AddNode(context, NodeTypes.Update);
		public override ParserNode VisitWith([NotNull] SQLParser.WithContext context) => AddNode(context, NodeTypes.With);
		public override ParserNode VisitSelecttable([NotNull] SQLParser.SelecttableContext context) => AddAttribute(Parent, NodeTypes.Table, context.Table);
		public override ParserNode VisitAlias([NotNull] SQLParser.AliasContext context) => AddAttribute(Parent, NodeTypes.Alias, context.Alias);
	}
}
