using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Models;
using NeoEdit.Common.Transform;

namespace NeoEdit.Program
{
	partial class Tab
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

		void Flip(System.Drawing.RotateFlipType type)
		{
			var bitmap = Coder.StringToBitmap(Text.GetString());
			bitmap.RotateFlip(type);
			Replace(new List<Range> { Range.FromIndex(0, Text.Length) }, new List<string> { Coder.BitmapToString(bitmap) });
			Selections = new List<Range> { new Range() };
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

		object Configure_Image_GrabColor() => state.TabsWindow.RunImageGrabColorDialog(Selections.Select(range => Text.GetString(range)).FirstOrDefault());

		void Execute_Image_GrabColor()
		{
			var result = state.Configuration as ImageGrabColorDialogResult;
			ReplaceOneWithMany(result.Colors, true);
		}

		object Configure_Image_GrabImage() => state.TabsWindow.RunImageGrabImageDialog(GetVariables());

		void Execute_Image_GrabImage()
		{
			var result = state.Configuration as ImageGrabImageDialogResult;
			var variables = GetVariables();
			var x = state.GetExpression(result.GrabX).EvaluateList<int>(variables, Selections.Count());
			var y = state.GetExpression(result.GrabY).EvaluateList<int>(variables, Selections.Count());
			var width = state.GetExpression(result.GrabWidth).EvaluateList<int>(variables, Selections.Count());
			var height = state.GetExpression(result.GrabHeight).EvaluateList<int>(variables, Selections.Count());

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
			ReplaceSelections(strs);
		}

		object Configure_Image_AdjustColor() => state.TabsWindow.RunImageAdjustColorDialog(GetVariables());

		void Execute_Image_AdjustColor()
		{
			var result = state.Configuration as ImageAdjustColorDialogResult;
			var results = GetExpressionResults<double>(result.Expression, Selections.Count());
			var strs = Selections.AsParallel().AsOrdered().Select((range, index) => AdjustColor(Text.GetString(range), results[index], result.Alpha, result.Red, result.Green, result.Blue)).ToList();
			ReplaceSelections(strs);
		}

		object Configure_Image_AddOverlayColor(bool add) => state.TabsWindow.RunImageAddOverlayColorDialog(add, GetVariables());

		void Execute_Image_AddColor()
		{
			var result = state.Configuration as ImageAddOverlayColorDialogResult;
			var results = GetExpressionResults<string>(result.Expression, Selections.Count());
			var strs = Selections.AsParallel().AsOrdered().Select((range, index) => AddColor(Text.GetString(range), results[index])).ToList();
			ReplaceSelections(strs);
		}

		void Execute_Image_OverlayColor()
		{
			var result = state.Configuration as ImageAddOverlayColorDialogResult;
			var results = GetExpressionResults<string>(result.Expression, Selections.Count());
			var strs = Selections.AsParallel().AsOrdered().Select((range, index) => OverlayColor(results[index], Text.GetString(range))).ToList();
			ReplaceSelections(strs);
		}

		object Configure_Image_Size() => state.TabsWindow.RunImageSizeDialog(GetVariables());

		void Execute_Image_Size()
		{
			var result = state.Configuration as ImageSizeDialogResult;
			var variables = GetVariables();
			var width = state.GetExpression(result.WidthExpression).Evaluate<int>(variables);
			var height = state.GetExpression(result.HeightExpression).Evaluate<int>(variables);

			var bitmap = GetBitmap();
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

			Replace(new List<Range> { Range.FromIndex(0, Text.Length) }, new List<string> { Coder.BitmapToString(resultBitmap) });
			Selections = new List<Range> { new Range() };
		}

		object Configure_Image_Crop() => state.TabsWindow.RunImageCropDialog(GetVariables());

		void Execute_Image_Crop()
		{
			var result = state.Configuration as ImageCropDialogResult;
			var variables = GetVariables();
			var destX = state.GetExpression(result.XExpression).Evaluate<int>(variables);
			var destY = state.GetExpression(result.YExpression).Evaluate<int>(variables);
			var newWidth = state.GetExpression(result.WidthExpression).Evaluate<int>(variables);
			var newHeight = state.GetExpression(result.HeightExpression).Evaluate<int>(variables);
			if ((newWidth <= 0) || (newHeight <= 0))
				throw new Exception("Width and height must be greater than 0");

			var bitmap = GetBitmap();
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

			Replace(new List<Range> { Range.FromIndex(0, Text.Length) }, new List<string> { Coder.BitmapToString(resultBitmap) });
			Selections = new List<Range> { new Range() };
		}

		void Execute_Image_FlipHorizontal() => Flip(System.Drawing.RotateFlipType.RotateNoneFlipX);

		void Execute_Image_FlipVertical() => Flip(System.Drawing.RotateFlipType.RotateNoneFlipY);

		object Configure_Image_Rotate() => state.TabsWindow.RunImageRotateDialog(GetVariables());

		void Execute_Image_Rotate()
		{
			var result = state.Configuration as ImageRotateDialogResult;
			var variables = GetVariables();
			var angle = state.GetExpression(result.AngleExpression).Evaluate<float>(variables, "deg");

			var bitmap = GetBitmap();
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

			Replace(new List<Range> { Range.FromIndex(0, Text.Length) }, new List<string> { Coder.BitmapToString(resultBitmap) });
			Selections = new List<Range> { new Range() };
		}

		object Configure_Image_GIF_Animate() => state.TabsWindow.RunImageGIFAnimateDialog(GetVariables());

		void Execute_Image_GIF_Animate()
		{
			var result = state.Configuration as ImageGIFAnimateDialogResult;
			var variables = GetVariables();
			var inputFiles = state.GetExpression(result.InputFiles).EvaluateList<string>(variables);
			var outputFile = state.GetExpression(result.OutputFile).Evaluate<string>(variables);
			var delays = state.GetExpression(result.Delay).EvaluateList<int>(variables, inputFiles.Count, "ms");
			var repeat = state.GetExpression(result.Repeat).Evaluate<int>(variables);

			using (var writer = new GIFWriter(outputFile, repeat))
				for (var ctr = 0; ctr < inputFiles.Count; ++ctr)
					using (var image = System.Drawing.Image.FromFile(inputFiles[ctr]))
						writer.WriteFrame(image, delays[ctr]);
		}

		object Configure_Image_GIF_Split()
		{
			var variables = GetVariables();
			variables.Add(NEVariable.Constant("chunk", "Chunk number", 1));
			return state.TabsWindow.RunImageGIFSplitDialog(variables);
		}

		void Execute_Image_GIF_Split()
		{
			var result = state.Configuration as ImageGIFSplitDialogResult;
			var variables = GetVariables();
			variables.Add(NEVariable.Constant("chunk", "Chunk number", "{0}"));
			var files = RelativeSelectedFiles();
			var outputTemplates = state.GetExpression(result.OutputTemplate).EvaluateList<string>(variables, files.Count);
			Enumerable.Range(0, files.Count).ForEach(index => SplitGIF(files[index], outputTemplates[index]));
		}
	}
}
