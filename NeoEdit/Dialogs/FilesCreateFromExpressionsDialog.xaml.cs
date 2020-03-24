using System;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Models;
using NeoEdit.Common.Transform;
using NeoEdit.Program;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Dialogs
{
	partial class FilesCreateFromExpressionsDialog
	{
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

		FilesCreateFromExpressionsDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new FilesCreateFromExpressionsDialogResult { FileName = FileName, Data = Data, CodePage = CodePage };
			data.AddCurrentSuggestion();
			fileName.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static FilesCreateFromExpressionsDialogResult Run(Window parent, NEVariables variables, Coder.CodePage codePage)
		{
			var dialog = new FilesCreateFromExpressionsDialog(variables, codePage) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
