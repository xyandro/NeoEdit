using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class LimitDialog
	{
		internal class Result
		{
			public string SelMult { get; set; }
			public string NumSels { get; set; }
			public bool IgnoreBlank { get; set; }
		}

		[DepProp]
		public string SelMult { get { return UIHelper<LimitDialog>.GetPropValue<string>(this); } set { UIHelper<LimitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string NumSels { get { return UIHelper<LimitDialog>.GetPropValue<string>(this); } set { UIHelper<LimitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IgnoreBlank { get { return UIHelper<LimitDialog>.GetPropValue<bool>(this); } set { UIHelper<LimitDialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static LimitDialog() { UIHelper<LimitDialog>.Register(); }

		LimitDialog(int maxSels, NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();

			SelMult = "1";
			NumSels = maxSels.ToString();
			IgnoreBlank = false;
		}

		Result result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { SelMult = SelMult, NumSels = NumSels, IgnoreBlank = IgnoreBlank };
			DialogResult = true;
		}

		public static Result Run(Window parent, int numSels, NEVariables variables)
		{
			var dialog = new LimitDialog(numSels, variables) { Owner = parent };
			if (!dialog.ShowDialog())
				return null;

			return dialog.result;
		}
	}
}
