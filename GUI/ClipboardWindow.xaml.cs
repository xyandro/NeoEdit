﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NeoEdit.GUI.Common;

namespace NeoEdit.GUI
{
	public partial class ClipboardWindow : Window
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
				return Text.Replace("\r", " ").Replace("\n", " ").Substring(0, 500);
			}
		}

		static ObservableCollection<ClipboardData> clipboard = new ObservableCollection<ClipboardData>();

		static void Set(ClipboardData data)
		{
			Clipboard.SetDataObject(data.Data, true);
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
			var guid = Clipboard.GetDataObject().GetData(typeof(T));
			if (!(guid is string))
				return null;
			var found = clipboard.FirstOrDefault(data => data.GUID == (string)guid);
			if (found == null)
				return null;
			return found.Contents as T;
		}

		public static void SetFiles(IEnumerable<string> files, bool isCut)
		{
			var objs = new List<string>(files);

			var data = new ClipboardData(objs, String.Join(" ", objs.Select(file => String.Format("\"{0}\"", file))));

			var dropList = new StringCollection();
			objs.ForEach(file => dropList.Add(file));
			data.Data.SetFileDropList(dropList);

			data.Data.SetData("Preferred DropEffect", new MemoryStream(BitConverter.GetBytes((int)(isCut ? DragDropEffects.Move : DragDropEffects.Copy | DragDropEffects.Link))));
			Add(data);
		}

		public static void Set(byte[] bytes, string text)
		{
			Add(new ClipboardData(bytes, text));
		}

		public static void Set(string[] strings)
		{
			Add(new ClipboardData(strings, String.Join(" ", strings)));
		}

		public static bool GetFiles(out List<string> files, out bool isCut)
		{
			files = null;
			isCut = false;

			var dropEffectStream = Clipboard.GetDataObject().GetData("Preferred DropEffect");
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

			var contents = GetContents<List<string>>();
			if (contents != null)
			{
				files = contents;
				return files.Count != 0;
			}

			var dropList = Clipboard.GetFileDropList();
			if ((dropList == null) || (dropList.Count == 0))
				return false;

			files = dropList.Cast<string>().ToList();
			return true;
		}

		public static byte[] GetBytes()
		{
			return GetContents<byte[]>();
		}

		public static string[] GetStrings()
		{
			var contents = GetContents<string[]>();
			if (contents != null)
				return contents;

			var str = Clipboard.GetText();
			if (!String.IsNullOrEmpty(str))
				return new string[] { str };

			var dropList = Clipboard.GetFileDropList();
			if ((dropList != null) && (dropList.Count != 0))
				return dropList.Cast<string>().ToArray();

			return new string[0];
		}

		public static string GetString()
		{
			var str = Clipboard.GetText();
			if (String.IsNullOrEmpty(str))
				return null;
			return str;
		}

		static ClipboardWindow current;
		public static new void Show()
		{
			if (current == null)
				current = new ClipboardWindow();
			current.Focus();
		}

		static ClipboardData Current()
		{
			var guid = Clipboard.GetDataObject().GetData("GUID");
			if (!(guid is string))
				return null;
			return clipboard.FirstOrDefault(data => data.GUID == (string)guid);
		}

		static ClipboardWindow()
		{
			UIHelper<ClipboardWindow>.Register();
			UIHelper<ClipboardWindow>.AddObservableCallback(a => a.Records, (obj, s, e) => obj.items.SelectedItem = Current());
		}

		[DepProp]
		ObservableCollection<ClipboardData> Records { get { return uiHelper.GetPropValue<ObservableCollection<ClipboardData>>(); } set { uiHelper.SetPropValue(value); } }

		readonly UIHelper<ClipboardWindow> uiHelper;
		ClipboardWindow()
		{
			uiHelper = new UIHelper<ClipboardWindow>(this);
			InitializeComponent();

			Records = clipboard;

			Loaded += (s, e) =>
			{
				var item = items.ItemContainerGenerator.ContainerFromItem(items.SelectedItem) as ListBoxItem;
				if (item != null)
					item.Focus();
			};
			KeyDown += (s, e) =>
			{
				if (e.Key == Key.Escape)
					Close();
			};
			items.MouseDoubleClick += (s, e) => ItemClicked();
			items.PreviewKeyDown += (s, e) =>
			{
				if (e.Key == Key.Enter)
				{
					e.Handled = true;
					ItemClicked();
				}
			};
		}

		void ItemClicked()
		{
			var item = items.SelectedItem as ClipboardData;
			Set(item);
			Close();
		}

		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			if (current == this)
				current = null;
		}
	}
}
