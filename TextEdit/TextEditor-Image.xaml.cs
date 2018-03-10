using System;
using System.Data;
using System.Linq;
using NeoEdit.TextEdit.Dialogs;

namespace NeoEdit.TextEdit
{
	partial class TextEditor
	{
		string AddColor(string color1, string color2)
		{
			ColorConverter.GetARGB(color1, out byte alpha1, out byte red1, out byte green1, out byte blue1);
			ColorConverter.GetARGB(color2, out byte alpha2, out byte red2, out byte green2, out byte blue2);
			alpha1 = (byte)Math.Max(0, Math.Min(alpha1 + alpha2, 255));
			red1 = (byte)Math.Max(0, Math.Min(red1 + red2, 255));
			green1 = (byte)Math.Max(0, Math.Min(green1 + green2, 255));
			blue1 = (byte)Math.Max(0, Math.Min(blue1 + blue2, 255));
			return ColorConverter.FromARGB(alpha1, red1, green1, blue1);
		}

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

		ImageAddColorDialog.Result Command_Image_AddColor_Dialog() => ImageAddColorDialog.Run(WindowParent, GetVariables());

		void Command_Image_AddColor(ImageAddColorDialog.Result result)
		{
			var results = GetFixedExpressionResults<string>(result.Expression);
			var strs = Selections.AsParallel().AsOrdered().Select((range, index) => AddColor(GetString(range), results[index])).ToList();
			ReplaceSelections(strs);
		}
	}
}
