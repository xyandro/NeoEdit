using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class LimitDialog
	{
		internal class Result
		{
			public string FirstSel { get; set; }
			public string SelMult { get; set; }
			public string NumSels { get; set; }
			public bool JoinSels { get; set; }
		}

		[DepProp]
		public string FirstSel { get { return UIHelper<LimitDialog>.GetPropValue<string>(this); } set { UIHelper<LimitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string SelMult { get { return UIHelper<LimitDialog>.GetPropValue<string>(this); } set { UIHelper<LimitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string NumSels { get { return UIHelper<LimitDialog>.GetPropValue<string>(this); } set { UIHelper<LimitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool JoinSels { get { return UIHelper<LimitDialog>.GetPropValue<bool>(this); } set { UIHelper<LimitDialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static LimitDialog() { UIHelper<LimitDialog>.Register(); }

		LimitDialog(int maxSels, NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();

			FirstSel = "1";
			SelMult = "1";
			NumSels = maxSels.ToString();
			JoinSels = false;
		}

		Result result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			firstSel.AddCurrentSuggestion();
			selMult.AddCurrentSuggestion();
			numSels.AddCurrentSuggestion();
			result = new Result { FirstSel = FirstSel, SelMult = SelMult, NumSels = NumSels, JoinSels = JoinSels };
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
