using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using NeoEdit.Common;
using NeoEdit.Common.Parsing;
using NeoEdit.GUI.Dialogs;
using NeoEdit.TextEdit.Dialogs;

namespace NeoEdit.TextEdit
{
	partial class TextEditor
	{
		DbConnection dbConnection;

		string DBSanitize(string name) => (!string.IsNullOrEmpty(name)) && (!char.IsLetter(name[0])) ? $"[{name}]" : name;

		Tuple<string, Table> RunDBSelect(string commandText)
		{
			var tableName = Regex.Match(commandText, @"\bFROM\b.*?([\[\]a-z\.]+)", RegexOptions.IgnoreCase).Groups[1].Value.Replace("[", "").Replace("]", "").CoalesceNullOrEmpty();
			using (var command = dbConnection.CreateCommand())
			{
				command.CommandText = commandText;
				using (var reader = command.ExecuteReader())
				{
					if (reader.FieldCount == 0)
						return null;
					return Tuple.Create(tableName, new Table(reader));
				}
			}
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
		}

		void Command_Database_ExecuteQuery()
		{
			ValidateConnection();
			var selections = Selections.ToList();
			if ((Selections.Count == 1) && (!Selections[0].HasSelection))
				selections = new List<Range> { FullRange };
			var tables = selections.Select(range => RunDBSelect(GetString(range))).ToList();
			if (!tables.NonNull().Any())
			{
				Message.Show($"Quer{(selections.Count == 1 ? "y" : "ies")} run successfully.");
				return;
			}

			if (!UseCurrentWindow)
			{
				foreach (var table in tables)
					OpenTable(table.Item2, table.Item1);
				return;
			}

			ReplaceSelections(tables.Select(table => table == null ? "Success" : GetTableText(table.Item2)).ToList());
		}

		void Command_Database_UseCurrentWindow(bool? multiStatus) => UseCurrentWindow = multiStatus != true;

		string Command_Database_QueryTable_Dialog()
		{
			ValidateConnection();
			var tableSchema = dbConnection.GetSchema("Tables");
			var tableCatalogColumn = tableSchema.Columns["table_catalog"];
			var tableSchemaColumn = tableSchema.Columns["table_schema"];
			var tableNameColumn = tableSchema.Columns["table_name"];
			List<string> tables;
			if (dbConnection is MySql.Data.MySqlClient.MySqlConnection)
				tables = tableSchema.Rows.Cast<DataRow>().Select(row => $"{DBSanitize(row[tableSchemaColumn]?.ToString())}.{DBSanitize(row[tableNameColumn]?.ToString())}").ToList();
			else
				tables = tableSchema.Rows.Cast<DataRow>().Select(row => $"{DBSanitize(row[tableCatalogColumn]?.ToString())}.{DBSanitize(row[tableSchemaColumn]?.ToString())}.{DBSanitize(row[tableNameColumn]?.ToString())}").ToList();

			return QueryTableDialog.Run(WindowParent, tables);
		}

		void Command_Database_QueryTable(string result) => ReplaceSelections(result);

		void Command_Database_Examine_Dialog()
		{
			ValidateConnection();
			ExamineDatabaseDialog.Run(WindowParent, dbConnection);
		}
	}
}
