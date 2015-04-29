using System.Collections.Generic;
using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class SetSizeDialog
	{
		public enum SizeType
		{
			None,
			Absolute,
			Relative,
			Minimum,
			Maximum,
			Multiple,
			Clipboard,
		}

		internal class Result
		{
			public SizeType Type { get; set; }
			public long Value { get; set; }
		}

		[DepProp]
		public SizeType Type { get { return UIHelper<SetSizeDialog>.GetPropValue(() => this.Type); } set { UIHelper<SetSizeDialog>.SetPropValue(() => this.Type, value); } }
		[DepProp]
		public long Value { get { return UIHelper<SetSizeDialog>.GetPropValue(() => this.Value); } set { UIHelper<SetSizeDialog>.SetPropValue(() => this.Value, value); } }
		[DepProp]
		public long ByteMult { get { return UIHelper<SetSizeDialog>.GetPropValue(() => this.ByteMult); } set { UIHelper<SetSizeDialog>.SetPropValue(() => this.ByteMult, value); } }
		[DepProp]
		public Dictionary<string, long> ByteMultDict { get { return UIHelper<SetSizeDialog>.GetPropValue(() => this.ByteMultDict); } set { UIHelper<SetSizeDialog>.SetPropValue(() => this.ByteMultDict, value); } }

		static SetSizeDialog() { UIHelper<SetSizeDialog>.Register(); }

		SetSizeDialog()
		{
			InitializeComponent();
			Type = SizeType.Absolute;
			ByteMultDict = new Dictionary<string, long> 
			{
				{ "GB", 1 << 30 },
				{ "MB", 1 << 20 },
				{ "KB", 1 << 10 },
				{ "bytes", 1 << 0 },
			};
			ByteMult = 1;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { Type = Type, Value = Value * ByteMult };
			DialogResult = true;
		}

		public static Result Run(Window parent)
		{
			var dialog = new SetSizeDialog() { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
