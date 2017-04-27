using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class ScaleDialog
	{
		internal class Result
		{
			public string PrevMin { get; set; }
			public string PrevMax { get; set; }
			public string NewMin { get; set; }
			public string NewMax { get; set; }
		}

		[DepProp]
		public string PrevMin { get { return UIHelper<ScaleDialog>.GetPropValue<string>(this); } set { UIHelper<ScaleDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string PrevMax { get { return UIHelper<ScaleDialog>.GetPropValue<string>(this); } set { UIHelper<ScaleDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string NewMin { get { return UIHelper<ScaleDialog>.GetPropValue<string>(this); } set { UIHelper<ScaleDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string NewMax { get { return UIHelper<ScaleDialog>.GetPropValue<string>(this); } set { UIHelper<ScaleDialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static ScaleDialog() { UIHelper<ScaleDialog>.Register(); }

		ScaleDialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();
			PrevMin = NewMin = "xmin";
			PrevMax = NewMax = "xmax";
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { PrevMin = PrevMin, PrevMax = PrevMax, NewMin = NewMin, NewMax = NewMax };
			DialogResult = true;
		}

		static public Result Run(Window parent, NEVariables variables)
		{
			var dialog = new ScaleDialog(variables) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
