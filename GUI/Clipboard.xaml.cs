using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.Records;
using NeoEdit.GUI.Records.Disk;

namespace NeoEdit.GUI
{
	public partial class Clipboard : Window
	{
		class ClipboardData
		{
			public string GUID { get; private set; }
			public string Text { get; private set; }
			public object Contents { get; private set; }
			public DataObject Data { get; private set; }

			public ClipboardData(object contents, string text)
			{
				GUID = Guid.NewGuid().ToString();
				Contents = contents;
				Text = text;
				Data = new DataObject();
				Data.SetData("GUID", GUID);
				Data.SetData(Contents.GetType(), GUID);
				Data.SetText(Text);
			}

			public override string ToString()
			{
				return Text.Replace("\r", " ").Replace("\n", " ");
			}
		}

		static ObservableCollection<ClipboardData> clipboard = new ObservableCollection<ClipboardData>();

		static void Set(ClipboardData data)
		{
			System.Windows.Clipboard.SetDataObject(data.Data, true);
		}

		const int maxClipboard = 20;
		static void Add(ClipboardData data)
		{
			Set(data);
			clipboard.Insert(0, data);
			while (clipboard.Count > maxClipboard)
				clipboard.RemoveAt(clipboard.Count - 1);
		}

		static T GetContents<T>() where T : class
		{
			var guid = System.Windows.Clipboard.GetDataObject().GetData(typeof(T));
			if (!(guid is string))
				return null;
			var found = clipboard.First(data => data.GUID == (string)guid);
			if (found == null)
				return null;
			return found.Contents as T;
		}

		static public void Set(IEnumerable<Record> records, bool isCut)
		{
			var objs = new List<Record>(records);

			var data = new ClipboardData(objs, String.Join(" ", objs.Select(record => String.Format("\"{0}\"", record.FullName))));

			if (objs.Any(a => a is DiskRecord))
			{
				var dropList = new StringCollection();
				objs.ForEach(record => dropList.Add(record.FullName));
				data.Data.SetFileDropList(dropList);
			}

			data.Data.SetData("Preferred DropEffect", new MemoryStream(BitConverter.GetBytes((int)(isCut ? DragDropEffects.Move : DragDropEffects.Copy | DragDropEffects.Link))));
			Add(data);
		}

		static public void Set(byte[] bytes, string text)
		{
			Add(new ClipboardData(bytes, text));
		}

		static public void Set(string[] strings)
		{
			Add(new ClipboardData(strings, String.Join(" ", strings)));
		}

		static public bool GetRecords(out List<Record> records, out bool isCut)
		{
			records = null;
			isCut = false;

			var dropEffectStream = System.Windows.Clipboard.GetDataObject().GetData("Preferred DropEffect");
			if (dropEffectStream is MemoryStream)
			{
				try
				{
					var dropEffect = (DragDropEffects)BitConverter.ToInt32(((MemoryStream)dropEffectStream).ToArray(), 0);
					if ((dropEffect & DragDropEffects.Move) != DragDropEffects.None)
						isCut = true;
				}
				catch { }
			}

			var contents = GetContents<List<Record>>();
			if (contents != null)
			{
				records = contents;
				return records.Count != 0;
			}

			var dropList = System.Windows.Clipboard.GetFileDropList();
			if ((dropList == null) || (dropList.Count == 0))
				return false;

			records = dropList.Cast<string>().ToList().Select(file => new DiskRoot().GetRecord(file)).ToList();
			return true;
		}

		static public byte[] GetBytes()
		{
			return GetContents<byte[]>();
		}

		static public string[] GetStrings()
		{
			var contents = GetContents<string[]>();
			if (contents != null)
				return contents;

			var str = System.Windows.Clipboard.GetText();
			if (String.IsNullOrEmpty(str))
				return new string[0];

			return new string[] { str };
		}

		static public string GetString()
		{
			var str = System.Windows.Clipboard.GetText();
			if (String.IsNullOrEmpty(str))
				return null;
			return str;
		}

		static Clipboard current;
		static public new void Show()
		{
			if (current == null)
				current = new Clipboard();
			current.Focus();
		}

		static ClipboardData Current()
		{
			var guid = System.Windows.Clipboard.GetDataObject().GetData("GUID");
			if (!(guid is string))
				return null;
			return clipboard.First(data => data.GUID == (string)guid);
		}

		static Clipboard() { UIHelper<Clipboard>.Register(); }

		[DepProp]
		ObservableCollection<ClipboardData> Records { get { return uiHelper.GetPropValue<ObservableCollection<ClipboardData>>(); } set { uiHelper.SetPropValue(value); } }

		readonly UIHelper<Clipboard> uiHelper;
		Clipboard()
		{
			uiHelper = new UIHelper<Clipboard>(this);
			InitializeComponent();

			uiHelper.AddObservableCallback(a => a.Records, () => items.SelectedItem = Current());

			Records = clipboard;

			items.MouseDoubleClick += (s, e) => ItemClicked();
			items.PreviewKeyDown += (s, e) =>
			{
				if (e.Key == Key.Enter)
					ItemClicked();
			};

			base.Show();
		}

		void ItemClicked()
		{
			var item = items.SelectedItem as ClipboardData;
			Set(item);
		}

		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			if (current == this)
				current = null;
		}
	}
}
