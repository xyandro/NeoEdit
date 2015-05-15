using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows;

namespace NeoEdit.Common
{
	public static class NEClipboard
	{
		public class ClipboardData
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
				return Text.Replace("\r", " ").Replace("\n", " ").Substring(0, Math.Min(Text.Length, 500));
			}
		}

		static readonly ObservableCollection<ClipboardData> history = new ObservableCollection<ClipboardData>();
		static public ObservableCollection<ClipboardData> History { get { return history; } }

		const int maxClipboard = 20;
		static void Add(ClipboardData data)
		{
			// Set current first so when on history's ObservableCollection events fire it's set
			Current = data;
			history.Insert(0, data);
			while (history.Count > maxClipboard)
				history.RemoveAt(history.Count - 1);
		}

		static T GetContents<T>() where T : class
		{
			var guid = Clipboard.GetDataObject().GetData(typeof(T));
			if (!(guid is string))
				return null;
			var found = history.FirstOrDefault(data => data.GUID == (string)guid);
			if (found == null)
				return null;
			return found.Contents as T;
		}

		static public void SetFiles(List<string> files, bool isCut)
		{
			var data = new ClipboardData(files, String.Join(" ", files.Select(file => String.Format("\"{0}\"", file))));

			var dropList = new StringCollection();
			dropList.AddRange(files.ToArray());
			data.Data.SetFileDropList(dropList);

			data.Data.SetData("Preferred DropEffect", new MemoryStream(BitConverter.GetBytes((int)(isCut ? DragDropEffects.Move : DragDropEffects.Copy | DragDropEffects.Link))));
			Add(data);
		}

		static public void SetBinary(byte[] bytes, string text)
		{
			Add(new ClipboardData(bytes, text));
		}

		static public void SetStrings(List<string> strings)
		{
			Add(new ClipboardData(strings, String.Join(" ", strings)));
		}

		static public bool GetFiles(out List<string> files, out bool isCut)
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
			if ((dropList != null) && (dropList.Count != 0))
			{
				files = dropList.Cast<string>().ToList();
				return true;
			}

			return false;
		}

		static public byte[] GetBytes()
		{
			return GetContents<byte[]>();
		}

		static public List<string> GetStrings()
		{
			List<string> files;
			bool isCut;
			if (GetFiles(out files, out isCut))
				return files;

			var str = GetString();
			if (str != null)
				return new List<string> { str };

			return new List<string>();
		}

		static public string GetString()
		{
			var str = Clipboard.GetText();
			if (String.IsNullOrEmpty(str))
				return null;
			return str;
		}

		static public ClipboardData Current
		{
			get
			{
				var guid = Clipboard.GetDataObject().GetData("GUID");
				if (!(guid is string))
					return null;
				return history.FirstOrDefault(data => data.GUID == (string)guid);
			}

			set
			{
				Clipboard.SetDataObject(value.Data, true);
			}
		}
	}
}
