using System;
using System.Data;
using System.Linq;
using NeoEdit.Program.Dialogs;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program
{
	partial class TextEditor
	{
		void Command_DateTime_Now() => ReplaceSelections(Dater.DateTimeOffsetToString(DateTimeOffset.Now));

		void Command_DateTime_UtcNow() => ReplaceSelections(Dater.DateTimeOffsetToString(DateTimeOffset.UtcNow));

		DateTimeFormatDialog.Result Command_DateTime_Format_Dialog() => DateTimeFormatDialog.Run(TabsParent, Selections.Select(range => GetString(range)).DefaultIfEmpty(Dater.DateTimeOffsetToString(DateTimeOffset.Now)).First());

		void Command_DateTime_Format(DateTimeFormatDialog.Result result) => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Dater.DateTimeOffsetToString(Dater.StringToDateTimeOffset(GetString(range), result.InputFormat), result.OutputFormat)).ToList());

		void Command_DateTime_ToUtc() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Dater.DateTimeOffsetToString(Dater.ChangeTimeZone(Dater.StringToDateTimeOffset(GetString(range), defaultTimeZone: Dater.UTC), Dater.UTC))).ToList());

		void Command_DateTime_ToLocal() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Dater.DateTimeOffsetToString(Dater.ChangeTimeZone(Dater.StringToDateTimeOffset(GetString(range), defaultTimeZone: Dater.Local), Dater.Local))).ToList());

		DateTimeToTimeZoneDialog.Result Command_DateTime_ToTimeZone_Dialog() => DateTimeToTimeZoneDialog.Run(TabsParent);

		void Command_DateTime_ToTimeZone(DateTimeToTimeZoneDialog.Result result) => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Dater.DateTimeOffsetToString(Dater.ChangeTimeZone(Dater.StringToDateTimeOffset(GetString(range), defaultTimeZone: result.TimeZone), result.TimeZone))).ToList());
	}
}
