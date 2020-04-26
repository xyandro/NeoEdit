using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Transform;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_DateTime_Format_Dialog
	{
		[DepProp]
		public string InputFormat { get { return UIHelper<Configure_DateTime_Format_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_DateTime_Format_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string OutputFormat { get { return UIHelper<Configure_DateTime_Format_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_DateTime_Format_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Example { get { return UIHelper<Configure_DateTime_Format_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_DateTime_Format_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string ParsedExample { get { return UIHelper<Configure_DateTime_Format_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_DateTime_Format_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string OutputExample { get { return UIHelper<Configure_DateTime_Format_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_DateTime_Format_Dialog>.SetPropValue(this, value); } }

		readonly static List<string> formats;

		static Configure_DateTime_Format_Dialog()
		{
			UIHelper<Configure_DateTime_Format_Dialog>.Register();
			UIHelper<Configure_DateTime_Format_Dialog>.AddCallback(a => a.InputFormat, (obj, o, n) => obj.CheckValidInput());
			UIHelper<Configure_DateTime_Format_Dialog>.AddCallback(a => a.OutputFormat, (obj, o, n) => obj.CheckValidInput());

			formats = Dater.GetAllFormats();
		}

		Configure_DateTime_Format_Dialog(string _example)
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

		Configuration_DateTime_Format result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_DateTime_Format { InputFormat = InputFormat, OutputFormat = OutputFormat };
			DialogResult = true;
		}

		public static Configuration_DateTime_Format Run(Window parent, string example)
		{
			var dialog = new Configure_DateTime_Format_Dialog(example) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
