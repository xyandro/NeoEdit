using System;
using System.Data;
using System.Linq;
using NeoEdit.TextEdit.Dialogs;

namespace NeoEdit.TextEdit
{
	partial class TextEditor
	{
		void Command_DateTime_Now() => ReplaceSelections(DateTime.Now.ToString("O"));

		ConvertDateTimeDialog.Result Command_DateTime_Convert_Dialog()
		{
			if (Selections.Count < 1)
				return null;

			return ConvertDateTimeDialog.Run(WindowParent, GetString(Selections.First()));
		}

		void Command_DateTime_Convert(ConvertDateTimeDialog.Result result) => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => ConvertDateTimeDialog.ConvertFormat(GetString(range), result.InputFormat, result.InputUTC, result.OutputFormat, result.OutputUTC)).ToList());
	}
}
