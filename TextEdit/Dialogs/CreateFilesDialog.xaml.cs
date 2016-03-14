using System.Linq;
using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class CreateFilesDialog
	{
		internal class Result
		{
			public string Data { get; set; }
			public string FileName { get; set; }
			public Coder.CodePage CodePage { get; set; }
		}

		[DepProp]
		public string Data { get { return UIHelper<CreateFilesDialog>.GetPropValue<string>(this); } set { UIHelper<CreateFilesDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string FileName { get { return UIHelper<CreateFilesDialog>.GetPropValue<string>(this); } set { UIHelper<CreateFilesDialog>.SetPropValue(this, value); } }
		[DepProp]
		public Coder.CodePage CodePage { get { return UIHelper<CreateFilesDialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<CreateFilesDialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static CreateFilesDialog() { UIHelper<CreateFilesDialog>.Register(); }

		CreateFilesDialog(NEVariables variables, Coder.CodePage defaultCodePage)
		{
			Variables = variables;
			InitializeComponent();

			Data = FileName = "x";
			CodePage = defaultCodePage;

			codePage.ItemsSource = Coder.GetStringCodePages().ToDictionary(page => page, page => Coder.GetDescription(page));
			codePage.SelectedValuePath = "Key";
			codePage.DisplayMemberPath = "Value";
		}

		private void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display();

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { Data = Data, FileName = FileName, CodePage = CodePage };
			data.AddCurrentSuggestion();
			fileName.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Result Run(Window parent, NEVariables variables, Coder.CodePage codePage)
		{
			var dialog = new CreateFilesDialog(variables, codePage) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
