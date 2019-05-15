using System;
using System.Data;
using System.Linq;
using NeoEdit.Dialogs;

namespace NeoEdit
{
	partial class TextEditor
	{
		void Command_DateTime_Now() => ReplaceSelections(DateTimeOffset.Now.ToString("O"));

		void Command_DateTime_UtcNow() => ReplaceSelections(DateTimeOffset.UtcNow.ToString("O"));

		DateTimeConvertDialog.Result Command_DateTime_Convert_Dialog(ITextEditor te)
		{
			if (te.Selections.Count < 1)
				return null;

			return DateTimeConvertDialog.Run(te.TabsParent, GetString(te.Selections.First()));
		}

		void Command_DateTime_Convert(ITextEditor te, DateTimeConvertDialog.Result result) => te.ReplaceSelections(te.Selections.AsParallel().AsOrdered().Select(range => DateTimeConvertDialog.ConvertFormat(GetString(range), result.InputFormat, result.InputTimeZone, result.OutputFormat, result.OutputTimeZone)).ToList());
	}
}
