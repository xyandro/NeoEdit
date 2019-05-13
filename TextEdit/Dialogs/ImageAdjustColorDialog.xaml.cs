using System.Windows;
using NeoEdit.TextEdit.Expressions;
using NeoEdit.TextEdit.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	partial class ImageAdjustColorDialog
	{
		public class Result
		{
			public string Expression { get; set; }
			public bool Alpha { get; set; }
			public bool Red { get; set; }
			public bool Green { get; set; }
			public bool Blue { get; set; }
		}

		[DepProp]
		public string Expression { get { return UIHelper<ImageAdjustColorDialog>.GetPropValue<string>(this); } set { UIHelper<ImageAdjustColorDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Alpha { get { return UIHelper<ImageAdjustColorDialog>.GetPropValue<bool>(this); } set { UIHelper<ImageAdjustColorDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Red { get { return UIHelper<ImageAdjustColorDialog>.GetPropValue<bool>(this); } set { UIHelper<ImageAdjustColorDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Green { get { return UIHelper<ImageAdjustColorDialog>.GetPropValue<bool>(this); } set { UIHelper<ImageAdjustColorDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Blue { get { return UIHelper<ImageAdjustColorDialog>.GetPropValue<bool>(this); } set { UIHelper<ImageAdjustColorDialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static ImageAdjustColorDialog() { UIHelper<ImageAdjustColorDialog>.Register(); }

		ImageAdjustColorDialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();
			Expression = "1";
			Red = Green = Blue = true;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			expression.AddCurrentSuggestion();
			result = new Result { Expression = Expression, Alpha = Alpha, Red = Red, Green = Green, Blue = Blue };
			DialogResult = true;
		}

		static public Result Run(Window parent, NEVariables variables)
		{
			var dialog = new ImageAdjustColorDialog(variables) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
