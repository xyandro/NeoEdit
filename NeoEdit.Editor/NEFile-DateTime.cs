using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Transform;
using NeoEdit.TaskRunning;

namespace NeoEdit.Editor
{
	partial class NEFile
	{
		string AddDates(string str1, string str2)
		{
			var val1 = GetAddSubtractValue(str1);
			if (val1 == null)
				throw new Exception($"Invalid input: {str1}");

			var val2 = GetAddSubtractValue(str2);
			if (val2 == null)
				throw new Exception($"Invalid input: {str2}");

			switch (val1)
			{
				case NumericValue nv1:
					switch (val2)
					{
						case NumericValue nv2: return SimplifyDateUnits(nv1 + nv2).ToString();
						case DateTimeOffset dto2: return Dater.DateTimeOffsetToString(dto2 + TimeSpan.FromTicks(nv1.RoundedLongValue));
					}
					break;
				case DateTimeOffset dto1:
					switch (val2)
					{
						case NumericValue nv2: return Dater.DateTimeOffsetToString(dto1 + TimeSpan.FromTicks(nv2.RoundedLongValue));
					}
					break;
			}
			throw new Exception($"Invalid inputs: {str1}, {str2}");
		}

		object GetAddSubtractValue(string str)
		{
			var split = str.IndexOf(' ');
			if ((split > 0) && (str.IndexOf(' ', split + 1) == -1))
				return new NumericValue(str.Substring(0, split), str.Substring(split + 1)).ConvertUnits(new ExpressionUnits("ticks"));
			return Dater.TryStringToDateTimeOffset(str);
		}

		NumericValue SimplifyDateUnits(NumericValue value)
		{
			var units = new List<string> { "days", "hours", "minutes", "seconds", "ms", "µs", "ns" };
			foreach (var unit in units)
			{
				var testUnit = new NumericValue(1, unit);
				if (value >= testUnit)
					return value.ConvertUnits(testUnit.Units);
			}
			return value;
		}

		string SubtractDates(string str1, string str2)
		{
			var val1 = GetAddSubtractValue(str1);
			if (val1 == null)
				throw new Exception($"Invalid input: {str1}");

			var val2 = GetAddSubtractValue(str2);
			if (val2 == null)
				throw new Exception($"Invalid input: {str2}");

			switch (val1)
			{
				case NumericValue nv1:
					switch (val2)
					{
						case NumericValue nv2: return SimplifyDateUnits(nv1 - nv2).ToString();
					}
					break;
				case DateTimeOffset dto1:
					switch (val2)
					{
						case NumericValue nv2: return Dater.DateTimeOffsetToString(dto1 - TimeSpan.FromTicks(nv2.RoundedLongValue));
						case DateTimeOffset dto2: return SimplifyDateUnits(new NumericValue((dto1 - dto2).Ticks, "ticks")).ToString();
					}
					break;
			}

			throw new Exception($"Invalid inputs: {str1}, {str2}");
		}

		void Execute_DateTime_Now() => ReplaceSelections(Dater.DateTimeOffsetToString(DateTimeOffset.Now));

		void Execute_DateTime_UTCNow() => ReplaceSelections(Dater.DateTimeOffsetToString(DateTimeOffset.UtcNow));

		void Execute_DateTime_ToUTC() => ReplaceSelections(Selections.AsTaskRunner().Select(range => Dater.DateTimeOffsetToString(Dater.ChangeTimeZone(Dater.StringToDateTimeOffset(Text.GetString(range), defaultTimeZone: Dater.UTC), Dater.UTC))).ToList());

		void Execute_DateTime_ToLocal() => ReplaceSelections(Selections.AsTaskRunner().Select(range => Dater.DateTimeOffsetToString(Dater.ChangeTimeZone(Dater.StringToDateTimeOffset(Text.GetString(range), defaultTimeZone: Dater.Local), Dater.Local))).ToList());

		static void Configure_DateTime_ToTimeZone() => EditorExecuteState.CurrentState.Configuration = EditorExecuteState.CurrentState.NEWindow.FilesWindow.RunDialog_Configure_DateTime_ToTimeZone();

		void Execute_DateTime_ToTimeZone()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_DateTime_ToTimeZone;
			ReplaceSelections(Selections.AsTaskRunner().Select(range => Dater.DateTimeOffsetToString(Dater.ChangeTimeZone(Dater.StringToDateTimeOffset(Text.GetString(range), defaultTimeZone: result.TimeZone), result.TimeZone))).ToList());
		}

		static void Configure_DateTime_Format() => EditorExecuteState.CurrentState.Configuration = EditorExecuteState.CurrentState.NEWindow.FilesWindow.RunDialog_Configure_DateTime_Format(EditorExecuteState.CurrentState.NEWindow.Focused.Selections.Select(range => EditorExecuteState.CurrentState.NEWindow.Focused.Text.GetString(range)).DefaultIfEmpty(Dater.DateTimeOffsetToString(DateTimeOffset.Now)).First());

		void Execute_DateTime_Format()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_DateTime_Format;
			ReplaceSelections(Selections.AsTaskRunner().Select(range => Dater.DateTimeOffsetToString(Dater.StringToDateTimeOffset(Text.GetString(range), result.InputFormat), result.OutputFormat)).ToList());
		}

		void Execute_DateTime_AddClipboard()
		{
			if (Selections.Count == 0)
				return;

			var clipboardStrings = Clipboard;
			if ((clipboardStrings.Count == 1) && (Selections.Count != 1))
				clipboardStrings = Selections.Select(str => clipboardStrings[0]).ToList();

			if (Selections.Count != clipboardStrings.Count())
				throw new Exception("Must have either one or equal number of clipboards.");

			ReplaceSelections(Selections.AsTaskRunner().Select((range, index) => AddDates(Text.GetString(range), clipboardStrings[index]).ToString()).ToList());
		}

		void Execute_DateTime_SubtractClipboard()
		{
			if (Selections.Count == 0)
				return;

			var clipboardStrings = Clipboard;
			if ((clipboardStrings.Count == 1) && (Selections.Count != 1))
				clipboardStrings = Selections.Select(str => clipboardStrings[0]).ToList();

			if (Selections.Count != clipboardStrings.Count())
				throw new Exception("Must have either one or equal number of clipboards.");

			ReplaceSelections(Selections.AsTaskRunner().Select((range, index) => SubtractDates(Text.GetString(range), clipboardStrings[index]).ToString()).ToList());
		}
	}
}
