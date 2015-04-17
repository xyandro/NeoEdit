using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class LimitDialog
	{
		internal class Result
		{
			public int SelMult { get; set; }
			public bool IgnoreBlank { get; set; }
			public int NumSels { get; set; }
		}

		[DepProp]
		public int SelMult { get { return UIHelper<LimitDialog>.GetPropValue(() => this.SelMult); } set { UIHelper<LimitDialog>.SetPropValue(() => this.SelMult, value); } }
		[DepProp]
		public bool IgnoreBlank { get { return UIHelper<LimitDialog>.GetPropValue(() => this.IgnoreBlank); } set { UIHelper<LimitDialog>.SetPropValue(() => this.IgnoreBlank, value); } }
		[DepProp]
		public int NumSels { get { return UIHelper<LimitDialog>.GetPropValue(() => this.NumSels); } set { UIHelper<LimitDialog>.SetPropValue(() => this.NumSels, value); } }
		[DepProp]
		public int MaxSels { get { return UIHelper<LimitDialog>.GetPropValue(() => this.MaxSels); } set { UIHelper<LimitDialog>.SetPropValue(() => this.MaxSels, value); } }

		static LimitDialog() { UIHelper<LimitDialog>.Register(); }

		LimitDialog(int maxSels)
		{
			InitializeComponent();

			NumSels = MaxSels = maxSels;
			SelMult = 1;
			IgnoreBlank = false;
		}

		Result result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { SelMult = SelMult, IgnoreBlank = IgnoreBlank, NumSels = NumSels };
			DialogResult = true;
		}

		public static Result Run(int numSels)
		{
			var dialog = new LimitDialog(numSels);
			if (dialog.ShowDialog() != true)
				return null;

			return dialog.result;
		}
	}
}
