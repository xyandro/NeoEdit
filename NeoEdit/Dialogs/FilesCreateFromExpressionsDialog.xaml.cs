using System.Linq;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.Common.Controls;
using NeoEdit.Common.Dialogs;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Transform;

namespace NeoEdit.Dialogs
{
	partial class FilesCreateFromExpressionsDialog
	{
		public class Result
		{
			public string FileName { get; set; }
			public string Data { get; set; }
			public Coder.CodePage CodePage { get; set; }
		}

		[DepProp]
		public string FileName { get { return UIHelper<FilesCreateFromExpressionsDialog>.GetPropValue<string>(this); } set { UIHelper<FilesCreateFromExpressionsDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Data { get { return UIHelper<FilesCreateFromExpressionsDialog>.GetPropValue<string>(this); } set { UIHelper<FilesCreateFromExpressionsDialog>.SetPropValue(this, value); } }
		[DepProp]
		public Coder.CodePage CodePage { get { return UIHelper<FilesCreateFromExpressionsDialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<FilesCreateFromExpressionsDialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static FilesCreateFromExpressionsDialog() { UIHelper<FilesCreateFromExpressionsDialog>.Register(); }

		FilesCreateFromExpressionsDialog(NEVariables variables, Coder.CodePage defaultCodePage)
		{
			Variables = variables;
			InitializeComponent();

			FileName = Data = "x";
			CodePage = defaultCodePage;

			codePage.ItemsSource = Coder.GetAllCodePages().ToDictionary(page => page, page => Coder.GetDescription(page));
			codePage.SelectedValuePath = "Key";
			codePage.DisplayMemberPath = "Value";
		}

		private void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { FileName = FileName, Data = Data, CodePage = CodePage };
			data.AddCurrentSuggestion();
			fileName.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Result Run(Window parent, NEVariables variables, Coder.CodePage codePage)
		{
			var dialog = new FilesCreateFromExpressionsDialog(variables, codePage) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
