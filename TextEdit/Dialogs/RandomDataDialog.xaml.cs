using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class RandomDataDialog
	{
		public enum RandomDataType
		{
			Absolute,
			Clipboard,
			SelectionLength,
		}

		internal class Result
		{
			public RandomDataType Type { get; set; }
			public int Value { get; set; }
			public char[] Chars { get; set; }
		}

		[DepProp]
		public RandomDataType Type { get { return UIHelper<RandomDataDialog>.GetPropValue<RandomDataType>(this); } set { UIHelper<RandomDataDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int Value { get { return UIHelper<RandomDataDialog>.GetPropValue<int>(this); } set { UIHelper<RandomDataDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Chars { get { return UIHelper<RandomDataDialog>.GetPropValue<string>(this); } set { UIHelper<RandomDataDialog>.SetPropValue(this, value); } }

		static RandomDataDialog()
		{
			UIHelper<RandomDataDialog>.Register();
			UIHelper<RandomDataDialog>.AddCallback(a => a.Value, (obj, o, n) => obj.Type = RandomDataType.Absolute);
		}

		RandomDataDialog(bool hasSelections)
		{
			InitializeComponent();

			Type = hasSelections ? RandomDataType.SelectionLength : RandomDataType.Absolute;
			Chars = "a-zA-Z";
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (Chars.Length == 0)
				return;

			var regex = new Regex("([" + Chars.Replace("[", @"\[").Replace("]", @"\]") + "])");
			var allChars = new string(Enumerable.Range(0, 65536).Select(ch => (char)ch).ToArray());
			var chars = regex.Matches(allChars).Cast<Match>().Select(match => match.Value[0]).ToArray();
			if (!chars.Any())
				return;

			result = new Result { Type = Type, Value = Value, Chars = chars };
			DialogResult = true;
		}

		public static Result Run(bool hasSelections)
		{
			var dialog = new RandomDataDialog(hasSelections);
			return dialog.ShowDialog() == true ? dialog.result : null;
		}
	}
}
