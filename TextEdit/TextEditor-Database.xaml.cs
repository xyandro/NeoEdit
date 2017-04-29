using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using NeoEdit.Common;
using NeoEdit.GUI.Controls;
using NeoEdit.TextEdit.Dialogs;
using NeoEdit.TextEdit.QueryBuilding;
using NeoEdit.TextEdit.QueryBuilding.Dialogs;

namespace NeoEdit.TextEdit
{
	partial class TextEditor
	{
		class QueryResult
		{
			public Exception Exception { get; set; }
			public string TableName { get; set; }
			public Table Table { get; set; }
		}

		[DepProp]
		public string DBName { get { return UIHelper<TextEditor>.GetPropValue<string>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }

		DbConnection dbConnection;

		string DBSanitize(string name) => (!string.IsNullOrEmpty(name)) && (!char.IsLetter(name[0])) ? $"[{name}]" : name;

		List<QueryResult> RunDBSelect(string commandText)
		{
			try
			{
				var result = new List<QueryResult>();
				var tableName = Regex.Match(commandText, @"\bFROM\b.*?([\[\]a-z\.]+)", RegexOptions.IgnoreCase).Groups[1].Value.Replace("[", "").Replace("]", "").CoalesceNullOrEmpty();
				using (var command = dbConnection.CreateCommand())
				{
					command.CommandText = commandText;
					using (var reader = command.ExecuteReader())
					{
						while (true)
						{
							if (reader.FieldCount != 0)
								result.Add(new QueryResult { TableName = tableName, Table = new Table(reader) });
							if (!reader.NextResult())
								break;
						}
					}
				}
				return result;
			}
			catch (Exception ex) { return new List<QueryResult> { new QueryResult { Exception = ex } }; }
		}

		void ValidateConnection()
		{
			if (dbConnection == null)
				throw new Exception("No connection.");
		}

		DatabaseConnectDialog.Result Command_Database_Connect_Dialog() => DatabaseConnectDialog.Run(WindowParent);

		void Command_Database_Connect(DatabaseConnectDialog.Result result)
		{
			if (dbConnection != null)
			{
				dbConnection.Dispose();
				dbConnection = null;
			}
			dbConnection = result.DBConnectInfo.GetConnection();
			DBName = result.DBConnectInfo.Name;
		}

		void Command_Database_ExecuteQuery()
		{
			ValidateConnection();
			var selections = Selections.ToList();
			if ((Selections.Count == 1) && (!Selections[0].HasSelection))
				selections = new List<Range> { FullRange };
			var strs = GetSelectionStrings();
			// Not in parallel because prior selections may affect later ones
			var results = selections.Select((range, index) => RunDBSelect(strs[index])).ToList();

			for (var ctr = 0; ctr < strs.Count; ++ctr)
			{
				var exception = results[ctr].Select(result => result.Exception).NonNull().FirstOrDefault();
				strs[ctr] += $": {(exception == null ? "Success" : $"{exception.Message}")}";

				foreach (var table in results[ctr].Where(table => table.Table != null))
					OpenTable(table.Table, table.TableName);
			}

			ReplaceSelections(strs);
		}

		string Command_Database_QueryBuilder_Dialog()
		{
			if (Selections.Count != 1)
				throw new Exception("Must run QueryBuilder with one selection");
			ValidateConnection();

			var tables = dbConnection.GetSchema("columns").Rows.OfType<DataRow>().GroupBy(row => row["table_name"]?.ToString()).Where(group => group.Key != null).Select(group => new TableSelect(group.Key, group.Select(row => row["column_name"]?.ToString()).NonNull().ToList())).ToList();
			var selectQuery = QuerySelect.FromStr(GetString(Selections[0]), tables);
			var querySelect = QueryBuilderDialog.Run(WindowParent, tables, selectQuery);
			if (querySelect == null)
				return null;
			return string.Join("", querySelect.QueryLines.Select(str => $"{str}{Data.DefaultEnding}"));
		}

		void Command_Database_QueryBuilder(string result) => ReplaceSelections(result);

		void Command_Database_Examine_Dialog()
		{
			ValidateConnection();
			DatabaseExamineDialog.Run(WindowParent, dbConnection);
		}
	}
}
