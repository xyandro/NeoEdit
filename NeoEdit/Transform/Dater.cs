using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace NeoEdit.Program.Transform
{
	public static class Dater
	{
		class DaterFormat
		{
			public string Name { get; }
			public string Format { get; }

			public DaterFormat(string name, string format)
			{
				Name = name;
				Format = format;
			}
		}

		class DaterTimeZone
		{
			public string Name { get; }
			public TimeZoneInfo TimeZoneInfo { get; }

			public DaterTimeZone(string name, TimeZoneInfo timeZoneInfo)
			{
				Name = name;
				TimeZoneInfo = timeZoneInfo;
			}
		}

		public static readonly string RoundTripDateTime = nameof(RoundTripDateTime);
		public static readonly string Unix = nameof(Unix);
		public static readonly string FileTime = nameof(FileTime);
		public static readonly string Excel = nameof(Excel);
		public static readonly string Ticks = nameof(Ticks);
		public static readonly string ShortDateFullTime = nameof(ShortDateFullTime);
		public static readonly string ShortDateShortTime = nameof(ShortDateShortTime);
		public static readonly string FullDate = nameof(FullDate);
		public static readonly string FullDateFullTime = nameof(FullDateFullTime);
		public static readonly string FullDateShortTime = nameof(FullDateShortTime);
		public static readonly string UTCFullDateTime = nameof(UTCFullDateTime);
		public static readonly string UTCSortableDateTime = nameof(UTCSortableDateTime);
		public static readonly string SortableDateTime = nameof(SortableDateTime);
		public static readonly string ShortDate = nameof(ShortDate);
		public static readonly string MonthDay = nameof(MonthDay);
		public static readonly string FullTime = nameof(FullTime);
		public static readonly string ShortTime = nameof(ShortTime);
		public static readonly string YearMonth = nameof(YearMonth);
		public static readonly string RFC1123 = nameof(RFC1123);

		static readonly List<DaterFormat> daterFormats;
		static readonly Dictionary<string, DaterFormat> daterFormatsDictionary;

		public static readonly string Local = $"{nameof(Local)} {TimeZoneInfo.Local.DisplayName}";
		public static readonly string UTC = nameof(UTC);

		static readonly List<DaterTimeZone> daterTimeZones;
		static readonly Dictionary<string, DaterTimeZone> daterTimeZonesDictionary;

		static DateTimeOffset epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
		static DateTimeOffset excelBase = new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.Zero);

		static Dater()
		{
			daterFormats = new List<DaterFormat>
			{
				new DaterFormat(RoundTripDateTime, "O"),
				new DaterFormat(Unix, nameof(Unix)),
				new DaterFormat(FileTime, nameof(FileTime)),
				new DaterFormat(Excel, nameof(Excel)),
				new DaterFormat(Ticks, nameof(Ticks)),
				new DaterFormat(ShortDateFullTime, "G"),
				new DaterFormat(ShortDateShortTime, "g"),
				new DaterFormat(FullDate, "D"),
				new DaterFormat(FullDateFullTime, "F"),
				new DaterFormat(FullDateShortTime, "f"),
				new DaterFormat(UTCFullDateTime, "U"),
				new DaterFormat(UTCSortableDateTime, "u"),
				new DaterFormat(SortableDateTime, "s"),
				new DaterFormat(ShortDate, "d"),
				new DaterFormat(MonthDay, "M"),
				new DaterFormat(FullTime, "T"),
				new DaterFormat(ShortTime, "t"),
				new DaterFormat(YearMonth, "Y"),
				new DaterFormat(RFC1123, "R"),
			};

			daterFormatsDictionary = daterFormats.ToDictionary(f => f.Name);

			daterTimeZones = new List<DaterTimeZone>
			{
				new DaterTimeZone(Local, TimeZoneInfo.Local),
				new DaterTimeZone(UTC, TimeZoneInfo.Utc),
			};
			daterTimeZones.AddRange(TimeZoneInfo.GetSystemTimeZones().Select(tz => new DaterTimeZone(tz.DisplayName, tz)));

			daterTimeZonesDictionary = daterTimeZones.ToDictionary(tz => tz.Name);
		}

		public static DateTimeOffset? TryStringToDateTimeOffset(string value, string format = null, string defaultTimeZone = null, bool generic = true)
		{
			try
			{
				format = format ?? RoundTripDateTime;

				if (daterFormatsDictionary.ContainsKey(format))
					format = daterFormatsDictionary[format].Format;

				defaultTimeZone = defaultTimeZone ?? Local;

				if (format == Unix)
				{
					if (!double.TryParse(value, out var seconds))
						return null;
					return epoch.AddSeconds(seconds);
				}

				if (format == FileTime)
				{
					if (!long.TryParse(value, out var fileTime))
						return null;
					return DateTimeOffset.FromFileTime(fileTime).ToUniversalTime();
				}

				if (format == Excel)
				{
					if (!double.TryParse(value, out var days))
						return null;
					return excelBase.AddDays(days + (days < 61 ? 1 : 0) - 2);
				}

				if (format == Ticks)
				{
					if (!long.TryParse(value, out var ticks))
						return null;
					return new DateTimeOffset(ticks, TimeSpan.Zero);
				}

				if (DateTimeOffset.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.RoundtripKind, out var dateTimeOffset))
				{
					if ((DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.RoundtripKind, out var dateTime)) && (dateTime.Kind == DateTimeKind.Unspecified))
						dateTimeOffset = SetTimeZone(dateTimeOffset, defaultTimeZone);

					return dateTimeOffset;
				}

				if (generic)
				{
					if (DateTimeOffset.TryParse(value, out dateTimeOffset))
					{
						if ((DateTime.TryParse(value, out var dateTime)) && (dateTime.Kind == DateTimeKind.Unspecified))
							dateTimeOffset = SetTimeZone(dateTimeOffset, defaultTimeZone);

						return dateTimeOffset;
					}
				}
			}
			catch { }
			return null;
		}

		public static DateTimeOffset StringToDateTimeOffset(string value, string format = null, string defaultTimeZone = null, bool generic = true)
		{
			var result = TryStringToDateTimeOffset(value, format ?? RoundTripDateTime, defaultTimeZone, true);
			if (!result.HasValue)
				throw new ArgumentException($"Invalid DateTime value: {value}");
			return result.Value;
		}

		public static string TryDateTimeOffsetToString(DateTimeOffset value, string format = null)
		{
			try
			{
				format = format ?? RoundTripDateTime;
				if (daterFormatsDictionary.ContainsKey(format))
					format = daterFormatsDictionary[format].Format;

				if (format == Unix)
					return (value - epoch).TotalSeconds.ToString();
				if (format == FileTime)
					return value.ToFileTime().ToString();
				if (format == Excel)
				{
					var days = (value - excelBase).TotalDays + 2;
					if (days < 61)
						--days;
					return days.ToString();
				}
				if (format == Ticks)
					return value.ToUniversalTime().Ticks.ToString();

				return value.ToString(format);
			}
			catch { return null; }
		}

		public static string DateTimeOffsetToString(DateTimeOffset value, string format = null)
		{
			var str = TryDateTimeOffsetToString(value, format ?? RoundTripDateTime);
			if (str == null)
				throw new ArgumentException($"Invalid DateTime value: {value}");
			return str;
		}

		public static DateTimeOffset? TryChangeTimeZone(DateTimeOffset value, string timeZone)
		{
			try
			{
				switch (GetTimeZone(timeZone))
				{
					case TimeZoneInfo tzi: return TimeZoneInfo.ConvertTime(value, tzi);
					case TimeSpan ts: return value.ToOffset(ts);
				}
			}
			catch { }
			return null;
		}

		public static DateTimeOffset ChangeTimeZone(DateTimeOffset value, string timeZone)
		{
			var result = TryChangeTimeZone(value, timeZone);
			if (result == null)
				throw new ArgumentException($"Invalid TimeZone value");
			return result.Value;
		}

		static DateTimeOffset SetTimeZone(DateTimeOffset dateTime, string timeZone)
		{
			switch (GetTimeZone(timeZone))
			{
				case TimeSpan ts: return new DateTimeOffset(dateTime.DateTime, ts);
				case TimeZoneInfo tzi: return new DateTimeOffset(dateTime.DateTime, tzi.GetUtcOffset(dateTime.DateTime));
				default: return dateTime;
			}
		}

		static object GetTimeZone(string timeZone)
		{
			if (string.IsNullOrWhiteSpace(timeZone))
				return null;
			if (daterTimeZonesDictionary.ContainsKey(timeZone))
				return daterTimeZonesDictionary[timeZone].TimeZoneInfo;
			if (double.TryParse(timeZone, out var d))
				return TimeSpan.FromHours(d);
			if (TimeSpan.TryParse(timeZone, out var timeSpan))
				return timeSpan;
			return null;
		}

		public static List<string> GetAllFormats() => daterFormats.Select(f => f.Name).ToList();

		public static List<string> GetAllTimeZones() => daterTimeZones.Select(f => f.Name).ToList();

		public static string GuessFormat(string str) => daterFormats.Select(f => f.Name).Where(format => TryStringToDateTimeOffset(str, format, UTC, false) != null).DefaultIfEmpty(null).First();
	}
}
