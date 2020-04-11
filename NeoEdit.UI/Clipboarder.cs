using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;
using NeoEdit.Common;
using NeoEdit.Common.Transform;

namespace NeoEdit.UI
{
	public class Clipboarder
	{
		[DesignerCategory("Code")]
		class ClipboardChangeNotifier : System.Windows.Forms.Form
		{
			readonly Action callback;
			public ClipboardChangeNotifier(Action callback)
			{
				this.callback = callback;
				IntPtr HWND_MESSAGE = new IntPtr(-3);
				SetParent(Handle, HWND_MESSAGE);
				AddClipboardFormatListener(Handle);
			}

			protected override void WndProc(ref System.Windows.Forms.Message m)
			{
				const int WM_CLIPBOARDUPDATE = 0x031D;
				if (m.Msg == WM_CLIPBOARDUPDATE)
					callback();
				base.WndProc(ref m);
			}

			[DllImport("user32.dll", SetLastError = true)]
			static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

			[DllImport("user32.dll", SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			static extern bool AddClipboardFormatListener(IntPtr hwnd);
		}
		static ClipboardChangeNotifier clipboardChangeNotifier = new ClipboardChangeNotifier(() => NEClipboard.System = null);

		static readonly int PID = Process.GetCurrentProcess().Id;
		static NEClipboard lastClipboard = null;

		static public void GetSystem()
		{
			try
			{
				var dataObj = Clipboard.GetDataObject();
				if (dataObj.GetData(typeof(NEClipboard)) as int? == PID)
				{
					NEClipboard.System = NEClipboard.Current = lastClipboard;
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

				NEClipboard.System = result;
			}
			catch { NEClipboard.System = new NEClipboard(); }
			NEClipboard.Current = NEClipboard.System;
		}

		static public void SetSystem()
		{
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
				lastClipboard = NEClipboard.Current;
				NEClipboard.System = NEClipboard.Current;
			}
			catch { }
		}
	}
}
