using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using NeoEdit.Common;
using NeoEdit.Common.Transform;

namespace NeoEdit.Program.NEClipboards
{
	public class NEClipboard : IEnumerable<IReadOnlyList<string>>
	{
		public delegate void ClipboardChangedDelegate();
		public static event ClipboardChangedDelegate ClipboardChanged;

		static NEClipboard currentClipboard = null, lastClipboard = null;

		static ClipboardChangeNotifier clipboardChangeNotifier = new ClipboardChangeNotifier(() =>
		{
			currentClipboard = null;
			for (var ctr = 0; ctr < 5; ++ctr)
				if (GetSystem() == null)
					Thread.Sleep(100);
				else
					break;
		});

		public static NEClipboard Current { get => GetSystem(); set => value.SetSystem(); }

		static readonly int PID = Process.GetCurrentProcess().Id;

		List<IReadOnlyList<string>> stringLists = new List<IReadOnlyList<string>>();
		public bool? IsCut { get; set; } = null;

		public void Add(IReadOnlyList<string> items) => stringLists.Add(items);
		public int Count => stringLists.Count;
		public int ChildCount => stringLists.Sum(list => list.Count);

		public string String => string.Join("\r\n", Strings);
		public IReadOnlyList<string> Strings => stringLists.SelectMany().ToList();

		static NEClipboard GetSystem()
		{
			try
			{
				if (currentClipboard != null)
					return currentClipboard;

				var dataObj = Clipboard.GetDataObject();
				if (dataObj.GetData(typeof(NEClipboard)) as int? == PID)
				{
					currentClipboard = lastClipboard;
					return currentClipboard;
				}

				var result = new NEClipboard();
				var list = new List<string>();
				result.Add(list);

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
					list.AddRange(dropList);
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

					list.Add(str);
				}

				currentClipboard = result;
				try { ClipboardChanged?.Invoke(); } catch { }
				return currentClipboard;
			}
			catch { return null; }
		}

		void SetSystem()
		{
			try
			{
				if (this == currentClipboard)
					return;

				var dataObj = new DataObject();

				dataObj.SetText(String, TextDataFormat.UnicodeText);
				dataObj.SetData(typeof(NEClipboard), PID);

				if (IsCut.HasValue)
				{
					var dropList = new StringCollection();
					dropList.AddRange(Strings.ToArray());
					dataObj.SetFileDropList(dropList);
					dataObj.SetData("Preferred DropEffect", new MemoryStream(BitConverter.GetBytes((int)(IsCut == true ? DragDropEffects.Move : DragDropEffects.Copy | DragDropEffects.Link))));
				}

				Clipboard.SetDataObject(dataObj, true);
				currentClipboard = lastClipboard = this;
				try { ClipboardChanged?.Invoke(); } catch { }
			}
			catch { }
		}

		public IEnumerator<IReadOnlyList<string>> GetEnumerator() => stringLists.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
