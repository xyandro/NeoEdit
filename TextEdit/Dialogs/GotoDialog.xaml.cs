using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class GotoDialog
	{
		internal class Result
		{
			public GotoType GotoType { get; set; }
			public int Value { get; set; }
			public bool ClipboardValue { get; set; }
			public bool Relative { get; set; }
		}

		internal enum GotoType
		{
			Line,
			Column,
			Position,
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

		readonly GotoType gotoType;
		readonly int startValue;
		GotoDialog(GotoType gotoType, int _Value)
		{
			InitializeComponent();

			this.gotoType = gotoType;
			var str = gotoType.ToString();
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
			result = new Result { GotoType = gotoType, Value = Value, ClipboardValue = ClipboardValue, Relative = Relative };
			DialogResult = true;
		}

		public static Result Run(GotoType gotoType, int startValue)
		{
			var dialog = new GotoDialog(gotoType, startValue);
			return dialog.ShowDialog() == true ? dialog.result : null;
		}
	}
}
