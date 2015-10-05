using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TableEdit.Dialogs
{
	internal partial class EditHeaderDialog
	{
		internal class Result
		{
			public string Name { get; set; }
		}

		[DepProp]
		public string ColumnName { get { return UIHelper<EditHeaderDialog>.GetPropValue<string>(this); } set { UIHelper<EditHeaderDialog>.SetPropValue(this, value); } }

		static EditHeaderDialog() { UIHelper<EditHeaderDialog>.Register(); }

		EditHeaderDialog(string name)
		{
			InitializeComponent();
			ColumnName = name;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result
			{
				Name = ColumnName,
			};
			DialogResult = true;
		}

		static public Result Run(Window parent, string name)
		{
			var dialog = new EditHeaderDialog(name) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
