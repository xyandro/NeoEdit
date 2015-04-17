using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.HexEdit.Dialogs
{
	internal partial class GotoDialog
	{
		internal class Result
		{
			public long Value { get; set; }
			public bool Relative { get; set; }
		}

		[DepProp]
		public long Value { get { return UIHelper<GotoDialog>.GetPropValue(() => this.Value); } set { UIHelper<GotoDialog>.SetPropValue(() => this.Value, value); } }
		[DepProp]
		public bool Relative { get { return UIHelper<GotoDialog>.GetPropValue(() => this.Relative); } set { UIHelper<GotoDialog>.SetPropValue(() => this.Relative, value); } }

		static GotoDialog()
		{
			UIHelper<GotoDialog>.Register();
			UIHelper<GotoDialog>.AddCallback(a => a.Relative, (obj, o, n) => obj.SetRelative(o, n));
		}

		readonly long startValue;
		GotoDialog(long value)
		{
			InitializeComponent();
			Value = startValue = value;
		}

		void SetRelative(bool oldValue, bool newValue)
		{
			if (oldValue == newValue)
				return;

			if (newValue)
				Value -= startValue;
			else
				Value += startValue;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { Value = Value, Relative = Relative };
			DialogResult = true;
		}

		public static Result Run(Window parent, long value)
		{
			var dialog = new GotoDialog(value) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
