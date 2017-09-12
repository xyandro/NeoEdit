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

		public static NEClipboard Create(string str, bool? isCut = null) => Create(new List<string> { str }, isCut);
		public static NEClipboard Create(IEnumerable<string> strings, bool? isCut = null) => Create(new List<IEnumerable<string>> { strings }, isCut);
		public static NEClipboard Create(IEnumerable<IEnumerable<string>> strings, bool? isCut = null) => new NEClipboard() { neClipboardLists = strings.Select(list => NEClipboardList.Create(list)).ToList(), IsCut = isCut };

		public static NEClipboard Create(object obj) => Create(new List<object> { obj });
		public static NEClipboard Create(IEnumerable<object> objects) => Create(new List<IEnumerable<object>> { objects });
		public static NEClipboard Create(IEnumerable<IEnumerable<object>> objects) => new NEClipboard() { neClipboardLists = objects.Select(list => NEClipboardList.Create(list)).ToList() };

		public static NEClipboard Create(BitmapSource image) => Create(new List<BitmapSource> { image });
		public static NEClipboard Create(IEnumerable<BitmapSource> images) => Create(new List<IEnumerable<BitmapSource>> { images });
		public static NEClipboard Create(IEnumerable<IEnumerable<BitmapSource>> images) => new NEClipboard() { neClipboardLists = images.Select(list => NEClipboardList.Create(list)).ToList() };

		static NEClipboard GetSystem()
		{
			var dataObj = Clipboard.GetDataObject();
			if (dataObj.GetData(typeof(NEClipboard)) as int? == PID)
				return null;

			var result = new NEClipboard();
			var list = new NEClipboardList();
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
				list.Add(dropList.Select(str => NEClipboardItem.Create(str)));
				result.IsCut = isCut;
			}
			else
			{
				var str = dataObj.GetData(DataFormats.UnicodeText) as string ?? dataObj.GetData(DataFormats.OemText) as string ?? dataObj.GetData(DataFormats.Text) as string ?? dataObj.GetData(typeof(string)) as string;
				list.Add(NEClipboardItem.Create(str));
			}

			var image = dataObj.GetData(DataFormats.Bitmap, true) as BitmapSource;
			if (image != null)
				list.Add(NEClipboardItem.Create(image));

			return result;
		}

		void SetSystem()
		{
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

			var image = Images.FirstOrDefault();
			if (image != null)
				dataObj.SetImage(image);

			Clipboard.SetDataObject(dataObj, true);
		}

		public IEnumerator<NEClipboardList> GetEnumerator() => neClipboardLists.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
