using System.Linq;
using System.Windows;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class InsertFilesDialog
	{
		internal class Result
		{
			public Coder.CodePage CodePage { get; set; }
		}

		[DepProp]
		public Coder.CodePage CodePage { get { return UIHelper<InsertFilesDialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<InsertFilesDialog>.SetPropValue(this, value); } }

		static InsertFilesDialog() { UIHelper<InsertFilesDialog>.Register(); }

		InsertFilesDialog()
		{
			InitializeComponent();

			CodePage = Coder.CodePage.AutoByBOM;

			codePage.ItemsSource = Coder.GetAllCodePages().ToDictionary(page => page, page => Coder.GetDescription(page));
			codePage.SelectedValuePath = "Key";
			codePage.DisplayMemberPath = "Value";
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { CodePage = CodePage };
			DialogResult = true;
		}

		public static Result Run(Window parent)
		{
			var dialog = new InsertFilesDialog() { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
