using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Transform;
using NeoEdit.MenuImage.Dialogs;

namespace NeoEdit.MenuImage
{
	public static class ImageFunctions
	{
		static string AddColor(string color1, string color2)
		{
			Colorer.StringToARGB(color1, out var alpha1, out var red1, out var green1, out var blue1);
			Colorer.StringToARGB(color2, out var alpha2, out var red2, out var green2, out var blue2);
			alpha1 = (byte)Math.Max(0, Math.Min(alpha1 + alpha2, 255));
			red1 = (byte)Math.Max(0, Math.Min(red1 + red2, 255));
			green1 = (byte)Math.Max(0, Math.Min(green1 + green2, 255));
			blue1 = (byte)Math.Max(0, Math.Min(blue1 + blue2, 255));
			return Colorer.ARGBToString(alpha1, red1, green1, blue1);
		}

		static string AdjustColor(string color, double multiplier, bool doAlpha, bool doRed, bool doGreen, bool doBlue)
		{
			Colorer.StringToARGB(color, out var alpha, out var red, out var green, out var blue);
			if (doAlpha)
				alpha = (byte)Math.Max(0, Math.Min((int)(alpha * multiplier + 0.5), 255));
			if (doRed)
				red = (byte)Math.Max(0, Math.Min((int)(red * multiplier + 0.5), 255));
			if (doGreen)
				green = (byte)Math.Max(0, Math.Min((int)(green * multiplier + 0.5), 255));
			if (doBlue)
				blue = (byte)Math.Max(0, Math.Min((int)(blue * multiplier + 0.5), 255));
			return Colorer.ARGBToString(alpha, red, green, blue);
		}

		static string OverlayColor(string color1, string color2)
		{
			Colorer.StringToARGB(color1, out byte alpha1, out byte red1, out byte green1, out byte blue1);
			Colorer.StringToARGB(color2, out byte alpha2, out byte red2, out byte green2, out byte blue2);
			red1 = (byte)((red1 * alpha1 / 255) + (red2 * alpha2 * (255 - alpha1) / (255 * 255)));
			green1 = (byte)((green1 * alpha1 / 255) + (green2 * alpha2 * (255 - alpha1) / (255 * 255)));
			blue1 = (byte)((blue1 * alpha1 / 255) + (blue2 * alpha2 * (255 - alpha1) / (255 * 255)));
			alpha1 = (byte)(alpha1 + (alpha2 * (255 - alpha1) / 255));
			return Colorer.ARGBToString(alpha1, red1, green1, blue1);
		}

		static void Flip(ITextEditor te, System.Drawing.RotateFlipType type)
		{
			var bitmap = Coder.StringToBitmap(te.AllText);
			bitmap.RotateFlip(type);
			te.Replace(new List<Range> { te.FullRange }, new List<string> { Coder.BitmapToString(bitmap) });
			te.SetSelections(new List<Range> { te.BeginRange });
		}

		static void SplitGIF(string fileName, string outputTemplate)
		{
			using (var image = System.Drawing.Image.FromFile(fileName))
			{
				var dimension = new System.Drawing.Imaging.FrameDimension(image.FrameDimensionsList[0]);
				var frameCount = image.GetFrameCount(dimension);
				for (var frame = 0; frame < frameCount; ++frame)
				{
					image.SelectActiveFrame(dimension, frame);
					image.Save(string.Format(outputTemplate, frame + 1), System.Drawing.Imaging.ImageFormat.Png);
				}
			}
		}

		static public ImageGrabColorDialog.Result Command_Image_GrabColor_Dialog(ITextEditor te) => ImageGrabColorDialog.Run(te.WindowParent, te.Selections.Select(range => te.GetString(range)).FirstOrDefault());

		static public void Command_Image_GrabColor(ITextEditor te, ImageGrabColorDialog.Result result) => te.ReplaceSelections(result.Color);

		static public ImageGrabImageDialog.Result Command_Image_GrabImage_Dialog(ITextEditor te) => ImageGrabImageDialog.Run(te.WindowParent, te.GetVariables());

		static public void Command_Image_GrabImage(ITextEditor te, ImageGrabImageDialog.Result result)
		{
			var variables = te.GetVariables();
			var x = new NEExpression(result.GrabX).EvaluateList<int>(variables, te.Selections.Count());
			var y = new NEExpression(result.GrabY).EvaluateList<int>(variables, te.Selections.Count());
			var width = new NEExpression(result.GrabWidth).EvaluateList<int>(variables, te.Selections.Count());
			var height = new NEExpression(result.GrabHeight).EvaluateList<int>(variables, te.Selections.Count());

			var strs = new List<string>();
			for (var ctr = 0; ctr < x.Count; ++ctr)
			{
				using (var image = new System.Drawing.Bitmap(width[ctr], height[ctr], System.Drawing.Imaging.PixelFormat.Format32bppArgb))
				{
					using (var dest = System.Drawing.Graphics.FromImage(image))
						dest.CopyFromScreen(x[ctr], y[ctr], 0, 0, image.Size);
					strs.Add(Coder.BitmapToString(image));
				}
			}
			te.ReplaceSelections(strs);
		}

		static public ImageAdjustColorDialog.Result Command_Image_AdjustColor_Dialog(ITextEditor te) => ImageAdjustColorDialog.Run(te.WindowParent, te.GetVariables());

		static public void Command_Image_AdjustColor(ITextEditor te, ImageAdjustColorDialog.Result result)
		{
			var results = te.GetFixedExpressionResults<double>(result.Expression);
			var strs = te.Selections.AsParallel().AsOrdered().Select((range, index) => AdjustColor(te.GetString(range), results[index], result.Alpha, result.Red, result.Green, result.Blue)).ToList();
			te.ReplaceSelections(strs);
		}

		static public ImageAddOverlayColorDialog.Result Command_Image_AddOverlayColor_Dialog(ITextEditor te, bool add) => ImageAddOverlayColorDialog.Run(te.WindowParent, add, te.GetVariables());

		static public void Command_Image_AddColor(ITextEditor te, ImageAddOverlayColorDialog.Result result)
		{
			var results = te.GetFixedExpressionResults<string>(result.Expression);
			var strs = te.Selections.AsParallel().AsOrdered().Select((range, index) => AddColor(te.GetString(range), results[index])).ToList();
			te.ReplaceSelections(strs);
		}

		static public void Command_Image_OverlayColor(ITextEditor te, ImageAddOverlayColorDialog.Result result)
		{
			var results = te.GetFixedExpressionResults<string>(result.Expression);
			var strs = te.Selections.AsParallel().AsOrdered().Select((range, index) => OverlayColor(results[index], te.GetString(range))).ToList();
			te.ReplaceSelections(strs);
		}

		static public ImageSizeDialog.Result Command_Image_Size_Dialog(ITextEditor te) => ImageSizeDialog.Run(te.WindowParent, te.GetVariables());

		static public void Command_Image_Size(ITextEditor te, ImageSizeDialog.Result result)
		{
			var variables = te.GetVariables();
			var width = new NEExpression(result.WidthExpression).Evaluate<int>(variables);
			var height = new NEExpression(result.HeightExpression).Evaluate<int>(variables);

			var bitmap = te.GetBitmap();
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

			te.Replace(new List<Range> { te.FullRange }, new List<string> { Coder.BitmapToString(resultBitmap) });
			te.SetSelections(new List<Range> { te.BeginRange });
		}

		static public ImageCropDialog.Result Command_Image_Crop_Dialog(ITextEditor te) => ImageCropDialog.Run(te.WindowParent, te.GetVariables());

		static public void Command_Image_Crop(ITextEditor te, ImageCropDialog.Result result)
		{
			var variables = te.GetVariables();
			var destX = new NEExpression(result.XExpression).Evaluate<int>(variables);
			var destY = new NEExpression(result.YExpression).Evaluate<int>(variables);
			var newWidth = new NEExpression(result.WidthExpression).Evaluate<int>(variables);
			var newHeight = new NEExpression(result.HeightExpression).Evaluate<int>(variables);
			if ((newWidth <= 0) || (newHeight <= 0))
				throw new Exception("Width and height must be greater than 0");

			var bitmap = te.GetBitmap();
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
			{
				graphics.FillRectangle(new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb((int)Colorer.StringToValue(result.FillColor))), new System.Drawing.Rectangle(System.Drawing.Point.Empty, resultBitmap.Size));
				graphics.DrawImage(bitmap, new System.Drawing.Rectangle(destX, destY, width, height), new System.Drawing.Rectangle(srcX, srcY, width, height), System.Drawing.GraphicsUnit.Pixel);
			}

			te.Replace(new List<Range> { te.FullRange }, new List<string> { Coder.BitmapToString(resultBitmap) });
			te.SetSelections(new List<Range> { te.BeginRange });
		}

		static public void Command_Image_FlipHorizontal(ITextEditor te) => Flip(te, System.Drawing.RotateFlipType.RotateNoneFlipX);

		static public void Command_Image_FlipVertical(ITextEditor te) => Flip(te, System.Drawing.RotateFlipType.RotateNoneFlipY);

		static public ImageRotateDialog.Result Command_Image_Rotate_Dialog(ITextEditor te) => ImageRotateDialog.Run(te.WindowParent, te.GetVariables());

		static public void Command_Image_Rotate(ITextEditor te, ImageRotateDialog.Result result)
		{
			var variables = te.GetVariables();
			var angle = new NEExpression(result.AngleExpression).Evaluate<float>(variables, "deg");

			var bitmap = te.GetBitmap();
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

			te.Replace(new List<Range> { te.FullRange }, new List<string> { Coder.BitmapToString(resultBitmap) });
			te.SetSelections(new List<Range> { te.BeginRange });
		}

		static public ImageGIFAnimateDialog.Result Command_Image_GIF_Animate_Dialog(ITextEditor te) => ImageGIFAnimateDialog.Run(te.WindowParent, te.GetVariables());

		static public void Command_Image_GIF_Animate(ITextEditor te, ImageGIFAnimateDialog.Result result)
		{
			var variables = te.GetVariables();
			var inputFiles = new NEExpression(result.InputFiles).EvaluateList<string>(variables);
			var outputFile = new NEExpression(result.OutputFile).Evaluate<string>(variables);
			var delays = new NEExpression(result.Delay).EvaluateList<int>(variables, inputFiles.Count, "ms");
			var repeat = new NEExpression(result.Repeat).Evaluate<int>(variables);

			using (var writer = new GIFWriter(outputFile, repeat))
				for (var ctr = 0; ctr < inputFiles.Count; ++ctr)
					using (var image = System.Drawing.Image.FromFile(inputFiles[ctr]))
						writer.WriteFrame(image, delays[ctr]);
		}

		static public ImageGIFSplitDialog.Result Command_Image_GIF_Split_Dialog(ITextEditor te)
		{
			var variables = te.GetVariables();
			variables.Add(NEVariable.Constant("chunk", "Chunk number", 1));
			return ImageGIFSplitDialog.Run(te.WindowParent, variables);
		}

		static public void Command_Image_GIF_Split(ITextEditor te, ImageGIFSplitDialog.Result result)
		{
			var variables = te.GetVariables();
			variables.Add(NEVariable.Constant("chunk", "Chunk number", "{0}"));
			var files = te.RelativeSelectedFiles();
			var outputTemplates = new NEExpression(result.OutputTemplate).EvaluateList<string>(variables, files.Count);
			Enumerable.Range(0, files.Count).AsParallel().ForEach(index => SplitGIF(files[index], outputTemplates[index]));
		}
	}
}
