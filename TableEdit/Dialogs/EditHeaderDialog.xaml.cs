using System;
using System.Collections.Generic;
using System.Windows;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.TableEdit.Dialogs
{
	internal partial class EditHeaderDialog
	{
		internal class Result
		{
			public Table.Header Header { get; set; }
			public string Expression { get; set; }
		}

		[DepProp]
		public string ColumnName { get { return UIHelper<EditHeaderDialog>.GetPropValue<string>(this); } set { UIHelper<EditHeaderDialog>.SetPropValue(this, value); } }
		[DepProp]
		public Type Type { get { return UIHelper<EditHeaderDialog>.GetPropValue<Type>(this); } set { UIHelper<EditHeaderDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Nullable { get { return UIHelper<EditHeaderDialog>.GetPropValue<bool>(this); } set { UIHelper<EditHeaderDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Expression { get { return UIHelper<EditHeaderDialog>.GetPropValue<string>(this); } set { UIHelper<EditHeaderDialog>.SetPropValue(this, value); } }

		static EditHeaderDialog() { UIHelper<EditHeaderDialog>.Register(); }

		readonly Dictionary<string, List<object>> examples;
		EditHeaderDialog(Table.Header header, Dictionary<string, List<object>> examples)
		{
			this.examples = examples;
			InitializeComponent();

			type.ItemsSource = new Dictionary<Type, string>
			{
				{ typeof(Boolean), "Boolean" },
				{ typeof(SByte), "Int8" },
				{ typeof(Int16), "Int16" },
				{ typeof(Int32), "Int32" },
				{ typeof(Int64), "Int64" },
				{ typeof(Byte), "UInt8" },
				{ typeof(UInt16), "UInt16" },
				{ typeof(UInt32), "UInt32" },
				{ typeof(UInt64), "UInt64" },
				{ typeof(Single), "Single" },
				{ typeof(Double), "Double" },
				{ typeof(DateTime), "DateTime" },
				{ typeof(String), "String" },
			};
			type.SelectedValuePath = "Key";
			type.DisplayMemberPath = "Value";

			ColumnName = header.Name;
			Type = header.Type;
			Nullable = header.Nullable;
			Expression = null;
		}

		void EditExpression(object sender, RoutedEventArgs e)
		{
			var result = GetExpressionDialog.Run(Owner, examples);
			if (result != null)
				Expression = result.Expression;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result
			{
				Header = new Table.Header { Name = ColumnName, Type = Type, Nullable = Nullable },
				Expression = Expression,
			};
			DialogResult = true;
		}

		static public Result Run(Window parent, Table.Header header, Dictionary<string, List<object>> examples)
		{
			var dialog = new EditHeaderDialog(header, examples) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
