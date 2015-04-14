using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class ConvertDateTimeDialog
	{
		internal class Result
		{
			public string InputFormat { get; set; }
			public bool InputUTC { get; set; }
			public string OutputFormat { get; set; }
			public bool OutputUTC { get; set; }
		}

		[DepProp]
		public string InputFormat { get { return UIHelper<ConvertDateTimeDialog>.GetPropValue<string>(this); } set { UIHelper<ConvertDateTimeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool InputUTC { get { return UIHelper<ConvertDateTimeDialog>.GetPropValue<bool>(this); } set { UIHelper<ConvertDateTimeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string OutputFormat { get { return UIHelper<ConvertDateTimeDialog>.GetPropValue<string>(this); } set { UIHelper<ConvertDateTimeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool OutputUTC { get { return UIHelper<ConvertDateTimeDialog>.GetPropValue<bool>(this); } set { UIHelper<ConvertDateTimeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Example { get { return UIHelper<ConvertDateTimeDialog>.GetPropValue<string>(this); } set { UIHelper<ConvertDateTimeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string ParsedExample { get { return UIHelper<ConvertDateTimeDialog>.GetPropValue<string>(this); } set { UIHelper<ConvertDateTimeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string OutputExample { get { return UIHelper<ConvertDateTimeDialog>.GetPropValue<string>(this); } set { UIHelper<ConvertDateTimeDialog>.SetPropValue(this, value); } }

		static ConvertDateTimeDialog()
		{
			UIHelper<ConvertDateTimeDialog>.Register();
			UIHelper<ConvertDateTimeDialog>.AddCallback(a => a.InputFormat, (obj, o, n) => obj.CheckValidInput());
			UIHelper<ConvertDateTimeDialog>.AddCallback(a => a.InputUTC, (obj, o, n) => { obj.CheckValidInput(); obj.OutputUTC = obj.InputUTC; });
			UIHelper<ConvertDateTimeDialog>.AddCallback(a => a.OutputFormat, (obj, o, n) => obj.CheckValidInput());
			UIHelper<ConvertDateTimeDialog>.AddCallback(a => a.OutputUTC, (obj, o, n) => obj.CheckValidInput());
		}

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

		ConvertDateTimeDialog(string _example)
		{
			InitializeComponent();

			Example = _example;

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

			var result = InterpretFormat(Example, InputFormat, InputUTC);
			inputFormat.SetValidation(ComboBox.TextProperty, result != null);
			if (result == null)
				return;

			ParsedExample = result.Value.ToString("O");

			var resultStr = InterpretFormat(result.Value, OutputFormat, OutputUTC);
			outputFormat.SetValidation(ComboBox.TextProperty, result != null);
			if (resultStr == null)
				return;

			OutputExample = resultStr;
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

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { InputFormat = InputFormat, InputUTC = InputUTC, OutputFormat = OutputFormat, OutputUTC = OutputUTC };
			DialogResult = true;
		}

		public static Result Run(string example)
		{
			var dialog = new ConvertDateTimeDialog(example);
			return dialog.ShowDialog() == true ? dialog.result : null;
		}
	}
}
