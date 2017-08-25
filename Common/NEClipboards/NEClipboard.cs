using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;

namespace NeoEdit.Common.NEClipboards
{
	public class NEClipboard : IEnumerable<NEClipboardList>
	{
		public delegate void ClipboardChangedDelegate();
		public static event ClipboardChangedDelegate ClipboardChanged;

		static ClipboardChangeNotifier clipboardChangeNotifier = new ClipboardChangeNotifier(() => { while (true) try { FetchSystemClipboard(); break; } catch { Thread.Sleep(100); } });
		static NEClipboard() { FetchSystemClipboard(); }

		static void FetchSystemClipboard()
		{
			var system = GetSystem();
			if (system == null)
				return;
			current = system;
			ClipboardChanged?.Invoke();
		}

		static NEClipboard current;
		public static NEClipboard Current
		{
			get { return current; }
			set
			{
				if (current == value)
					return;
				current = value;
				current.SetSystem();
				ClipboardChanged?.Invoke();
			}
		}

		static readonly int PID = Process.GetCurrentProcess().Id;

		List<NEClipboardList> neClipboardLists;
		public bool? IsCut { get; set; } = null;

		public NEClipboard()
		{
			neClipboardLists = new List<NEClipboardList>();
		}

		public void Add(NEClipboardList items) => neClipboardLists.Add(items);
		public int Count => neClipboardLists.Count;
		public int ChildCount => neClipboardLists.Sum(list => list.Count);

		public NEClipboardList this[int index] => neClipboardLists[index];

		public string String => string.Join(" ", Strings);
		public List<string> Strings => neClipboardLists.SelectMany(list => list.Strings).ToList();
		public List<object> Objects => neClipboardLists.SelectMany(list => list.Objects).ToList();
		public List<BitmapSource> Images => neClipboardLists.SelectMany(list => list.Images).ToList();

		public static NEClipboard CreateString(string text, bool? isCut = null) => CreateStrings(new List<string> { text }, isCut);
		public static NEClipboard CreateStrings(IEnumerable<string> strings, bool? isCut = null) => CreateStrings(new List<IEnumerable<string>> { strings }, isCut);
		public static NEClipboard CreateStrings(IEnumerable<IEnumerable<string>> strings, bool? isCut = null) => new NEClipboard() { neClipboardLists = strings.Select(list => NEClipboardList.CreateStrings(list)).ToList(), IsCut = isCut };

		public static NEClipboard CreateObject(object obj, string text = null) => CreateObjects(new List<object> { obj }, text);
		public static NEClipboard CreateObjects(IEnumerable<object> objects, string text = null) => CreateObjects(new List<IEnumerable<object>> { objects }, text);
		public static NEClipboard CreateObjects(IEnumerable<IEnumerable<object>> objects, string text = null) => new NEClipboard() { neClipboardLists = objects.Select(list => NEClipboardList.CreateObjects(list, text)).ToList() };

		public static NEClipboard CreateImage(BitmapSource image) => CreateImages(new List<BitmapSource> { image });
		public static NEClipboard CreateImages(IEnumerable<BitmapSource> images) => CreateImages(new List<IEnumerable<BitmapSource>> { images });
		public static NEClipboard CreateImages(IEnumerable<IEnumerable<BitmapSource>> images) => new NEClipboard() { neClipboardLists = images.Select(list => NEClipboardList.CreateImages(list)).ToList() };

		static NEClipboard GetSystem()
		{
			var dataObj = Clipboard.GetDataObject();
			if (dataObj.GetData(typeof(NEClipboard)) as int? == PID)
				return null;

			var image = dataObj.GetData(DataFormats.Bitmap, true) as BitmapSource;
			if (image != null)
				return CreateImage(image);

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
				return CreateStrings(dropList, isCut);
			}

			var str = dataObj.GetData(DataFormats.UnicodeText) as string ?? dataObj.GetData(DataFormats.OemText) as string ?? dataObj.GetData(DataFormats.Text) as string ?? dataObj.GetData(typeof(string)) as string;
			return CreateString(str ?? "");
		}

		void SetSystem()
		{
			var dataObj = new DataObject();

			dataObj.SetText(string.Join(" ", neClipboardLists.SelectMany().Select(item => item.Text)), TextDataFormat.UnicodeText);
			dataObj.SetData(typeof(NEClipboard), PID);

			if (IsCut.HasValue)
			{
				var dropList = new StringCollection();
				dropList.AddRange(neClipboardLists.SelectMany().Select(item => item.Text).ToArray());
				dataObj.SetFileDropList(dropList);
				dataObj.SetData("Preferred DropEffect", new MemoryStream(BitConverter.GetBytes((int)(IsCut == true ? DragDropEffects.Move : DragDropEffects.Copy | DragDropEffects.Link))));
			}

			var image = neClipboardLists.SelectMany().Select(item => item.Data).OfType<BitmapSource>().FirstOrDefault();
			if (image != null)
				dataObj.SetImage(image);

			Clipboard.SetDataObject(dataObj, true);
		}

		public IEnumerator<NEClipboardList> GetEnumerator() => neClipboardLists.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
