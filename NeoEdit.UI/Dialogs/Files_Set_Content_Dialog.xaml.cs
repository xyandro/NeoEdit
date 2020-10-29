using System;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Transform;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Files_Set_Content_Dialog
	{
		[DepProp]
		public string FileName { get { return UIHelper<Files_Set_Content_Dialog>.GetPropValue<string>(this); } set { UIHelper<Files_Set_Content_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Data { get { return UIHelper<Files_Set_Content_Dialog>.GetPropValue<string>(this); } set { UIHelper<Files_Set_Content_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public Coder.CodePage CodePage { get { return UIHelper<Files_Set_Content_Dialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<Files_Set_Content_Dialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static Files_Set_Content_Dialog() { UIHelper<Files_Set_Content_Dialog>.Register(); }

		Files_Set_Content_Dialog(NEVariables variables, Coder.CodePage defaultCodePage)
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

		Configuration_Files_Set_Content result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Files_Set_Content { FileName = FileName, Data = Data, CodePage = CodePage };
			data.AddCurrentSuggestion();
			fileName.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Configuration_Files_Set_Content Run(Window parent, NEVariables variables, Coder.CodePage codePage)
		{
			var dialog = new Files_Set_Content_Dialog(variables, codePage) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
