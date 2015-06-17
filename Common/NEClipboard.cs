using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace NeoEdit.Common
{
	public static class NEClipboard
	{
		public delegate void ClipboardChangedDelegate();
		public static event ClipboardChangedDelegate ClipboardChanged;

		public static object Data { get; private set; }
		public static List<string> Strings
		{
			get { return Data as List<string> ?? (Text != null ? new List<string> { Text } : new List<string>()); }
			set { Set(value, String.Join(" ", value)); }
		}
		public static string Text { get; private set; }
		public static bool? IsCut { get; private set; }

		static void SetClipboard(DataObject dataObj)
		{
			dataObj.SetData(typeof(NEClipboard), true);
			Clipboard.SetDataObject(dataObj, true);
			if (ClipboardChanged != null)
				ClipboardChanged();
		}

		public static void Set(object data, string text)
		{
			Data = data;
			Text = text;
			IsCut = null;

			var dataObj = new DataObject();
			dataObj.SetText(text);
			SetClipboard(dataObj);
		}

		public static void SetFiles(IEnumerable<string> files, bool isCut)
		{
			Data = files.ToList();
			Text = String.Join(" ", files.Select(file => String.Format("\"{0}\"", file)));
			IsCut = isCut;

			var dataObj = new DataObject();

			var dropList = new StringCollection();
			dropList.AddRange(files.ToArray());
			dataObj.SetFileDropList(dropList);

			dataObj.SetData("Preferred DropEffect", new MemoryStream(BitConverter.GetBytes((int)(isCut ? DragDropEffects.Move : DragDropEffects.Copy | DragDropEffects.Link))));
			dataObj.SetText(Text);
			SetClipboard(dataObj);
		}

		static void OnClipboardChanged()
		{
			var dataObj = Clipboard.GetDataObject();

			var isLocal = dataObj.GetData(typeof(NEClipboard)) as bool?;
			if (isLocal == true)
				return;

			Data = Text = dataObj.GetData(typeof(string)) as string;
			IsCut = null;

			var dropList = Clipboard.GetFileDropList();
			if ((dropList != null) && (dropList.Count != 0))
			{
				Data = dropList.Cast<string>().ToList();
				var dropEffectStream = dataObj.GetData("Preferred DropEffect");
				if (dropEffectStream is MemoryStream)
				{
					try
					{
						var dropEffect = (DragDropEffects)BitConverter.ToInt32(((MemoryStream)dropEffectStream).ToArray(), 0);
						IsCut = (dropEffect & DragDropEffects.Move) != DragDropEffects.None;
					}
					catch { }
				}
			}

			if (ClipboardChanged != null)
				ClipboardChanged();
		}

		static ClipboardChangedNotifier clipboardChangedNotifier = new ClipboardChangedNotifier();
		class ClipboardChangedNotifier : System.Windows.Forms.Form
		{
			public ClipboardChangedNotifier()
			{
				IntPtr HWND_MESSAGE = new IntPtr(-3);
				SetParent(Handle, HWND_MESSAGE);
				AddClipboardFormatListener(Handle);
			}

			protected override void WndProc(ref System.Windows.Forms.Message m)
			{
				const int WM_CLIPBOARDUPDATE = 0x031D;
				if (m.Msg == WM_CLIPBOARDUPDATE)
				{
					while (true)
						try
						{
							OnClipboardChanged();
							break;
						}
						catch
						{
							Thread.Sleep(100);
						}
				}
				base.WndProc(ref m);
			}
		}

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool AddClipboardFormatListener(IntPtr hwnd);
	}
}
