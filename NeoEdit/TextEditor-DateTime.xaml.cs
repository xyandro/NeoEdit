using System;
using System.Data;
using System.Linq;
using NeoEdit.Program.Dialogs;

namespace NeoEdit.Program
{
	partial class TextEditor
	{
		void Command_DateTime_Now() => ReplaceSelections(DateTimeOffset.Now.ToString("O"));

		void Command_DateTime_UtcNow() => ReplaceSelections(DateTimeOffset.UtcNow.ToString("O"));

		DateTimeConvertDialog.Result Command_DateTime_Convert_Dialog()
		{
			if (Selections.Count < 1)
				return null;

			return DateTimeConvertDialog.Run(TabsParent, GetString(Selections.First()));
		}

		void Command_DateTime_Convert(DateTimeConvertDialog.Result result) => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => DateTimeConvertDialog.ConvertFormat(GetString(range), result.InputFormat, result.InputTimeZone, result.OutputFormat, result.OutputTimeZone)).ToList());

		void Command_DateTime_ToUtc() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => DateTimeOffset.Parse(GetString(range)).ToUniversalTime().ToString("O")).ToList());

		void Command_DateTime_ToLocal() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => DateTimeOffset.Parse(GetString(range)).ToLocalTime().ToString("O")).ToList());
	}
}
