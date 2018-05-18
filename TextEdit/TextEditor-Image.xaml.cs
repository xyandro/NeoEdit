using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Transform;
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

		NEVariables GetImageVariables() => GetImageVariables(out var bitmap);

		NEVariables GetImageVariables(out System.Drawing.Bitmap bitmap)
		{
			bitmap = Coder.StringToBitmap(AllText);
			var variables = GetVariables();
			variables.Add(NEVariable.Constant("width", "Image width", bitmap.Width));
			variables.Add(NEVariable.Constant("height", "Image height", bitmap.Height));
			return variables;
		}

		string OverlayColor(string color1, string color2)
		{
			ColorConverter.GetARGB(color1, out byte alpha1, out byte red1, out byte green1, out byte blue1);
			ColorConverter.GetARGB(color2, out byte alpha2, out byte red2, out byte green2, out byte blue2);
			red1 = (byte)((red1 * alpha1 / 255) + (red2 * alpha2 * (255 - alpha1) / (255 * 255)));
			green1 = (byte)((green1 * alpha1 / 255) + (green2 * alpha2 * (255 - alpha1) / (255 * 255)));
			blue1 = (byte)((blue1 * alpha1 / 255) + (blue2 * alpha2 * (255 - alpha1) / (255 * 255)));
			alpha1 = (byte)(alpha1 + (alpha2 * (255 - alpha1) / 255));
			return ColorConverter.FromARGB(alpha1, red1, green1, blue1);
		}

		void Flip(System.Drawing.RotateFlipType type)
		{
			var bitmap = Coder.StringToBitmap(AllText);
			bitmap.RotateFlip(type);
			Replace(new List<Range> { FullRange }, new List<string> { Coder.BitmapToString(bitmap) });
			SetSelections(new List<Range> { BeginRange });
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

		ImageAddOverlayColorDialog.Result Command_Image_AddOverlayColor_Dialog(bool add) => ImageAddOverlayColorDialog.Run(WindowParent, add, GetVariables());

		void Command_Image_AddColor(ImageAddOverlayColorDialog.Result result)
		{
			var results = GetFixedExpressionResults<string>(result.Expression);
			var strs = Selections.AsParallel().AsOrdered().Select((range, index) => AddColor(GetString(range), results[index])).ToList();
			ReplaceSelections(strs);
		}

		void Command_Image_OverlayColor(ImageAddOverlayColorDialog.Result result)
		{
			var results = GetFixedExpressionResults<string>(result.Expression);
			var strs = Selections.AsParallel().AsOrdered().Select((range, index) => OverlayColor(results[index], GetString(range))).ToList();
			ReplaceSelections(strs);
		}

		ImageSizeDialog.Result Command_Image_Size_Dialog() => ImageSizeDialog.Run(WindowParent, GetImageVariables());

		void Command_Image_Size(ImageSizeDialog.Result result)
		{
			var variables = GetImageVariables(out var bitmap);
			var width = new NEExpression(result.WidthExpression).Evaluate<int>(variables);
			var height = new NEExpression(result.HeightExpression).Evaluate<int>(variables);

			var resultBitmap = new System.Drawing.Bitmap(width, height, bitmap.PixelFormat);
			resultBitmap.SetResolution(bitmap.HorizontalResolution, bitmap.VerticalResolution);
			using (var graphics = System.Drawing.Graphics.FromImage(resultBitmap))
			{
				graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
				graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
				graphics.InterpolationMode = result.InterpolationMode;
				graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
				graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

				using (var wrapMode = new System.Drawing.Imaging.ImageAttributes())
				{
					wrapMode.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);
					graphics.DrawImage(bitmap, new System.Drawing.Rectangle(0, 0, width, height), 0, 0, bitmap.Width, bitmap.Height, System.Drawing.GraphicsUnit.Pixel, wrapMode);
				}
			}

			Replace(new List<Range> { FullRange }, new List<string> { Coder.BitmapToString(resultBitmap) });
			SetSelections(new List<Range> { BeginRange });
		}

		ImageCropDialog.Result Command_Image_Crop_Dialog() => ImageCropDialog.Run(WindowParent, GetImageVariables());

		void Command_Image_Crop(ImageCropDialog.Result result)
		{
			var variables = GetImageVariables(out var bitmap);
			var destX = new NEExpression(result.XExpression).Evaluate<int>(variables);
			var destY = new NEExpression(result.YExpression).Evaluate<int>(variables);
			var newWidth = new NEExpression(result.WidthExpression).Evaluate<int>(variables);
			var newHeight = new NEExpression(result.HeightExpression).Evaluate<int>(variables);
			if ((newWidth <= 0) || (newHeight <= 0))
				throw new Exception("Width and height must be greater than 0");

			var srcX = 0;
			var srcY = 0;
			var width = bitmap.Width;
			var height = bitmap.Height;
			if (destX < 0)
			{
				width += destX;
				srcX -= destX;
				destX = 0;
			}
			if (destY < 0)
			{
				height += destY;
				srcY -= destY;
				destY = 0;
			}
			width = Math.Min(width, newWidth - destX);
			height = Math.Min(height, newHeight - destY);

			var resultBitmap = new System.Drawing.Bitmap(newWidth, newHeight, bitmap.PixelFormat);
			resultBitmap.SetResolution(bitmap.HorizontalResolution, bitmap.VerticalResolution);
			using (var graphics = System.Drawing.Graphics.FromImage(resultBitmap))
			using (var wrapMode = new System.Drawing.Imaging.ImageAttributes())
				graphics.DrawImage(bitmap, new System.Drawing.Rectangle(destX, destY, width, height), new System.Drawing.Rectangle(srcX, srcY, width, height), System.Drawing.GraphicsUnit.Pixel);

			Replace(new List<Range> { FullRange }, new List<string> { Coder.BitmapToString(resultBitmap) });
			SetSelections(new List<Range> { BeginRange });
		}

		void Command_Image_FlipHorizontal() => Flip(System.Drawing.RotateFlipType.RotateNoneFlipX);

		void Command_Image_FlipVertical() => Flip(System.Drawing.RotateFlipType.RotateNoneFlipY);

		ImageRotateDialog.Result Command_Image_Rotate_Dialog() => ImageRotateDialog.Run(WindowParent, GetImageVariables());

		void Command_Image_Rotate(ImageRotateDialog.Result result)
		{
			var variables = GetImageVariables(out var bitmap);
			var angle = new NEExpression(result.AngleExpression).Evaluate<float>(variables, "deg");

			var path = new System.Drawing.Drawing2D.GraphicsPath();
			path.AddRectangle(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height));
			var matrix = new System.Drawing.Drawing2D.Matrix();
			matrix.Rotate(angle);
			var rect = path.GetBounds(matrix);
			var resultBitmap = new System.Drawing.Bitmap((int)rect.Width, (int)rect.Height, bitmap.PixelFormat);
			using (var g = System.Drawing.Graphics.FromImage(resultBitmap))
			{
				g.TranslateTransform(-rect.X, -rect.Y);
				g.RotateTransform(angle);
				g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
				g.DrawImageUnscaled(bitmap, 0, 0);
			}

			Replace(new List<Range> { FullRange }, new List<string> { Coder.BitmapToString(resultBitmap) });
			SetSelections(new List<Range> { BeginRange });
		}
	}
}
