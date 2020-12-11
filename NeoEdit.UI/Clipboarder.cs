using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using NeoEdit.Common;
using NeoEdit.Common.Transform;

namespace NeoEdit.UI
{
	public class Clipboarder
	{
		class ClipboardChangeNotifier
		{
			readonly Action callback;
			public ClipboardChangeNotifier(Action callback)
			{
				this.callback = callback;
				IntPtr HWND_MESSAGE = new IntPtr(-3);
				var hwndSource = new HwndSource(0, 0, 0, 0, 0, 0, 0, null, HWND_MESSAGE);
				hwndSource.AddHook(WndProc);
				AddClipboardFormatListener(hwndSource.Handle);
			}

			IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
			{
				const int WM_CLIPBOARDUPDATE = 0x031D;
				if (msg == WM_CLIPBOARDUPDATE)
					callback();
				//base.WndProc(ref m);
				return IntPtr.Zero;
			}

			[DllImport("user32.dll", SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			static extern bool AddClipboardFormatListener(IntPtr hwnd);
		}
		static ClipboardChangeNotifier clipboardChangeNotifier;

		public static void Initialize()
		{
			// Must be called from dispatcher thread to set up listener properly
			clipboardChangeNotifier = new ClipboardChangeNotifier(() => clipboardChanged = true);
		}

		static readonly int PID = Process.GetCurrentProcess().Id;
		static bool clipboardChanged = true;
		static NEClipboard systemClipboard;

		public static void GetSystem()
		{
			if (!clipboardChanged)
				return;

			try
			{
				var dataObj = Clipboard.GetDataObject();

				if (dataObj == null)
					return;

				if (dataObj.GetData(typeof(NEClipboard)) as int? == PID)
				{
					clipboardChanged = false;
					return;
				}

				var result = new NEClipboard();

				var dropList = (dataObj.GetData(DataFormats.FileDrop) as string[])?.OrderBy(Helpers.SmartComparer(false)).ToList();
				if ((dropList != null) && (dropList.Count != 0))
				{
					var isCut = false;
					var dropEffectStream = dataObj.GetData("Preferred DropEffect");
					if (dropEffectStream is MemoryStream)
					{
						try
						{
							var dropEffect = (DragDropEffects)BitConverter.ToInt32(((MemoryStream)dropEffectStream).ToArray(), 0);
							isCut = dropEffect.HasFlag(DragDropEffects.Move);
						}
						catch { }
					}
					result.Add(new List<string>(dropList));
					result.IsCut = isCut;
				}
				else
				{
					var str = dataObj.GetData(DataFormats.UnicodeText) as string ?? dataObj.GetData(DataFormats.OemText) as string ?? dataObj.GetData(DataFormats.Text) as string ?? dataObj.GetData(typeof(string)) as string;

					if ((str == null) && (dataObj.GetData(DataFormats.Bitmap, true) is BitmapSource image))
					{
						var bmp = new Bitmap(image.PixelWidth, image.PixelHeight, PixelFormat.Format32bppPArgb);
						var data = bmp.LockBits(new Rectangle(System.Drawing.Point.Empty, bmp.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
						image.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
						bmp.UnlockBits(data);
						str = Coder.BitmapToString(bmp);
					}

					result.Add(new List<string> { str });
				}

				NEClipboard.Current = systemClipboard = result;
				clipboardChanged = false;
			}
			catch { }
		}

		public static void SetSystem()
		{
			if ((clipboardChanged) || (NEClipboard.Current == systemClipboard))
				return;

			try
			{
				var dataObj = new DataObject();

				dataObj.SetText(NEClipboard.Current.String, TextDataFormat.UnicodeText);
				dataObj.SetData(typeof(NEClipboard), PID);

				if (NEClipboard.Current.IsCut.HasValue)
				{
					var dropList = new StringCollection();
					dropList.AddRange(NEClipboard.Current.Strings.ToArray());
					dataObj.SetFileDropList(dropList);
					dataObj.SetData("Preferred DropEffect", new MemoryStream(BitConverter.GetBytes((int)(NEClipboard.Current.IsCut == true ? DragDropEffects.Move : DragDropEffects.Copy | DragDropEffects.Link))));
				}

				Clipboard.SetDataObject(dataObj, true);
				systemClipboard = NEClipboard.Current;
			}
			catch { }
		}
	}
}
