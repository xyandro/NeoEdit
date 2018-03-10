using System;
using System.Data;
using System.Linq;
using NeoEdit.TextEdit.Dialogs;

namespace NeoEdit.TextEdit
{
	partial class TextEditor
	{
		string AdjustColor(string color, double multiplier, bool doAlpha, bool doRed, bool doGreen, bool doBlue)
		{
			ColorConverter.GetARGB(color, out byte alpha, out byte red, out byte green, out byte blue);
			if (doAlpha)
				alpha = (byte)Math.Max(0, Math.Min((int)(alpha * multiplier + 0.5), 255));
			if (doRed)
				red = (byte)Math.Max(0, Math.Min((int)(red * multiplier + 0.5), 255));
			if (doGreen)
				green = (byte)Math.Max(0, Math.Min((int)(green * multiplier + 0.5), 255));
			if (doBlue)
				blue = (byte)Math.Max(0, Math.Min((int)(blue * multiplier + 0.5), 255));
			return ColorConverter.FromARGB(alpha, red, green, blue);
		}

		ImageGrabColorDialog.Result Command_Image_GrabColor_Dialog() => ImageGrabColorDialog.Run(WindowParent, Selections.Select(range => GetString(range)).FirstOrDefault());

		void Command_Image_GrabColor(ImageGrabColorDialog.Result result) => ReplaceSelections(result.Color);

		ImageAdjustColorDialog.Result Command_Image_AdjustColor_Dialog() => ImageAdjustColorDialog.Run(WindowParent, GetVariables());

		void Command_Image_AdjustColor(ImageAdjustColorDialog.Result result)
		{
			var results = GetFixedExpressionResults<double>(result.Expression);
			var strs = Selections.AsParallel().AsOrdered().Select((range, index) => AdjustColor(GetString(range), results[index], result.Alpha, result.Red, result.Green, result.Blue)).ToList();
			ReplaceSelections(strs);
		}
	}
}
