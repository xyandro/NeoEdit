using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class ConvertBaseDialog
	{
		internal class Result
		{
			public Dictionary<char, int> InputSet { get; set; }
			public Dictionary<int, char> OutputSet { get; set; }
		}

		[DepProp]
		public string InputSet { get { return UIHelper<ConvertBaseDialog>.GetPropValue<string>(this); } set { UIHelper<ConvertBaseDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string OutputSet { get { return UIHelper<ConvertBaseDialog>.GetPropValue<string>(this); } set { UIHelper<ConvertBaseDialog>.SetPropValue(this, value); } }

		static ConvertBaseDialog() { UIHelper<ConvertBaseDialog>.Register(); }

		ConvertBaseDialog()
		{
			InitializeComponent();
			InputSet = "0-9";
			OutputSet = "0-9a-f";
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			var inputSet = Misc.GetCharsFromRegexString(InputSet);
			var outputSet = Misc.GetCharsFromRegexString(OutputSet);
			if (((inputSet.GroupBy(ch => ch).Any(group => group.Count() > 1))) || (outputSet.GroupBy(ch => ch).Any(group => group.Count() > 1)))
				throw new ArgumentException("Can't have same number more than once");
			result = new Result
			{
				InputSet = inputSet.Select((ch, index) => new { ch, index }).ToDictionary(pair => pair.ch, pair => pair.index),
				OutputSet = outputSet.Select((ch, index) => new { ch, index }).ToDictionary(pair => pair.index, pair => pair.ch),
			};
			DialogResult = true;
		}

		static public Result Run(Window parent)
		{
			var dialog = new ConvertBaseDialog { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
