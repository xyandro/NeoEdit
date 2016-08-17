using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using NeoEdit.Common;
using NeoEdit.Common.Parsing;
using NeoEdit.TextEdit.Content.SQL.Parser;

namespace NeoEdit.TextEdit.QueryBuilding
{
	class QBVisitor : SQLParserBaseVisitor<object>
	{
		public static QuerySelect Parse(string input, IEnumerable<TableSelect> tables)
		{
			var tree = ParserHelper.Parse<SQLLexer, SQLParser, SQLParser.SelectentryContext>(input, parser => parser.selectentry());
			return new QBVisitor(input, tables).Visit(tree) as QuerySelect;
		}

		readonly string input;
		private readonly Dictionary<string, TableSelect> tables;
		QBVisitor(string input, IEnumerable<TableSelect> tables)
		{
			this.input = input;
			this.tables = tables.ToDictionary(table => table.Table, StringComparer.OrdinalIgnoreCase);
		}

		string GetString(object value)
		{
			if (value == null)
				return null;
			else if (value is ParserRuleContext)
			{
				int start, end;
				(value as ParserRuleContext).GetBounds(out start, out end);
				return input.Substring(start, end - start);
			}
			else if (value is IToken)
			{
				int start, end;
				(value as IToken).GetBounds(out start, out end);
				return input.Substring(start, end - start);
			}

			throw new Exception("Invalid input");
		}

		QuerySelect.Directions GetDirection(string direction)
		{
			switch (direction?.StripWhitespace()?.ToLowerInvariant())
			{
				case null: case "": return QuerySelect.Directions.None;
				case "asc": return QuerySelect.Directions.Asc;
				case "desc": return QuerySelect.Directions.Desc;
				default: return QuerySelect.Directions.None;
			}
		}

		QuerySelect.JoinType GetJoinType(string joinType)
		{
			switch (joinType.StripWhitespace().ToLowerInvariant())
			{
				case "join": return QuerySelect.JoinType.InnerJoin;
				case "fulljoin": return QuerySelect.JoinType.FullJoin;
				case "fullouterjoin": return QuerySelect.JoinType.FullJoin;
				case "leftjoin": return QuerySelect.JoinType.LeftJoin;
				case "leftouterjoin": return QuerySelect.JoinType.LeftJoin;
				case "innerjoin": return QuerySelect.JoinType.InnerJoin;
				case "crossjoin": return QuerySelect.JoinType.CrossJoin;
				case "crossapply": return QuerySelect.JoinType.CrossApply;
				case "outerapply": return QuerySelect.JoinType.OuterApply;
				default: return QuerySelect.JoinType.InnerJoin;
			}
		}

		public override object VisitSelectentry([NotNull] SQLParser.SelectentryContext context) => context.select() == null ? new QuerySelect() : Visit(context.select());

		public override object VisitSelect([NotNull] SQLParser.SelectContext context)
		{
			var select = new QuerySelect();
			context.selects().selected().ForEach(selected => select.Selects.Add(new QuerySelect.SelectedData { Expr = GetString(selected.Selected), Alias = GetString(selected.alias()?.Alias) }));
			if (context.sourceclause() != null)
				select.Source.AddRange(Visit(context.sourceclause()) as List<QuerySelect.JoinData>);
			select.Where = GetString(context.whereclause()?.Condition);
			select.GroupBy.AddRange(context.groupbyclause()?.groupbyitem()?.Select(item => GetString(item)) ?? new List<string>());
			select.Having = GetString(context.havingclause()?.Condition);
			select.OrderBy.AddRange(context.orderbyclause()?.orderbyitem()?.Select(item => new QuerySelect.OrderByData { Expr = GetString(item.Expr), Direction = GetDirection(GetString(item.Direction)) }) ?? new List<QuerySelect.OrderByData>());
			return select;
		}

		Select GetTable(SQLParser.ExprContext context)
		{
			var result = Visit(context);
			if (result is Select)
				return result as Select;

			var str = GetString(context);
			var table = Regex.Replace(str, @"[ \t\r\n\[\]]+", "");
			if (tables.ContainsKey(table))
				return tables[table];

			return new TableSelect(str);
		}

		public override object VisitSourceclause([NotNull] SQLParser.SourceclauseContext context) => context.children.Select(child => Visit(child)).OfType<List<QuerySelect.JoinData>>().SelectMany(list => list).ToList();
		public override object VisitFromclause([NotNull] SQLParser.FromclauseContext context) => context.selecttable().Select(selecttable => new QuerySelect.JoinData { Type = QuerySelect.JoinType.Normal, Table = GetTable(selecttable.Table), Alias = GetString(selecttable.alias()?.Alias) }).ToList();
		public override object VisitJoinclause([NotNull] SQLParser.JoinclauseContext context) => new List<QuerySelect.JoinData> { new QuerySelect.JoinData { Type = GetJoinType(GetString(context.JoinType)), Table = GetTable(context.selecttable().Table), Alias = GetString(context.selecttable().alias()?.Alias), Condition = GetString(context.Condition) } };
		public override object VisitApplyclause([NotNull] SQLParser.ApplyclauseContext context) => new List<QuerySelect.JoinData> { new QuerySelect.JoinData { Type = GetJoinType(GetString(context.ApplyType)), Table = GetTable(context.selecttable().Table), Alias = GetString(context.selecttable().alias()?.Alias) } };

		//public override object VisitSelecttable([NotNull] SQLParser.SelecttableContext context)
		//{
		//	return base.VisitSelecttable(context);
		//}

		//public override object VisitExpr([NotNull] SQLParser.ExprContext context) => context.GetText();
	}
}
