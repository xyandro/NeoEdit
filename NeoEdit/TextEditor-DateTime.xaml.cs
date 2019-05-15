﻿using System;
using System.Data;
using System.Linq;
using NeoEdit.Dialogs;

namespace NeoEdit
{
	partial class TextEditor
	{
		static void Command_DateTime_Now(ITextEditor te) => te.ReplaceSelections(DateTimeOffset.Now.ToString("O"));

		static void Command_DateTime_UtcNow(ITextEditor te) => te.ReplaceSelections(DateTimeOffset.UtcNow.ToString("O"));

		static DateTimeConvertDialog.Result Command_DateTime_Convert_Dialog(ITextEditor te)
		{
			if (te.Selections.Count < 1)
				return null;

			return DateTimeConvertDialog.Run(te.TabsParent, te.GetString(te.Selections.First()));
		}

		static void Command_DateTime_Convert(ITextEditor te, DateTimeConvertDialog.Result result) => te.ReplaceSelections(te.Selections.AsParallel().AsOrdered().Select(range => DateTimeConvertDialog.ConvertFormat(te.GetString(range), result.InputFormat, result.InputTimeZone, result.OutputFormat, result.OutputTimeZone)).ToList());
	}
}
