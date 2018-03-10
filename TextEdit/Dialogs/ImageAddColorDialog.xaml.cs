using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	partial class ImageAddColorDialog
	{
		public class Result
		{
			public string Expression { get; set; }
		}

		[DepProp]
		public string Expression { get { return UIHelper<ImageAddColorDialog>.GetPropValue<string>(this); } set { UIHelper<ImageAddColorDialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static ImageAddColorDialog() { UIHelper<ImageAddColorDialog>.Register(); }

		ImageAddColorDialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();
			Expression = "c";
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			expression.AddCurrentSuggestion();
			result = new Result { Expression = Expression };
			DialogResult = true;
		}

		static public Result Run(Window parent, NEVariables variables)
		{
			var dialog = new ImageAddColorDialog(variables) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
