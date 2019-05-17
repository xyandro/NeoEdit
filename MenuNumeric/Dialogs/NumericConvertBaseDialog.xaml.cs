using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit;
using NeoEdit.Controls;

namespace NeoEdit.Dialogs
{
	partial class NumericConvertBaseDialog
	{
		public class Result
		{
			public Dictionary<char, int> InputSet { get; set; }
			public Dictionary<int, char> OutputSet { get; set; }
		}

		[DepProp]
		public int? InputBase { get { return UIHelper<NumericConvertBaseDialog>.GetPropValue<int?>(this); } set { UIHelper<NumericConvertBaseDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string InputSet { get { return UIHelper<NumericConvertBaseDialog>.GetPropValue<string>(this); } set { UIHelper<NumericConvertBaseDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int? OutputBase { get { return UIHelper<NumericConvertBaseDialog>.GetPropValue<int?>(this); } set { UIHelper<NumericConvertBaseDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string OutputSet { get { return UIHelper<NumericConvertBaseDialog>.GetPropValue<string>(this); } set { UIHelper<NumericConvertBaseDialog>.SetPropValue(this, value); } }

		static NumericConvertBaseDialog()
		{
			UIHelper<NumericConvertBaseDialog>.Register();
			UIHelper<NumericConvertBaseDialog>.AddCallback(a => a.InputBase, (obj, o, n) => obj.Update(nameof(InputBase)));
			UIHelper<NumericConvertBaseDialog>.AddCallback(a => a.InputSet, (obj, o, n) => obj.Update(nameof(InputSet)));
			UIHelper<NumericConvertBaseDialog>.AddCallback(a => a.OutputBase, (obj, o, n) => obj.Update(nameof(OutputBase)));
			UIHelper<NumericConvertBaseDialog>.AddCallback(a => a.OutputSet, (obj, o, n) => obj.Update(nameof(OutputSet)));
		}

		NumericConvertBaseDialog()
		{
			InitializeComponent();
			InputBase = 10;
			OutputBase = 16;
		}

		bool updating = false;
		void Update(string property)
		{
			if (updating)
				return;

			updating = true;
			switch (property)
			{
				case nameof(InputBase): InputSet = CharsFromBase(InputBase); break;
				case nameof(InputSet): InputBase = BaseFromChars(InputSet); break;
				case nameof(OutputBase): OutputSet = CharsFromBase(OutputBase); break;
				case nameof(OutputSet): OutputBase = BaseFromChars(OutputSet); break;
			}
			updating = false;
		}

		int? BaseFromChars(string chars) { try { return Helpers.GetCharsFromCharString(chars).Length; } catch { return null; } }

		string GetChars(ref int count, char start, char end)
		{
			if (count <= 0)
				return "";
			var use = Math.Min(count, end - start + 1);
			count -= use;
			if (use == 1)
				return $"{start}";
			return $"{start}-{(char)(start + use - 1)}";
		}

		string CharsFromBase(int? baseValue)
		{
			if (!baseValue.HasValue)
				return "";

			var value = baseValue.Value;
			if (value == 64)
				return "A-Za-z0-9+/";

			var result = "";
			result += GetChars(ref value, '0', '9');
			result += GetChars(ref value, 'a', 'z');
			result += GetChars(ref value, 'A', 'Z');

			if (value != 0)
			{
				MessageBox.Show(this, "Please specify characters to use for this base.");
				return "";
			}

			return result;
		}


		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			var inputChars = Helpers.GetCharsFromCharString(InputSet);
			var outputChars = Helpers.GetCharsFromCharString(OutputSet);
			if (((inputChars.GroupBy(ch => ch).Any(group => group.Count() > 1))) || (outputChars.GroupBy(ch => ch).Any(group => group.Count() > 1)))
				throw new ArgumentException("Can't have same number more than once");

			result = new Result
			{
				InputSet = inputChars.Select((ch, index) => new { ch, index }).ToDictionary(pair => pair.ch, pair => pair.index),
				OutputSet = outputChars.Select((ch, index) => new { ch, index }).ToDictionary(pair => pair.index, pair => pair.ch),
			};

			inputSet.AddCurrentSuggestion();
			outputSet.AddCurrentSuggestion();
			inputBase.AddCurrentSuggestion();
			outputBase.AddCurrentSuggestion();

			DialogResult = true;
		}

		static public Result Run(Window parent)
		{
			var dialog = new NumericConvertBaseDialog { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
