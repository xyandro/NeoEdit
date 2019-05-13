using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.TextEdit;
using NeoEdit.TextEdit.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class DateTimeConvertDialog
	{
		internal class Result
		{
			public string InputFormat { get; set; }
			public string InputTimeZone { get; set; }
			public string OutputFormat { get; set; }
			public string OutputTimeZone { get; set; }
		}

		[DepProp]
		public string InputFormat { get { return UIHelper<DateTimeConvertDialog>.GetPropValue<string>(this); } set { UIHelper<DateTimeConvertDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string InputTimeZone { get { return UIHelper<DateTimeConvertDialog>.GetPropValue<string>(this); } set { UIHelper<DateTimeConvertDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string OutputFormat { get { return UIHelper<DateTimeConvertDialog>.GetPropValue<string>(this); } set { UIHelper<DateTimeConvertDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string OutputTimeZone { get { return UIHelper<DateTimeConvertDialog>.GetPropValue<string>(this); } set { UIHelper<DateTimeConvertDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Example { get { return UIHelper<DateTimeConvertDialog>.GetPropValue<string>(this); } set { UIHelper<DateTimeConvertDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string ParsedExample { get { return UIHelper<DateTimeConvertDialog>.GetPropValue<string>(this); } set { UIHelper<DateTimeConvertDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string OutputExample { get { return UIHelper<DateTimeConvertDialog>.GetPropValue<string>(this); } set { UIHelper<DateTimeConvertDialog>.SetPropValue(this, value); } }

		readonly static List<string> formats;
		readonly static List<string> timeZones;

		const string Unix = "Unix";
		const string FileTime = "FileTime";
		const string Excel = "Excel";
		const string Ticks = "Ticks";
		static readonly string Local = $"Local {TimeZoneInfo.Local.DisplayName}";
		const string UTC = "UTC";

		static DateTimeConvertDialog()
		{
			UIHelper<DateTimeConvertDialog>.Register();
			UIHelper<DateTimeConvertDialog>.AddCallback(a => a.InputFormat, (obj, o, n) => obj.CheckValidInput());
			UIHelper<DateTimeConvertDialog>.AddCallback(a => a.InputTimeZone, (obj, o, n) => obj.CheckValidInput());
			UIHelper<DateTimeConvertDialog>.AddCallback(a => a.OutputFormat, (obj, o, n) => obj.CheckValidInput());
			UIHelper<DateTimeConvertDialog>.AddCallback(a => a.OutputTimeZone, (obj, o, n) => obj.CheckValidInput());

			formats = new List<string>
			{
				"O",
				Unix,
				FileTime,
				Excel,
				Ticks,
				"d", "D", "f", "F", "g", "G", "M", "R", "s", "t", "T", "u", "U", "Y",
			};

			timeZones = new List<string>
			{
				Local,
				UTC,
			};
			TimeZoneInfo.GetSystemTimeZones().ForEach(timeZone => timeZones.Add(timeZone.DisplayName));
		}

		DateTimeConvertDialog(string _example)
		{
			InitializeComponent();

			Example = _example;

			foreach (var format in formats)
			{
				inputFormat.Items.Add(format);
				outputFormat.Items.Add(format);

				var value = ToDateTimeOffset(Example, format, Local, false);
				if (value != null)
				{
					InputFormat = format;
					if (value?.Offset == TimeSpan.Zero)
						InputTimeZone = OutputTimeZone = UTC;
					else if (TimeZoneInfo.Local.GetUtcOffset(value.Value) == value?.Offset)
						InputTimeZone = OutputTimeZone = Local;
					else
						InputTimeZone = OutputTimeZone = value?.Offset.ToString();
				}
			}

			inputTimeZone.AddSuggestions(timeZones.ToArray());
			outputTimeZone.AddSuggestions(timeZones.ToArray());

			if (string.IsNullOrEmpty(InputFormat))
				InputFormat = formats.First();
			OutputFormat = formats.First();
		}

		void CheckValidInput()
		{
			ParsedExample = OutputExample = "";

			var result = ToDateTimeOffset(Example, InputFormat, InputTimeZone);
			inputFormat.SetValidation(ComboBox.TextProperty, result != null);
			if (result == null)
				return;

			ParsedExample = result.Value.ToString("O");

			var resultStr = FromDateTimeOffset(result.Value, OutputFormat, OutputTimeZone);
			outputFormat.SetValidation(ComboBox.TextProperty, result != null);
			if (resultStr == null)
				return;

			OutputExample = resultStr;
		}

		static object GetTimeZone(string timeZone)
		{
			if (string.IsNullOrWhiteSpace(timeZone))
				return null;
			if (timeZone == Local)
				return TimeZoneInfo.Local;
			if (timeZone == UTC)
				return TimeZoneInfo.Utc;
			var found = TimeZoneInfo.GetSystemTimeZones().FirstOrDefault(tz => tz.DisplayName == timeZone);
			if (found != null)
				return found;
			TimeSpan timeSpan;
			if (TimeSpan.TryParse(timeZone, out timeSpan))
				return timeSpan;
			return null;
		}

		static DateTimeOffset SetTimeZone(DateTimeOffset dateTime, string timeZone)
		{
			var tz = GetTimeZone(timeZone);
			if (tz is TimeSpan)
				dateTime = new DateTimeOffset(dateTime.DateTime, (TimeSpan)tz);
			else if (tz is TimeZoneInfo)
				dateTime = new DateTimeOffset(dateTime.DateTime, (tz as TimeZoneInfo).GetUtcOffset(dateTime.DateTime));
			return dateTime;
		}

		static DateTimeOffset ConvertTimeZone(DateTimeOffset dateTime, string timeZone)
		{
			var tz = GetTimeZone(timeZone);
			if (tz is TimeSpan)
				dateTime = dateTime.ToOffset((TimeSpan)tz);
			else if (tz is TimeZoneInfo)
				dateTime = TimeZoneInfo.ConvertTime(dateTime, tz as TimeZoneInfo);
			return dateTime;
		}

		static DateTimeOffset epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
		static DateTimeOffset excelBase = new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.Zero);
		static DateTimeOffset? ToDateTimeOffset(string value, string format, string timeZone, bool generic = true)
		{
			try
			{
				if (format == Unix)
				{
					double seconds;
					if (!double.TryParse(value, out seconds))
						return null;
					return epoch.AddSeconds(seconds);
				}

				if (format == FileTime)
				{
					long fileTimeLong;
					if (!long.TryParse(value, out fileTimeLong))
						return null;
					return DateTimeOffset.FromFileTime(fileTimeLong);
				}

				if (format == Excel)
				{
					double days;
					if (!double.TryParse(value, out days))
						return null;
					return SetTimeZone(excelBase.AddDays(days + (days < 61 ? 1 : 0) - 2), timeZone);
				}

				if (format == Ticks)
				{
					long ticks;
					if (!long.TryParse(value, out ticks))
						return null;
					return SetTimeZone(new DateTimeOffset(ticks, TimeSpan.Zero), timeZone);
				}

				DateTimeOffset dateTime;
				if (DateTimeOffset.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.RoundtripKind, out dateTime))
				{
					DateTime dateTimeValue;
					if ((DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.RoundtripKind, out dateTimeValue)) && (dateTimeValue.Kind == DateTimeKind.Unspecified))
						dateTime = SetTimeZone(dateTime, timeZone);

					return dateTime;
				}

				if (generic)
				{
					if (DateTimeOffset.TryParse(value, out dateTime))
					{
						DateTime dateTimeValue;
						if ((DateTime.TryParse(value, out dateTimeValue)) && (dateTimeValue.Kind == DateTimeKind.Unspecified))
							dateTime = SetTimeZone(dateTime, timeZone);

						return dateTime;
					}
				}
			}
			catch { }
			return null;
		}

		static string FromDateTimeOffset(DateTimeOffset value, string format, string timeZone)
		{
			if (format == Unix)
				return (value - epoch).TotalSeconds.ToString();
			if (format == FileTime)
				return value.ToFileTime().ToString();
			if (format == Excel)
			{
				value = new DateTimeOffset(ConvertTimeZone(value, timeZone).DateTime, TimeSpan.Zero);
				var days = (value - excelBase).TotalDays + 2;
				if (days < 61)
					--days;
				return days.ToString();
			}

			if (format == Ticks)
				return ConvertTimeZone(value, timeZone).Ticks.ToString();

			var tz = GetTimeZone(timeZone);
			if (tz is TimeZoneInfo)
				value = TimeZoneInfo.ConvertTime(value, tz as TimeZoneInfo);
			else if (tz is TimeSpan)
				value = value.ToOffset((TimeSpan)tz);

			return value.ToString(format);
		}

		static public string ConvertFormat(string input, string inputFormat, string inputTimeZone, string outputFormat, string outputTimeZone)
		{
			var value = ToDateTimeOffset(input, inputFormat, inputTimeZone);
			if (value == null)
				throw new Exception($"Can't interpret time: {input}");
			return FromDateTimeOffset(value.Value, outputFormat, outputTimeZone);
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { InputFormat = InputFormat, InputTimeZone = InputTimeZone, OutputFormat = OutputFormat, OutputTimeZone = OutputTimeZone };
			DialogResult = true;
		}

		public static Result Run(Window parent, string example)
		{
			var dialog = new DateTimeConvertDialog(example) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
