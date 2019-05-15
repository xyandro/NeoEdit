using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using NeoEdit.Content;
using NeoEdit.Dialogs;
using NeoEdit.Transform;

namespace NeoEdit
{
	partial class TextEditor
	{
		class QueryResult
		{
			public Exception Exception { get; set; }
			public string TableName { get; set; }
			public Table Table { get; set; }
		}

		string dbNameField;
		public string DBName { get => dbNameField; set { dbNameField = value; statusBarRenderTimer.Start(); } }

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

		DatabaseConnectDialog.Result Command_Database_Connect_Dialog() => DatabaseConnectDialog.Run(TabsParent);

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

		void Command_Database_ExecuteQuery(ITextEditor te)
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
					OpenTable(te, table.Table, table.TableName);
			}

			ReplaceSelections(strs);
		}

		void Command_Database_Examine_Dialog()
		{
			ValidateConnection();
			DatabaseExamineDialog.Run(TabsParent, dbConnection);
		}

		void Command_Database_GetSproc()
		{
			ValidateConnection();

			var results = new List<string>();
			foreach (var selection in Selections)
			{
				var sproc = GetString(selection);
				var result = "Success";
				try
				{
					var text = "";
					using (var command = dbConnection.CreateCommand())
					{
						command.CommandText = $"sp_helptext '{sproc}'";
						using (var reader = command.ExecuteReader())
							while (reader.Read())
								text += reader.GetString(0);
					}

					TabsParent.Add(new TextEditor(displayName: sproc, bytes: Coder.StringToBytes(text, Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, contentType: Parser.ParserType.SQL, modified: false));
				}
				catch (Exception ex) { result = ex.Message; }
				results.Add($"{sproc}: {result}");
			}
			ReplaceSelections(results);
		}
	}
}
