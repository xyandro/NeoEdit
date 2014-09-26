using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEditor.Dialogs
{
	public partial class ConvertTime : Window
	{
		[DepProp]
		public string InputFormat { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool InputUTC { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public string OutputFormat { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool OutputUTC { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public string Example { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public string ParsedExample { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public string OutputExample { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }

		static ConvertTime() { UIHelper<ConvertTime>.Register(); }

		const string Unix = "Unix";
		const string FileTime = "FileTime";
		const string Excel = "Excel";
		static List<string> formats = new List<string>
		{
			"O",
			Unix,
			FileTime,
			Excel,
			"d", "D", "f", "F", "g", "G", "M", "R", "s", "t", "T", "u", "U", "Y",
		};

		readonly UIHelper<ConvertTime> uiHelper;
		ConvertTime(string _example)
		{
			uiHelper = new UIHelper<ConvertTime>(this);
			InitializeComponent();

			Example = _example;

			uiHelper.AddCallback(a => a.InputFormat, (s, e) => CheckValidInput());
			uiHelper.AddCallback(a => a.InputUTC, (s, e) => { CheckValidInput(); OutputUTC = InputUTC; });
			uiHelper.AddCallback(a => a.OutputFormat, (s, e) => CheckValidInput());
			uiHelper.AddCallback(a => a.OutputUTC, (s, e) => CheckValidInput());

			foreach (var format in formats)
			{
				inputFormat.Items.Add(format);
				outputFormat.Items.Add(format);

				var value = InterpretFormat(Example, format, InputUTC, false);
				if (value != null)
				{
					InputFormat = format;
					InputUTC = OutputUTC = value.Value.Kind == DateTimeKind.Utc;
				}
			}

			if (String.IsNullOrEmpty(InputFormat))
				InputFormat = formats.First();
			OutputFormat = formats.First();
		}

		void CheckValidInput()
		{
			ParsedExample = OutputExample = "";

			var bindingExpressionBase = inputFormat.GetBindingExpression(ComboBox.TextProperty);
			var result = InterpretFormat(Example, InputFormat, InputUTC);
			if (result == null)
			{
				Validation.MarkInvalid(bindingExpressionBase, new ValidationError(new ExceptionValidationRule(), bindingExpressionBase));
				return;
			}

			Validation.ClearInvalid(bindingExpressionBase);
			ParsedExample = result.Value.ToString("O");

			bindingExpressionBase = outputFormat.GetBindingExpression(ComboBox.TextProperty);
			var resultStr = InterpretFormat(result.Value, OutputFormat, OutputUTC);
			if (resultStr == null)
			{
				Validation.MarkInvalid(bindingExpressionBase, new ValidationError(new ExceptionValidationRule(), bindingExpressionBase));
				return;
			}

			Validation.ClearInvalid(bindingExpressionBase);
			OutputExample = resultStr;
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		static DateTime excelBase = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Local).AddDays(-2);
		static DateTime? InterpretFormat(string value, string format, bool inputUTC, bool generic = true)
		{
			try
			{
				DateTime? result = null;
				if ((!result.HasValue) && (format == Unix))
				{
					double seconds;
					if (!Double.TryParse(value, out seconds))
						return null;
					return epoch.AddSeconds(seconds);
				}

				if ((!result.HasValue) && (format == FileTime))
				{
					long fileTimeLong;
					if (!Int64.TryParse(value, out fileTimeLong))
						return null;
					return DateTime.FromFileTimeUtc(fileTimeLong);
				}

				if ((!result.HasValue) && (format == Excel))
				{
					double days;
					if (!Double.TryParse(value, out days))
						return null;
					return excelBase.AddDays(days);
				}

				if (!result.HasValue)
				{
					DateTime dateTimeValue;
					if (DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.RoundtripKind, out dateTimeValue))
						result = dateTimeValue;
				}

				if ((!result.HasValue) && (generic))
				{
					DateTime dateTimeValue;
					if (DateTime.TryParse(value, out dateTimeValue))
						result = dateTimeValue;
				}

				if ((result.HasValue) && (result.Value.Kind == DateTimeKind.Unspecified))
					result = DateTime.SpecifyKind(result.Value, inputUTC ? DateTimeKind.Utc : DateTimeKind.Local);

				return result;
			}
			catch { return null; }
		}

		static string InterpretFormat(DateTime value, string format, bool toUTC)
		{
			if (format == Unix)
				return (value.ToUniversalTime() - epoch).TotalSeconds.ToString();
			if (format == FileTime)
				return value.ToFileTimeUtc().ToString();
			if (format == Excel)
				return (value.ToLocalTime() - excelBase).TotalDays.ToString();


			if (toUTC)
				value = value.ToUniversalTime();
			else
				value = value.ToLocalTime();

			return value.ToString(format);
		}

		static public string ConvertFormat(string input, string inputFormat, bool inputUTC, string outputFormat, bool outputUTC)
		{
			var value = InterpretFormat(input, inputFormat, inputUTC);
			if (value == null)
				throw new Exception(String.Format("Can't interpret time: {0}", input));
			return InterpretFormat(value.Value, outputFormat, outputUTC);
		}

		static public bool Run(string example, out string inputFormat, out bool inputUTC, out string outputFormat, out bool outputUTC)
		{
			inputFormat = outputFormat = null;
			inputUTC = outputUTC = false;
			var dialog = new ConvertTime(example);
			if (dialog.ShowDialog() != true)
				return false;

			inputFormat = dialog.InputFormat;
			inputUTC = dialog.InputUTC;
			outputFormat = dialog.OutputFormat;
			outputUTC = dialog.OutputUTC;

			return true;
		}
	}
}
