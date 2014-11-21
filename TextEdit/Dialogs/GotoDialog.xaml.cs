using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class GotoDialog
	{
		internal class Result
		{
			public int Value { get; set; }
			public bool ClipboardValue { get; set; }
			public bool Relative { get; set; }
		}

		[DepProp]
		public string DisplayString { get { return UIHelper<GotoDialog>.GetPropValue<string>(this); } set { UIHelper<GotoDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int Value { get { return UIHelper<GotoDialog>.GetPropValue<int>(this); } set { UIHelper<GotoDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ClipboardValue { get { return UIHelper<GotoDialog>.GetPropValue<bool>(this); } set { UIHelper<GotoDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Relative { get { return UIHelper<GotoDialog>.GetPropValue<bool>(this); } set { UIHelper<GotoDialog>.SetPropValue(this, value); } }

		static GotoDialog()
		{
			UIHelper<GotoDialog>.Register();
			UIHelper<GotoDialog>.AddCallback(a => a.Relative, (obj, o, n) => obj.SetRelative(o, n));
		}

		readonly int startValue;
		GotoDialog(bool isLine, int _Value)
		{
			InitializeComponent();

			var str = isLine ? "Line" : "Column";
			DisplayString = "_" + str + ":";
			Title = "Go To " + str;
			Value = startValue = _Value;
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
			result = new Result { Value = Value, ClipboardValue = ClipboardValue, Relative = Relative };
			DialogResult = true;
		}

		public static Result Run(bool isLine, int startValue)
		{
			var dialog = new GotoDialog(isLine, startValue);
			return dialog.ShowDialog() == true ? dialog.result : null;
		}
	}
}
