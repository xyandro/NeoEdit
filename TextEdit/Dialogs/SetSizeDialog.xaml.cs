using System.Collections.Generic;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class SetSizeDialog
	{
		public enum SizeType
		{
			Absolute,
			Relative,
			Minimum,
			Maximum,
			Multiple,
		}

		internal class Result
		{
			public SizeType Type { get; set; }
			public string Expression { get; set; }
			public long Factor { get; set; }
		}

		[DepProp]
		public SizeType Type { get { return UIHelper<SetSizeDialog>.GetPropValue<SizeType>(this); } set { UIHelper<SetSizeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Expression { get { return UIHelper<SetSizeDialog>.GetPropValue<string>(this); } set { UIHelper<SetSizeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public long Factor { get { return UIHelper<SetSizeDialog>.GetPropValue<long>(this); } set { UIHelper<SetSizeDialog>.SetPropValue(this, value); } }
		public Dictionary<string, long> FactorDict { get; private set; }
		public Dictionary<string, List<object>> ExpressionData { get; private set; }

		static SetSizeDialog() { UIHelper<SetSizeDialog>.Register(); }

		SetSizeDialog(Dictionary<string, List<object>> expressionData)
		{
			FactorDict = new Dictionary<string, long>
			{
				{ "GB", 1 << 30 },
				{ "MB", 1 << 20 },
				{ "KB", 1 << 10 },
				{ "bytes", 1 << 0 },
			};
			ExpressionData = expressionData;
			InitializeComponent();
			Type = SizeType.Absolute;
			Factor = 1;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { Type = Type, Expression = Expression, Factor = Factor };
			DialogResult = true;
		}

		public static Result Run(Window parent, Dictionary<string, List<object>> expressionData)
		{
			var dialog = new SetSizeDialog(expressionData) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
