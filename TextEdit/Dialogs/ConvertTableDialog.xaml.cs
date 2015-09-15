using System;
using System.Linq;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class ConvertTableDialog
	{
		internal class Result
		{
			public TextEditor.TableType InputType { get; set; }
			public TextEditor.TableType OutputType { get; set; }
		}

		[DepProp]
		public TextEditor.TableType InputType { get { return UIHelper<ConvertTableDialog>.GetPropValue<TextEditor.TableType>(this); } set { UIHelper<ConvertTableDialog>.SetPropValue(this, value); } }
		[DepProp]
		public TextEditor.TableType OutputType { get { return UIHelper<ConvertTableDialog>.GetPropValue<TextEditor.TableType>(this); } set { UIHelper<ConvertTableDialog>.SetPropValue(this, value); } }

		static ConvertTableDialog() { UIHelper<ConvertTableDialog>.Register(); }

		ConvertTableDialog(TextEditor.TableType tableType)
		{
			InitializeComponent();

			inputType.ItemsSource = outputType.ItemsSource = Enum.GetValues(typeof(TextEditor.TableType)).Cast<TextEditor.TableType>().ToList();

			InputType = tableType;
			OutputType = InputType == TextEditor.TableType.Columns ? TextEditor.TableType.TSV : TextEditor.TableType.Columns;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { InputType = InputType, OutputType = OutputType };
			DialogResult = true;
		}

		static public Result Run(Window parent, TextEditor.TableType tableType)
		{
			var dialog = new ConvertTableDialog(tableType) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
