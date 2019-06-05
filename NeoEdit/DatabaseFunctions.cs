using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using NeoEdit.Common;
using NeoEdit.Common.Transform;
using NeoEdit.MenuDatabase.Dialogs;

namespace NeoEdit.MenuDatabase
{
	public static class DatabaseFunctions
	{
		class QueryResult
		{
			public Exception Exception { get; set; }
			public string TableName { get; set; }
			public Table Table { get; set; }
		}

		static public void Load() { } // Doesn't do anything except load the assembly

		static string DBSanitize(string name) => (!string.IsNullOrEmpty(name)) && (!char.IsLetter(name[0])) ? $"[{name}]" : name;

		static List<QueryResult> RunDBSelect(ITextEditor te, string commandText)
		{
			try
			{
				var result = new List<QueryResult>();
				var tableName = Regex.Match(commandText, @"\bFROM\b.*?([\[\]a-z\.]+)", RegexOptions.IgnoreCase).Groups[1].Value.Replace("[", "").Replace("]", "").CoalesceNullOrEmpty();
				using (var command = te.dbConnection.CreateCommand())
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

		static void ValidateConnection(ITextEditor te)
		{
			if (te.dbConnection == null)
				throw new Exception("No connection.");
		}

		static public DatabaseConnectDialog.Result Command_Database_Connect_Dialog(ITextEditor te) => DatabaseConnectDialog.Run(te.WindowParent);

		static public void Command_Database_Connect(ITextEditor te, DatabaseConnectDialog.Result result)
		{
			if (te.dbConnection != null)
			{
				te.dbConnection.Dispose();
				te.dbConnection = null;
			}
			te.dbConnection = result.DBConnectInfo.GetConnection();
			te.DBName = result.DBConnectInfo.Name;
		}

		static public void Command_Database_ExecuteQuery(ITextEditor te)
		{
			ValidateConnection(te);
			var selections = te.Selections.ToList();
			if ((te.Selections.Count == 1) && (!te.Selections[0].HasSelection))
				selections = new List<Range> { te.FullRange };
			var strs = te.GetSelectionStrings();
			// Not in parallel because prior selections may affect later ones
			var results = selections.Select((range, index) => RunDBSelect(te, strs[index])).ToList();

			for (var ctr = 0; ctr < strs.Count; ++ctr)
			{
				var exception = results[ctr].Select(result => result.Exception).NonNull().FirstOrDefault();
				strs[ctr] += $": {(exception == null ? "Success" : $"{exception.Message}")}";

				foreach (var table in results[ctr].Where(table => table.Table != null))
					te.OpenTable(table.Table, table.TableName);
			}

			te.ReplaceSelections(strs);
		}

		static public void Command_Database_Examine_Dialog(ITextEditor te)
		{
			ValidateConnection(te);
			DatabaseExamineDialog.Run(te.WindowParent, te.dbConnection);
		}

		static public void Command_Database_GetSproc(ITextEditor te)
		{
			ValidateConnection(te);

			var results = new List<string>();
			foreach (var selection in te.Selections)
			{
				var sproc = te.GetString(selection);
				var result = "Success";
				try
				{
					var text = "";
					using (var command = te.dbConnection.CreateCommand())
					{
						command.CommandText = $"sp_helptext '{sproc}'";
						using (var reader = command.ExecuteReader())
							while (reader.Read())
								text += reader.GetString(0);
					}

					te.TabsParent.Add(displayName: sproc, bytes: Coder.StringToBytes(text, Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, contentType: ParserType.SQL, modified: false);
				}
				catch (Exception ex) { result = ex.Message; }
				results.Add($"{sproc}: {result}");
			}
			te.ReplaceSelections(results);
		}
	}
}
