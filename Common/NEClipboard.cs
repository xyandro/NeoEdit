using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
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
		static ClipboardChangedDelegate clipboardChanged = null;
		public static event ClipboardChangedDelegate ClipboardChanged
		{
			add
			{
				clipboardChanged += value;
				OnClipboardChanged();
			}
			remove { clipboardChanged -= value; }
		}

		public static object Data { get; private set; }
		public static string Text { get; private set; }
		public static bool? IsCut { get; private set; }
		public static object Extra { get; private set; }

		static int PID = Process.GetCurrentProcess().Id;

		public static List<string> Strings
		{
			get { return Data as List<string> ?? (Text != null ? new List<string> { Text } : new List<string>()); }
			set { Set(value, String.Join(" ", value)); }
		}

		static void SetClipboard(DataObject dataObj)
		{
			dataObj.SetData(typeof(NEClipboard), PID);
			Clipboard.SetDataObject(dataObj, true);
			if (clipboardChanged != null)
				clipboardChanged();
		}

		public static void Set(object data, string text = null, object extra = null)
		{
			if (text == null)
				text = data.ToString();

			Data = data;
			Text = text;
			IsCut = null;
			Extra = extra;

			var dataObj = new DataObject();
			dataObj.SetText(text);
			SetClipboard(dataObj);
		}

		public static void SetFiles(IEnumerable<string> files, bool isCut, object extra = null)
		{
			Data = files.ToList();
			Text = String.Join(" ", files.Select(file => String.Format("\"{0}\"", file)));
			IsCut = isCut;
			Extra = extra;

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

			if (dataObj.GetData(typeof(NEClipboard)) as int? == PID) // Local change
				return;

			Data = Text = dataObj.GetData(typeof(string)) as string;
			IsCut = null;
			Extra = null;

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

			if (clipboardChanged != null)
				clipboardChanged();
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
