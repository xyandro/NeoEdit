using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows;
using NeoEdit.Records;
using NeoEdit.Records.Disk;

namespace NeoEdit
{
	class Clipboard
	{
		public static Clipboard Current { get; private set; }
		static Clipboard()
		{
			Current = new Clipboard();
		}

		object contents;
		string contentGUID;

		public void Set(IEnumerable<Record> records, bool isCut)
		{
			var objs = new List<Record>(records);

			contents = objs;
			contentGUID = Guid.NewGuid().ToString();

			var dataObj = new DataObject();
			dataObj.SetData(objs.GetType(), contentGUID);
			dataObj.SetText(String.Join(" ", records.Select(record => String.Format("\"{0}\"", record.FullName))));

			if (objs.Any(a => a is DiskRecord))
			{
				var dropList = new StringCollection();
				objs.ForEach(record => dropList.Add(record.FullName));
				dataObj.SetFileDropList(dropList);
			}

			dataObj.SetData("Preferred DropEffect", new MemoryStream(BitConverter.GetBytes((int)(isCut ? DragDropEffects.Move : DragDropEffects.Copy | DragDropEffects.Link))));
			System.Windows.Clipboard.SetDataObject(dataObj, true);
		}

		public bool GetRecords(out List<Record> records, out bool isCut)
		{
			records = null;
			isCut = false;

			var dataObj = System.Windows.Clipboard.GetDataObject();

			var dropEffectStream = dataObj.GetData("Preferred DropEffect");
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

			var guid = dataObj.GetData(typeof(List<Record>));
			if ((guid is string) && (((string)guid).Equals(contentGUID)))
			{
				records = contents as List<Record>;
				return records.Count != 0;
			}

			var dropList = System.Windows.Clipboard.GetFileDropList();
			if ((dropList == null) || (dropList.Count == 0))
				return false;

			records = dropList.Cast<string>().ToList().Select(file => new DiskRoot().GetRecord(file)).ToList();
			return true;
		}

		public void Set(byte[] bytes, string text)
		{
			contents = bytes;
			contentGUID = Guid.NewGuid().ToString();

			var dataObj = new DataObject();
			dataObj.SetData(bytes.GetType(), contentGUID);
			dataObj.SetText(text);
			System.Windows.Clipboard.SetDataObject(dataObj, true);
		}

		public byte[] GetBytes()
		{
			var dataObj = System.Windows.Clipboard.GetDataObject();
			var guid = dataObj.GetData(typeof(byte[]));
			if ((guid is string) && (((string)guid).Equals(contentGUID)))
				return contents as byte[];

			return null;
		}

		public void Set(string[] strings)
		{
			contents = strings;
			contentGUID = Guid.NewGuid().ToString();

			var dataObj = new DataObject();
			dataObj.SetData(strings.GetType(), contentGUID);
			dataObj.SetText(String.Join(" ", strings));
			System.Windows.Clipboard.SetDataObject(dataObj, true);
		}

		public string[] GetStrings()
		{
			var dataObj = System.Windows.Clipboard.GetDataObject();
			var guid = dataObj.GetData(typeof(string[]));
			if ((guid is string) && (((string)guid).Equals(contentGUID)))
				return contents as string[];

			var str = System.Windows.Clipboard.GetText();
			if (String.IsNullOrEmpty(str))
				return new string[0];

			return new string[] { str };
		}

		public string GetString()
		{
			var str = System.Windows.Clipboard.GetText();
			if (String.IsNullOrEmpty(str))
				return null;
			return str;
		}
	}
}
