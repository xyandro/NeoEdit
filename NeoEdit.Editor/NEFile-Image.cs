using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;
using NeoEdit.TaskRunning;

namespace NeoEdit.Editor
{
	partial class NEFile
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

		DateTime? GetImageTakenDate(string fileName)
		{
			using (var image = new System.Drawing.Bitmap(fileName))
			{
				var dateTaken = image.PropertyItems.Where(a => a.Id == 0x9004).FirstOrDefault();
				if (dateTaken == null)
					dateTaken = image.PropertyItems.Where(a => a.Id == 0x9003).FirstOrDefault();
				if (dateTaken == null)
					return null;

				var str = Encoding.UTF8.GetString(dateTaken.Value, 0, dateTaken.Value.Length - 1);
				var originalDate = DateTime.ParseExact(str, "yyyy:MM:dd HH:mm:ss", null);
				return originalDate;
			}
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

		void SetImageTakenDate(string fileName, DateTime dateTime)
		{
			string tempName;
			using (System.Drawing.Image image = new System.Drawing.Bitmap(fileName))
			{
				var bytes = Encoding.UTF8.GetBytes(dateTime.ToString("yyyy:MM:dd HH:mm:ss") + '\0');

				var newItem = (System.Drawing.Imaging.PropertyItem)FormatterServices.GetUninitializedObject(typeof(System.Drawing.Imaging.PropertyItem));
				newItem.Id = 0x9004;
				newItem.Value = bytes;
				newItem.Len = bytes.Length;
				newItem.Type = 2;
				image.SetPropertyItem(newItem);

				newItem = (System.Drawing.Imaging.PropertyItem)FormatterServices.GetUninitializedObject(typeof(System.Drawing.Imaging.PropertyItem));
				newItem.Id = 0x9003;
				newItem.Value = bytes;
				newItem.Len = bytes.Length;
				newItem.Type = 2;
				image.SetPropertyItem(newItem);

				tempName = Path.Combine(Path.GetDirectoryName(fileName), $"{Guid.NewGuid()}{Path.GetExtension(fileName)}");
				image.Save(tempName);
			}
			File.Delete(fileName);
			File.Move(tempName, fileName);
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

		static void Configure_Image_Resize() => EditorExecuteState.CurrentState.Configuration = EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_Image_Resize(EditorExecuteState.CurrentState.NEFiles.Focused.GetVariables());

		void Execute_Image_Resize()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_Image_Resize;
			var variables = GetVariables();
			var width = EditorExecuteState.CurrentState.GetExpression(result.WidthExpression).Evaluate<int>(variables);
			var height = EditorExecuteState.CurrentState.GetExpression(result.HeightExpression).Evaluate<int>(variables);

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

		static void Configure_Image_Crop() => EditorExecuteState.CurrentState.Configuration = EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_Image_Crop(EditorExecuteState.CurrentState.NEFiles.Focused.GetVariables());

		void Execute_Image_Crop()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_Image_Crop;
			var variables = GetVariables();
			var destX = EditorExecuteState.CurrentState.GetExpression(result.XExpression).Evaluate<int>(variables);
			var destY = EditorExecuteState.CurrentState.GetExpression(result.YExpression).Evaluate<int>(variables);
			var newWidth = EditorExecuteState.CurrentState.GetExpression(result.WidthExpression).Evaluate<int>(variables);
			var newHeight = EditorExecuteState.CurrentState.GetExpression(result.HeightExpression).Evaluate<int>(variables);
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

		static void Configure_Image_GrabColor() => EditorExecuteState.CurrentState.Configuration = EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_Image_GrabColor(EditorExecuteState.CurrentState.NEFiles.Focused.Selections.Select(range => EditorExecuteState.CurrentState.NEFiles.Focused.Text.GetString(range)).FirstOrDefault());

		void Execute_Image_GrabColor()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_Image_GrabColor;
			ReplaceOneWithMany(result.Colors, true);
		}

		static void Configure_Image_GrabImage() => EditorExecuteState.CurrentState.Configuration = EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_Image_GrabImage(EditorExecuteState.CurrentState.NEFiles.Focused.GetVariables());

		void Execute_Image_GrabImage()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_Image_GrabImage;
			var variables = GetVariables();
			var x = EditorExecuteState.CurrentState.GetExpression(result.GrabX).EvaluateList<int>(variables, Selections.Count());
			var y = EditorExecuteState.CurrentState.GetExpression(result.GrabY).EvaluateList<int>(variables, Selections.Count());
			var width = EditorExecuteState.CurrentState.GetExpression(result.GrabWidth).EvaluateList<int>(variables, Selections.Count());
			var height = EditorExecuteState.CurrentState.GetExpression(result.GrabHeight).EvaluateList<int>(variables, Selections.Count());

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

		static void Configure_Image_AddOverlayColor(bool add) => EditorExecuteState.CurrentState.Configuration = EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_Image_AddOverlayColor(add, EditorExecuteState.CurrentState.NEFiles.Focused.GetVariables());

		void Execute_Image_AddColor()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_Image_AddOverlayColor;
			var results = GetExpressionResults<string>(result.Expression, Selections.Count());
			var strs = Selections.AsTaskRunner().Select((range, index) => AddColor(Text.GetString(range), results[index])).ToList();
			ReplaceSelections(strs);
		}

		static void Configure_Image_AdjustColor() => EditorExecuteState.CurrentState.Configuration = EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_Image_AdjustColor(EditorExecuteState.CurrentState.NEFiles.Focused.GetVariables());

		void Execute_Image_AdjustColor()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_Image_AdjustColor;
			var results = GetExpressionResults<double>(result.Expression, Selections.Count());
			var strs = Selections.AsTaskRunner().Select((range, index) => AdjustColor(Text.GetString(range), results[index], result.Alpha, result.Red, result.Green, result.Blue)).ToList();
			ReplaceSelections(strs);
		}

		void Execute_Image_OverlayColor()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_Image_AddOverlayColor;
			var results = GetExpressionResults<string>(result.Expression, Selections.Count());
			var strs = Selections.AsTaskRunner().Select((range, index) => OverlayColor(results[index], Text.GetString(range))).ToList();
			ReplaceSelections(strs);
		}

		void Execute_Image_FlipHorizontal() => Flip(System.Drawing.RotateFlipType.RotateNoneFlipX);

		void Execute_Image_FlipVertical() => Flip(System.Drawing.RotateFlipType.RotateNoneFlipY);

		static void Configure_Image_Rotate() => EditorExecuteState.CurrentState.Configuration = EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_Image_Rotate(EditorExecuteState.CurrentState.NEFiles.Focused.GetVariables());

		void Execute_Image_Rotate()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_Image_Rotate;
			var variables = GetVariables();
			var angle = EditorExecuteState.CurrentState.GetExpression(result.AngleExpression).Evaluate<float>(variables, "deg");

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

		static void Configure_Image_GIF_Animate() => EditorExecuteState.CurrentState.Configuration = EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_Image_GIF_Animate(EditorExecuteState.CurrentState.NEFiles.Focused.GetVariables());

		void Execute_Image_GIF_Animate()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_Image_GIF_Animate;
			var variables = GetVariables();
			var inputFiles = EditorExecuteState.CurrentState.GetExpression(result.InputFiles).EvaluateList<string>(variables);
			var outputFile = EditorExecuteState.CurrentState.GetExpression(result.OutputFile).Evaluate<string>(variables);
			var delays = EditorExecuteState.CurrentState.GetExpression(result.Delay).EvaluateList<int>(variables, inputFiles.Count, "ms");
			var repeat = EditorExecuteState.CurrentState.GetExpression(result.Repeat).Evaluate<int>(variables);

			using (var writer = new GIFWriter(outputFile, repeat))
				for (var ctr = 0; ctr < inputFiles.Count; ++ctr)
					using (var image = System.Drawing.Image.FromFile(inputFiles[ctr]))
						writer.WriteFrame(image, delays[ctr]);
		}

		static void Configure_Image_GIF_Split()
		{
			var variables = EditorExecuteState.CurrentState.NEFiles.Focused.GetVariables();
			variables.Add(NEVariable.Constant("chunk", "Chunk number", 1));
			EditorExecuteState.CurrentState.Configuration = EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_Image_GIF_Split(variables);
		}

		void Execute_Image_GIF_Split()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_Image_GIF_Split;
			var variables = GetVariables();
			variables.Add(NEVariable.Constant("chunk", "Chunk number", "{0}"));
			var files = RelativeSelectedFiles();
			var outputTemplates = EditorExecuteState.CurrentState.GetExpression(result.OutputTemplate).EvaluateList<string>(variables, files.Count);
			Enumerable.Range(0, files.Count).ForEach(index => SplitGIF(files[index], outputTemplates[index]));
		}

		void Execute_Image_GetTakenDate() => ReplaceSelections(RelativeSelectedFiles().AsTaskRunner().Select(fileName => GetImageTakenDate(fileName)?.ToString() ?? "<NONE>").ToList());

		static void Configure_Image_SetTakenDate() => EditorExecuteState.CurrentState.Configuration = EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_Image_SetTakenDate(EditorExecuteState.CurrentState.NEFiles.Focused.GetVariables());

		void Execute_Image_SetTakenDate()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_Image_SetTakenDate;
			var variables = GetVariables();

			var fileNameExpression = EditorExecuteState.CurrentState.GetExpression(result.FileName);
			var dateTimeExpression = EditorExecuteState.CurrentState.GetExpression(result.DateTime);
			var resultCount = variables.ResultCount(fileNameExpression, dateTimeExpression);

			var fileNames = fileNameExpression.EvaluateList<string>(variables, resultCount);
			var dateTimes = dateTimeExpression.EvaluateList<string>(variables, resultCount).Select(DateTime.Parse).ToList();

			const int InvalidCount = 10;
			var invalid = fileNames.Distinct().Where(name => !Helpers.FileOrDirectoryExists(name)).Take(InvalidCount).ToList();
			if (invalid.Any())
				throw new Exception($"Files don't exist:\n{string.Join("\n", invalid)}");

			TaskRunner.Range(0, fileNames.Count).Select(index => (fileName: fileNames[index], dateTime: dateTimes[index])).ForEach(pair => SetImageTakenDate(pair.fileName, pair.dateTime));
		}
	}
}
