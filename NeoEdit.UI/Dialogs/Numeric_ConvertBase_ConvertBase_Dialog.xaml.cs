using System;
using System.Linq;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Numeric_ConvertBase_ConvertBase_Dialog
	{
		[DepProp]
		public int? InputBase { get { return UIHelper<Numeric_ConvertBase_ConvertBase_Dialog>.GetPropValue<int?>(this); } set { UIHelper<Numeric_ConvertBase_ConvertBase_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string InputSet { get { return UIHelper<Numeric_ConvertBase_ConvertBase_Dialog>.GetPropValue<string>(this); } set { UIHelper<Numeric_ConvertBase_ConvertBase_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public int? OutputBase { get { return UIHelper<Numeric_ConvertBase_ConvertBase_Dialog>.GetPropValue<int?>(this); } set { UIHelper<Numeric_ConvertBase_ConvertBase_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string OutputSet { get { return UIHelper<Numeric_ConvertBase_ConvertBase_Dialog>.GetPropValue<string>(this); } set { UIHelper<Numeric_ConvertBase_ConvertBase_Dialog>.SetPropValue(this, value); } }

		static Numeric_ConvertBase_ConvertBase_Dialog()
		{
			UIHelper<Numeric_ConvertBase_ConvertBase_Dialog>.Register();
			UIHelper<Numeric_ConvertBase_ConvertBase_Dialog>.AddCallback(a => a.InputBase, (obj, o, n) => obj.Update(nameof(InputBase)));
			UIHelper<Numeric_ConvertBase_ConvertBase_Dialog>.AddCallback(a => a.InputSet, (obj, o, n) => obj.Update(nameof(InputSet)));
			UIHelper<Numeric_ConvertBase_ConvertBase_Dialog>.AddCallback(a => a.OutputBase, (obj, o, n) => obj.Update(nameof(OutputBase)));
			UIHelper<Numeric_ConvertBase_ConvertBase_Dialog>.AddCallback(a => a.OutputSet, (obj, o, n) => obj.Update(nameof(OutputSet)));
		}

		Numeric_ConvertBase_ConvertBase_Dialog()
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

		Configuration_Numeric_ConvertBase_ConvertBase result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			var inputChars = Helpers.GetCharsFromCharString(InputSet);
			var outputChars = Helpers.GetCharsFromCharString(OutputSet);
			if (((inputChars.GroupBy(ch => ch).Any(group => group.Count() > 1))) || (outputChars.GroupBy(ch => ch).Any(group => group.Count() > 1)))
				throw new ArgumentException("Can't have same number more than once");

			result = new Configuration_Numeric_ConvertBase_ConvertBase
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

		public static Configuration_Numeric_ConvertBase_ConvertBase Run(Window parent)
		{
			var dialog = new Numeric_ConvertBase_ConvertBase_Dialog { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
