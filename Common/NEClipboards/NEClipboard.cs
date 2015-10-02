using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;

namespace NeoEdit.Common.NEClipboards
{
	public static class NEClipboard
	{
		public delegate void ClipboardChangedDelegate();
		static ClipboardChangedDelegate clipboardChanged = null;
		public static event ClipboardChangedDelegate ClipboardChanged
		{
			add { clipboardChanged += value; SignalClipboardChanged(); }
			remove { clipboardChanged -= value; }
		}

		class ClipboardData
		{
			public string Text { get; set; }
			public List<string> Strings { get; set; }
			public List<object> Objects { get; set; }
			public bool IsCut { get; set; }
			public int ExternalRevision { get; set; }
		}

		static ClipboardData globalClipboard;
		static int PID = Process.GetCurrentProcess().Id;
		static int ExternalRevision = 0;
		static Type LastType = null;
		static ClipboardChangeNotifier clipboardChangeNotifier = new ClipboardChangeNotifier(() => { while (true) try { FetchExternalClipboard(); break; } catch { Thread.Sleep(100); } });

		static NEClipboard() { FetchExternalClipboard(); }

		static void SignalClipboardChanged()
		{
			if (clipboardChanged != null)
				clipboardChanged();
		}

		static void FetchExternalClipboard()
		{
			var dataObj = Clipboard.GetDataObject();
			if (dataObj.GetData(typeof(NEClipboard)) as int? == PID)
				return;

			++ExternalRevision;

			var text = dataObj.GetData(typeof(string)) as string;
			var strings = new List<string> { text };
			var isCut = false;

			var dropList = Clipboard.GetFileDropList();
			if ((dropList != null) && (dropList.Count != 0))
			{
				strings = dropList.Cast<string>().ToList();
				var dropEffectStream = dataObj.GetData("Preferred DropEffect");
				if (dropEffectStream is MemoryStream)
				{
					try
					{
						var dropEffect = (DragDropEffects)BitConverter.ToInt32(((MemoryStream)dropEffectStream).ToArray(), 0);
						isCut = (dropEffect & DragDropEffects.Move) != DragDropEffects.None;
					}
					catch { }
				}
			}

			globalClipboard = new ClipboardData
			{
				Text = text,
				Strings = strings,
				Objects = strings.Cast<object>().ToList(),
				IsCut = isCut,
				ExternalRevision = ExternalRevision,
			};
			SignalClipboardChanged();
		}

		internal static void SetTextInternal(IClipboardEnabled control, string text)
		{
			var data = new ClipboardData();
			data.Text = text ?? "<NULL>";
			data.Strings = new List<string> { data.Text };
			data.Objects = data.Strings.Cast<object>().ToList();
			SetClipboard(control, data);
		}

		internal static void SetStringsInternal(IClipboardEnabled control, IEnumerable<string> strings)
		{
			var data = new ClipboardData();
			data.Strings = strings.ToList();
			data.Text = String.Join(" ", data.Strings.Select(str => str ?? "<NULL>"));
			data.Objects = data.Strings.Cast<object>().ToList();
			SetClipboard(control, data);
		}

		internal static void SetFileInternal(IClipboardEnabled control, string fileName, bool isCut)
		{
			SetFilesInternal(control, new List<string> { fileName }, isCut);
		}

		internal static void SetFilesInternal(IClipboardEnabled control, IEnumerable<string> fileNames, bool isCut)
		{
			var data = new ClipboardData();
			data.Strings = fileNames.ToList();
			data.Text = String.Join(" ", data.Strings.Select(file => String.Format(@"""{0}""", file)));
			data.Objects = data.Strings.Cast<object>().ToList();

			var dataObj = new DataObject();
			var dropList = new StringCollection();
			dropList.AddRange(data.Strings.ToArray());
			dataObj.SetFileDropList(dropList);
			dataObj.SetData("Preferred DropEffect", new MemoryStream(BitConverter.GetBytes((int)(isCut ? DragDropEffects.Move : DragDropEffects.Copy | DragDropEffects.Link))));

			SetClipboard(control, data, dataObj);
		}

		internal static void SetObjectsInternal(IClipboardEnabled control, IEnumerable<object> objects, string text = null)
		{
			var data = new ClipboardData();
			data.Objects = objects.ToList();
			data.Strings = data.Objects.Select(obj => (obj ?? "<NULL>").ToString()).ToList();
			data.Text = text ?? String.Join(" ", data.Strings);
			SetClipboard(control, data);
		}

		static void SetClipboard(IClipboardEnabled control, ClipboardData data, DataObject dataObj = null)
		{
			var type = control == null ? null : control.GetType();
			if ((type == null) || (LastType != type))
				++ExternalRevision;
			LastType = type;

			data.ExternalRevision = ExternalRevision;
			if ((control != null) && (control.UseLocalClipboard))
				control.LocalClipboardData = data;
			else
				globalClipboard = data;

			dataObj = dataObj ?? new DataObject();
			dataObj.SetText(data.Text);
			dataObj.SetData(typeof(NEClipboard), PID);
			Clipboard.SetDataObject(dataObj, true);
			SignalClipboardChanged();
		}

		static ClipboardData GetData(IClipboardEnabled control)
		{
			var data = globalClipboard;
			if ((control != null) && (control.UseLocalClipboard))
			{
				var local = (control.LocalClipboardData as ClipboardData);
				if ((local != null) && (local.ExternalRevision == ExternalRevision))
					data = local;
			}
			return data;
		}

		internal static string GetTextInternal(IClipboardEnabled control) { return GetData(control).Text; }
		internal static List<string> GetStringsInternal(IClipboardEnabled control) { return GetData(control).Strings; }
		internal static List<object> GetObjectsInternal(IClipboardEnabled control) { return GetData(control).Objects; }


		public static string GetText() { return GetTextInternal(null); }
		public static List<string> GetStrings() { return GetStringsInternal(null); }
		public static List<object> GetObjects() { return GetObjectsInternal(null); }

		public static void SetText(string text) { SetTextInternal(null, text); }
		public static void SetFile(string fileName, bool isCut) { SetFileInternal(null, fileName, isCut); }
		public static void SetFiles(IEnumerable<string> fileNames, bool isCut) { SetFilesInternal(null, fileNames, isCut); }
		public static void SetStrings(IEnumerable<string> strings) { SetStringsInternal(null, strings); }
		public static void SetObjects(IEnumerable<object> objects, string text = null) { SetObjectsInternal(null, objects, text); }

		public static string Text { get { return GetTextInternal(null); } set { SetTextInternal(null, value); } }
		public static string CopiedFile { set { SetFileInternal(null, value, false); } }
		public static string CutFile { set { SetFileInternal(null, value, true); } }
		public static List<string> CopiedFiles { set { SetFilesInternal(null, value, false); } }
		public static List<string> CutFiles { set { SetFilesInternal(null, value, true); } }
		public static List<string> Strings { get { return GetStringsInternal(null); } set { SetStringsInternal(null, value); } }
		public static List<object> Objects { get { return GetObjectsInternal(null); } set { SetObjectsInternal(null, value); } }
	}
}
