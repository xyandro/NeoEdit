using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.NEClipboards;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Converters;
using NeoEdit.GUI.Dialogs;
using NeoEdit.ImageEdit.Dialogs;

namespace NeoEdit.ImageEdit
{
	public class TabsControl : TabsControl<ImageEditor, ImageEditCommand> { }

	partial class ImageEditor
	{
		[DepProp]
		public string FileName { get { return UIHelper<ImageEditor>.GetPropValue<string>(this); } set { UIHelper<ImageEditor>.SetPropValue(this, value); } }
		[DepProp]
		public PixelFormat Format { get { return UIHelper<ImageEditor>.GetPropValue<PixelFormat>(this); } set { UIHelper<ImageEditor>.SetPropValue(this, value); } }
		[DepProp]
		public BitmapSource Image { get { return UIHelper<ImageEditor>.GetPropValue<BitmapSource>(this); } set { UIHelper<ImageEditor>.SetPropValue(this, value); } }
		[DepProp]
		public double Zoom { get { return UIHelper<ImageEditor>.GetPropValue<double>(this); } set { UIHelper<ImageEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int? PosX { get { return UIHelper<ImageEditor>.GetPropValue<int?>(this); } set { UIHelper<ImageEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int? PosY { get { return UIHelper<ImageEditor>.GetPropValue<int?>(this); } set { UIHelper<ImageEditor>.SetPropValue(this, value); } }
		[DepProp]
		public Color? PosColor { get { return UIHelper<ImageEditor>.GetPropValue<Color?>(this); } set { UIHelper<ImageEditor>.SetPropValue(this, value); } }

		const double MinZoom = 0.03125;
		const double MaxZoom = 512;

		Point lastPos;

		static ImageEditor()
		{
			UIHelper<ImageEditor>.Register();
			UIHelper<ImageEditor>.AddCallback(x => x.Image, (obj, o, n) => obj.Format = n.Format);
		}

		public ImageEditor(string fileName, BitmapSource image)
		{
			InitializeComponent();
			FileName = fileName;
			Image = image;
			SetupTabLabel();
			Loaded += (s, e) => Command_View_Zoom_Fit();
		}

		static ImageEditor GetScreenImage()
		{
			using (var screenBmp = new System.Drawing.Bitmap((int)SystemParameters.VirtualScreenWidth, (int)SystemParameters.VirtualScreenHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
			using (var bmpGraphics = System.Drawing.Graphics.FromImage(screenBmp))
			{
				bmpGraphics.CopyFromScreen((int)SystemParameters.VirtualScreenLeft, (int)SystemParameters.VirtualScreenTop, 0, 0, screenBmp.Size);
				var image = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(screenBmp.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
				return new ImageEditor(null, image);
			}
		}

		public NEVariables GetVariables()
		{
			// Can't access DependencyProperties/clipboard from other threads; grab a copy:
			var fileName = FileName;
			var clipboard = NEClipboard.Current.Strings;

			var results = new NEVariables();
			results.Add(NEVariable.Constant("f", "Filename", () => fileName));
			results.Add(NEVariable.Constant("w", "Image width", () => Image.Width));
			results.Add(NEVariable.Constant("h", "Image height", () => Image.Height));
			if (clipboard.Count >= 1)
				results.Add(NEVariable.Constant("c", "Clipboard string", () => clipboard[0]));

			return results;
		}

		static double GetZoom(double value, bool high)
		{
			for (var setZoom = MaxZoom; setZoom >= MinZoom; setZoom /= 2)
				if (setZoom <= value)
				{
					if ((high) && (setZoom != value))
						setZoom *= 2;
					return setZoom;
				}
			return 0;
		}

		void OnMouseMove(object sender, MouseEventArgs e)
		{
			PosX = PosY = null;
			PosColor = null;
			if (image.IsMouseOver)
			{
				var mousePos = e.GetPosition(image);
				if ((mousePos.X >= 0) && (mousePos.X < Image.Width) && (mousePos.Y >= 0) && (mousePos.Y < Image.Height))
				{
					PosX = (int)mousePos.X;
					PosY = (int)mousePos.Y;
					var bitmap = new FormatConvertedBitmap(new CroppedBitmap(Image, new Int32Rect(PosX.Value, PosY.Value, 1, 1)), PixelFormats.Bgra32, null, 0);
					var pixels = new byte[4];
					bitmap.CopyPixels(pixels, pixels.Length, 0);
					PosColor = Color.FromArgb(pixels[3], pixels[2], pixels[1], pixels[0]);
				}
			}

			if (!canvas.IsMouseCaptured)
				return;

			var pos = e.GetPosition(sv);
			var dist = pos - lastPos;
			sv.ScrollToHorizontalOffset(sv.HorizontalOffset - dist.X);
			sv.ScrollToVerticalOffset(sv.VerticalOffset - dist.Y);
			lastPos = pos;
		}

		void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
		{
			lastPos = e.GetPosition(sv);
			canvas.CaptureMouse();
		}

		void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e) => canvas.ReleaseMouseCapture();

		public static IEnumerable<ImageEditor> OpenFile(string fileName)
		{
			if (fileName == null)
			{
				yield return GetScreenImage();
				yield break;
			}

			var decoder = BitmapDecoder.Create(new Uri(fileName), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None);
			var count = decoder.Frames.Count;
			if (count <= 0)
				throw new Exception("No frames available");

			if (count > 1)
			{
				switch (new Message
				{
					Title = "Confirm",
					Text = $"This file has {count} frames.  Load all frames?",
					Options = Message.OptionsEnum.YesNoCancel,
					DefaultAccept = Message.OptionsEnum.Yes,
					DefaultCancel = Message.OptionsEnum.Cancel,
				}.Show())
				{
					case Message.OptionsEnum.No: count = 1; break;
					case Message.OptionsEnum.Cancel: count = 0; break;
				}
			}

			for (var ctr = 0; ctr < count; ++ctr)
				yield return new ImageEditor(fileName, decoder.Frames[ctr]);
		}

		void ResetScroll()
		{
			var dt = new DispatcherTimer();
			dt.Tick += (s, e) =>
			{
				sv.ScrollToHorizontalOffset(Math.Min(sv.ViewportWidth, (sv.ExtentWidth - sv.ViewportWidth) / 2));
				sv.ScrollToVerticalOffset(Math.Min(sv.ViewportHeight, (sv.ExtentHeight - sv.ViewportHeight) / 2));
				dt.Stop();
			};
			dt.Start();
		}

		void SetupTabLabel()
		{
			var multiBinding = new MultiBinding { Converter = new NEExpressionConverter(), ConverterParameter = @"StrFormat(""{0} ({1:0.###})% - NeoEdit Image Editor"", FileName([0]), [1] * 100)" };
			multiBinding.Bindings.Add(new Binding(UIHelper<ImageEditor>.GetProperty(a => a.FileName).Name) { Source = this });
			multiBinding.Bindings.Add(new Binding(UIHelper<ImageEditor>.GetProperty(a => a.Zoom).Name) { Source = this });
			SetBinding(UIHelper<TabsControl<ImageEditor, ImageEditCommand>>.GetProperty(a => a.TabLabel), multiBinding);
		}

		void ZoomInOut(double newZoom)
		{
			var move = image.IsMouseOver;

			newZoom = Math.Max(MinZoom, Math.Min(newZoom, MaxZoom));

			var pos = Mouse.GetPosition(sv);
			var x = (pos.X + sv.HorizontalOffset - Canvas.GetLeft(image)) / Zoom;
			var y = (pos.Y + sv.VerticalOffset - Canvas.GetTop(image)) / Zoom;

			Zoom = newZoom;

			if (move)
			{
				sv.ScrollToHorizontalOffset(x * Zoom + Canvas.GetLeft(image) - pos.X);
				sv.ScrollToVerticalOffset(y * Zoom + Canvas.GetTop(image) - pos.Y);
			}
		}

		public bool GetDialogResult(ImageEditCommand command, out object dialogResult)
		{
			dialogResult = null;

			switch (command)
			{
				case ImageEditCommand.Image_Size: dialogResult = Command_Image_Size_Dialog(); break;
				default: return true;
			}

			return dialogResult != null;
		}

		public void HandleCommand(ImageEditCommand command, bool shiftDown, object dialogResult, bool? multiStatus)
		{
			switch (command)
			{
				case ImageEditCommand.File_Save_Save: Command_File_Save_Save(); break;
				case ImageEditCommand.File_Save_SaveAs: Command_File_Save_SaveAs(); break;
				case ImageEditCommand.File_Close: Command_File_Close(); break;
				case ImageEditCommand.Edit_Copy_Position: Command_Edit_Copy_Position(); break;
				case ImageEditCommand.Edit_Copy_Color: Command_Edit_Copy_Color(); break;
				case ImageEditCommand.Image_Size: Command_Image_Size(dialogResult as ImageSizeDialog.Result); break;
				case ImageEditCommand.Image_Format_Rgb24: Command_Image_Format(PixelFormats.Rgb24); break;
				case ImageEditCommand.Image_Format_Rgb48: Command_Image_Format(PixelFormats.Rgb48); break;
				case ImageEditCommand.Image_Format_Rgba64: Command_Image_Format(PixelFormats.Rgba64); break;
				case ImageEditCommand.Image_Format_Prgba64: Command_Image_Format(PixelFormats.Prgba64); break;
				case ImageEditCommand.Image_Format_Rgb128Float: Command_Image_Format(PixelFormats.Rgb128Float); break;
				case ImageEditCommand.Image_Format_Rgba128Float: Command_Image_Format(PixelFormats.Rgba128Float); break;
				case ImageEditCommand.Image_Format_Prgba128Float: Command_Image_Format(PixelFormats.Prgba128Float); break;
				case ImageEditCommand.Image_Format_Bgr555: Command_Image_Format(PixelFormats.Bgr555); break;
				case ImageEditCommand.Image_Format_Bgr565: Command_Image_Format(PixelFormats.Bgr565); break;
				case ImageEditCommand.Image_Format_Bgr24: Command_Image_Format(PixelFormats.Bgr24); break;
				case ImageEditCommand.Image_Format_Bgr32: Command_Image_Format(PixelFormats.Bgr32); break;
				case ImageEditCommand.Image_Format_Bgr101010: Command_Image_Format(PixelFormats.Bgr101010); break;
				case ImageEditCommand.Image_Format_Bgra32: Command_Image_Format(PixelFormats.Bgra32); break;
				case ImageEditCommand.Image_Format_Pbgra32: Command_Image_Format(PixelFormats.Pbgra32); break;
				case ImageEditCommand.Image_Format_Cmyk32: Command_Image_Format(PixelFormats.Cmyk32); break;
				case ImageEditCommand.Image_Format_BlackWhite: Command_Image_Format(PixelFormats.BlackWhite); break;
				case ImageEditCommand.Image_Format_Indexed1: Command_Image_Format(PixelFormats.Indexed1); break;
				case ImageEditCommand.Image_Format_Indexed2: Command_Image_Format(PixelFormats.Indexed2); break;
				case ImageEditCommand.Image_Format_Indexed4: Command_Image_Format(PixelFormats.Indexed4); break;
				case ImageEditCommand.Image_Format_Indexed8: Command_Image_Format(PixelFormats.Indexed8); break;
				case ImageEditCommand.Image_Format_Gray2: Command_Image_Format(PixelFormats.Gray2); break;
				case ImageEditCommand.Image_Format_Gray4: Command_Image_Format(PixelFormats.Gray4); break;
				case ImageEditCommand.Image_Format_Gray8: Command_Image_Format(PixelFormats.Gray8); break;
				case ImageEditCommand.Image_Format_Gray16: Command_Image_Format(PixelFormats.Gray16); break;
				case ImageEditCommand.Image_Format_Gray32Float: Command_Image_Format(PixelFormats.Gray32Float); break;
				case ImageEditCommand.View_Zoom_Center: Command_View_Zoom_Center(); break;
				case ImageEditCommand.View_Zoom_Fit: Command_View_Zoom_Fit(); break;
				case ImageEditCommand.View_Zoom_100Percent: Command_View_Zoom_100Percent(); break;
				case ImageEditCommand.View_Zoom_ZoomIn: Command_View_Zoom_ZoomIn(); break;
				case ImageEditCommand.View_Zoom_ZoomOut: Command_View_Zoom_ZoomOut(); break;
			}
		}

		void Command_File_Save_Save()
		{
			if (FileName == null)
			{
				Command_File_Save_SaveAs();
				return;
			}

			BitmapEncoder encoder;
			switch (FileTypeExtensions.GetFileType(FileName))
			{
				case FileType.Bitmap: encoder = new BmpBitmapEncoder(); break;
				case FileType.Jpeg: encoder = new JpegBitmapEncoder(); break;
				case FileType.Gif: encoder = new GifBitmapEncoder(); break;
				case FileType.Tiff: encoder = new TiffBitmapEncoder(); break;
				case FileType.Png: encoder = new PngBitmapEncoder(); break;
				default: throw new Exception("Invalid image type");
			}

			encoder.Frames.Add(BitmapFrame.Create(Image));

			using (var output = File.Create(FileName))
				encoder.Save(output);
		}

		void Command_File_Save_SaveAs()
		{
			var dialog = new SaveFileDialog
			{
				Filter = FileTypeExtensions.GetSaveFilter(),
				FilterIndex = FileTypeExtensions.GetSaveFilterIndex(FileName),
				FileName = Path.GetFileName(FileName),
				InitialDirectory = Path.GetDirectoryName(FileName),
			};
			if (dialog.ShowDialog() != true)
				return;

			FileName = dialog.FileName;
			Command_File_Save_Save();
		}

		void Command_File_Close() => TabsParent.Remove(this);

		void Command_Edit_Copy_Position() => NEClipboard.Current = NEClipboard.Create($"{PosX},{PosY}");

		void Command_Edit_Copy_Color() => NEClipboard.Current = NEClipboard.Create(PosColor?.ToString());

		ImageSizeDialog.Result Command_Image_Size_Dialog() => ImageSizeDialog.Run(WindowParent, GetVariables());

		void Command_Image_Size(ImageSizeDialog.Result result)
		{
			var variables = GetVariables();
			var width = new NEExpression(result.ImageWidth).EvaluateRow<int>(variables);
			var height = new NEExpression(result.ImageHeight).EvaluateRow<int>(variables);

			Image = new TransformedBitmap(Image, new ScaleTransform(width / Image.Width, height / Image.Height));
		}

		void Command_Image_Format(PixelFormat format) => Image = new FormatConvertedBitmap(Image, format, null, 0);

		void Command_View_Zoom_Center()
		{
			var pos = Mouse.GetPosition(sv);
			sv.ScrollToHorizontalOffset(sv.HorizontalOffset + pos.X - sv.ViewportWidth / 2);
			sv.ScrollToVerticalOffset(sv.VerticalOffset + pos.Y - sv.ViewportHeight / 2);

			var middle = sv.PointToScreen(new Point { X = sv.ViewportWidth / 2, Y = sv.ViewportHeight / 2 });
			Win32.SetCursorPos((int)middle.X, (int)middle.Y);
		}

		void Command_View_Zoom_Fit()
		{
			Zoom = Math.Max(MinZoom, Math.Min(sv.ViewportWidth / Image.Width, sv.ViewportHeight / Image.Height));
			ResetScroll();
		}


		void Command_View_Zoom_100Percent()
		{
			Zoom = 1;
			ResetScroll();
		}

		void Command_View_Zoom_ZoomIn() => ZoomInOut(GetZoom(Zoom, false) * 2);

		void Command_View_Zoom_ZoomOut() => ZoomInOut(GetZoom(Zoom, true) / 2);
	}

	class ColorToBrushConverter : MarkupExtension, IValueConverter
	{
		ColorToBrushConverter converter;
		public override object ProvideValue(IServiceProvider serviceProvider) => converter = converter ?? new ColorToBrushConverter();

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is Color ? new SolidColorBrush((Color)value) : DependencyProperty.UnsetValue;

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
	}
}
