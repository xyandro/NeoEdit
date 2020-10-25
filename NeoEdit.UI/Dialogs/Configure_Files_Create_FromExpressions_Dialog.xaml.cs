using System;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Transform;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Files_Create_FromExpressions_Dialog
	{
		[DepProp]
		public string FileName { get { return UIHelper<Configure_Files_Create_FromExpressions_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Files_Create_FromExpressions_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Data { get { return UIHelper<Configure_Files_Create_FromExpressions_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Files_Create_FromExpressions_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public Coder.CodePage CodePage { get { return UIHelper<Configure_Files_Create_FromExpressions_Dialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<Configure_Files_Create_FromExpressions_Dialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static Configure_Files_Create_FromExpressions_Dialog() { UIHelper<Configure_Files_Create_FromExpressions_Dialog>.Register(); }

		Configure_Files_Create_FromExpressions_Dialog(NEVariables variables, Coder.CodePage defaultCodePage)
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

		Configuration_Files_Create_FromExpressions result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Files_Create_FromExpressions { FileName = FileName, Data = Data, CodePage = CodePage };
			data.AddCurrentSuggestion();
			fileName.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Configuration_Files_Create_FromExpressions Run(Window parent, NEVariables variables, Coder.CodePage codePage)
		{
			var dialog = new Configure_Files_Create_FromExpressions_Dialog(variables, codePage) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
