using System;
using System.Collections.Generic;
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

			var chars = new List<char>();
			var matches = Regex.Matches(Regex.Unescape(Chars), "(.)-(.)|(.)", RegexOptions.Singleline);
			foreach (Match match in matches)
			{
				if (match.Groups[1].Success)
				{
					var v0 = match.Groups[1].Value[0];
					var v1 = match.Groups[2].Value[0];
					var start = (char)Math.Min(v0, v1);
					var end = (char)Math.Max(v0, v1);
					for (var c = start; c <= end; ++c)
						chars.Add(c);
				}
				else if (match.Groups[3].Success)
					chars.Add(match.Groups[3].Value[0]);
			}

			if (!chars.Any())
				return;

			result = new Result { Type = Type, Value = Value, Chars = chars.ToArray() };
			DialogResult = true;
		}

		public static Result Run(bool hasSelections)
		{
			var dialog = new RandomDataDialog(hasSelections);
			return dialog.ShowDialog() == true ? dialog.result : null;
		}
	}
}
