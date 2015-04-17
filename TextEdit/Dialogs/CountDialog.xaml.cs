using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class CountDialog
	{
		internal class Result
		{
			public int MinCount { get; set; }
			public int MaxCount { get; set; }
			public bool CaseSensitive { get; set; }
		}

		[DepProp]
		public int MinCount { get { return UIHelper<CountDialog>.GetPropValue(() => this.MinCount); } set { UIHelper<CountDialog>.SetPropValue(() => this.MinCount, value); } }
		[DepProp]
		public int MaxCount { get { return UIHelper<CountDialog>.GetPropValue(() => this.MaxCount); } set { UIHelper<CountDialog>.SetPropValue(() => this.MaxCount, value); } }
		[DepProp]
		public bool CaseSensitive { get { return UIHelper<CountDialog>.GetPropValue(() => this.CaseSensitive); } set { UIHelper<CountDialog>.SetPropValue(() => this.CaseSensitive, value); } }

		static CountDialog() { UIHelper<CountDialog>.Register(); }

		CountDialog()
		{
			InitializeComponent();
			MinCount = MaxCount = 2;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (MaxCount < MinCount)
				return;
			result = new Result { MinCount = MinCount, MaxCount = MaxCount, CaseSensitive = CaseSensitive };
			DialogResult = true;
		}

		public static Result Run(Window parent)
		{
			var dialog = new CountDialog { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
