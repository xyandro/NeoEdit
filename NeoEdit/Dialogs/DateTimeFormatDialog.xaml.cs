using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program.Dialogs
{
	partial class DateTimeFormatDialog
	{
		public class Result
		{
			public string InputFormat { get; set; }
			public string OutputFormat { get; set; }
		}

		[DepProp]
		public string InputFormat { get { return UIHelper<DateTimeFormatDialog>.GetPropValue<string>(this); } set { UIHelper<DateTimeFormatDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string OutputFormat { get { return UIHelper<DateTimeFormatDialog>.GetPropValue<string>(this); } set { UIHelper<DateTimeFormatDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Example { get { return UIHelper<DateTimeFormatDialog>.GetPropValue<string>(this); } set { UIHelper<DateTimeFormatDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string ParsedExample { get { return UIHelper<DateTimeFormatDialog>.GetPropValue<string>(this); } set { UIHelper<DateTimeFormatDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string OutputExample { get { return UIHelper<DateTimeFormatDialog>.GetPropValue<string>(this); } set { UIHelper<DateTimeFormatDialog>.SetPropValue(this, value); } }

		readonly static List<string> formats;

		static DateTimeFormatDialog()
		{
			UIHelper<DateTimeFormatDialog>.Register();
			UIHelper<DateTimeFormatDialog>.AddCallback(a => a.InputFormat, (obj, o, n) => obj.CheckValidInput());
			UIHelper<DateTimeFormatDialog>.AddCallback(a => a.OutputFormat, (obj, o, n) => obj.CheckValidInput());

			formats = Dater.GetAllFormats();
		}

		DateTimeFormatDialog(string _example)
		{
			InitializeComponent();

			Example = _example;

			foreach (var format in formats)
			{
				inputFormat.Items.Add(format);
				outputFormat.Items.Add(format);
			}

			InputFormat = OutputFormat = Dater.GuessFormat(Example) ?? Dater.RoundTripDateTime;

			if (string.IsNullOrEmpty(InputFormat))
				InputFormat = formats.First();
			OutputFormat = formats.First();
		}

		void CheckValidInput()
		{
			ParsedExample = OutputExample = "";

			var result = Dater.TryStringToDateTimeOffset(Example, InputFormat);
			inputFormat.SetValidation(ComboBox.TextProperty, result != null);
			if (result == null)
				return;

			ParsedExample = result.Value.ToString("O");

			var resultStr = Dater.TryDateTimeOffsetToString(result.Value, OutputFormat);
			outputFormat.SetValidation(ComboBox.TextProperty, result != null);
			if (resultStr == null)
				return;

			OutputExample = resultStr;
		}

		void OnHelp(object sender, RoutedEventArgs e) => DateTimeHelpDialog.Display();

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { InputFormat = InputFormat, OutputFormat = OutputFormat };
			DialogResult = true;
		}

		public static Result Run(Window parent, string example)
		{
			var dialog = new DateTimeFormatDialog(example) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
