using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.TextEdit.QueryBuilding
{
	class QuerySelect : Select
	{
		public class SelectedData
		{
			public string Expr { get; set; }
			public string Alias { get; set; }
		}

		class JoinTextAttribute : Attribute
		{
			public string Text { get; }
			public JoinTextAttribute(string text) { Text = text; }
		}

		public enum JoinType
		{
			Normal,

			StartJoin,
			[JoinText("JOIN")]
			InnerJoin,
			[JoinText("LEFT JOIN")]
			LeftJoin,
			[JoinText("RIGHT JOIN")]
			RightJoin,
			[JoinText("FULL OUTER JOIN")]
			FullJoin,
			[JoinText(",")]
			CrossJoin,
			EndJoin,

			StartApply,
			[JoinText("CROSS APPLY")]
			CrossApply,
			[JoinText("OUTER APPLY")]
			OuterApply,
			EndApply,
		}

		public class JoinData
		{
			public JoinType Type { get; set; }
			public Select Table { get; set; }
			public string Alias { get; set; }
			public string Condition { get; set; }
		}

		public enum Directions
		{
			None,
			Asc,
			Desc,
		}

		public class OrderByData
		{
			public string Expr { get; set; }
			public Directions Direction { get; set; }
		}

		public List<SelectedData> Selects { get; set; } = new List<SelectedData>();
		public List<JoinData> Source { get; set; } = new List<JoinData>();
		public string Where { get; set; }
		public List<string> GroupBy { get; set; } = new List<string>();
		public string Having { get; set; }
		public List<OrderByData> OrderBy { get; set; } = new List<OrderByData>();

		public static QuerySelect FromStr(string query, IEnumerable<TableSelect> tables) => QBVisitor.Parse(query, tables);

		public override IEnumerable<string> Columns => Selects.Select(sel => sel.Alias);

		public override IEnumerable<string> QueryLines
		{
			get
			{
				yield return "SELECT";
				foreach (var select in Selects)
				{
					var str = $"\t{select.Expr}";
					if (!string.IsNullOrEmpty(select.Alias))
						str += $" AS {select.Alias}";
					if (select != Selects.Last())
						str += ",";
					yield return str;
				}
				foreach (var source in Source)
				{
					if (source == Source.First())
						yield return "FROM";

					var table = $"{string.Join(" ", source.Table.QueryLines.Select(val => val.Trim()))}";
					var alias = string.IsNullOrEmpty(source.Alias) ? "" : $" AS {source.Alias}";

					var joinApplyText = typeof(JoinType).GetMember(source.Type.ToString()).FirstOrDefault()?.GetCustomAttributes(typeof(JoinTextAttribute), false).Cast<JoinTextAttribute>().FirstOrDefault()?.Text + " " ?? "";
					if ((source.Type == JoinType.Normal) && (source == Source.First()))
						joinApplyText = "";
					var str = $"\t{joinApplyText}{table}{alias}";
					if ((source.Type >= JoinType.StartJoin) && (source.Type <= JoinType.EndJoin))
						str += $" ON {source.Condition}";
					yield return str;
				}
				if (!string.IsNullOrEmpty(Where))
				{
					yield return "WHERE";
					yield return $"\t{Where}";
				}
				foreach (var groupBy in GroupBy)
				{
					if (groupBy == GroupBy.First())
						yield return "GROUP BY";
					yield return $"\t{groupBy}{(groupBy == GroupBy.Last() ? "" : ",")}";
				}
				if (!string.IsNullOrEmpty(Having))
				{
					yield return "HAVING";
					yield return $"\t{Having}";
				}
				foreach (var orderBy in OrderBy)
				{
					if (orderBy == OrderBy.First())
						yield return "ORDER BY";
					yield return $"\t{orderBy.Expr}{(orderBy.Direction == Directions.None ? "" : $" {orderBy.Direction}".ToUpperInvariant())}{(orderBy == OrderBy.Last() ? "" : ",")}";
				}
			}
		}
	}
}
