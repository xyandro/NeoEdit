using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using NeoEdit.Common;
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
			System.Windows.Clipboard.SetDataObject(dataObj);
		}

		public void GetRecords(out List<Record> records, out bool isCut)
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
				return;
			}

			var dropList = System.Windows.Clipboard.GetFileDropList();
			if ((dropList == null) || (dropList.Count == 0))
				return;

			records = dropList.Cast<string>().ToList().Select(file => DiskRoot.Static.GetRecord(file)).ToList();
		}

		public void Set(BinaryData bytes, bool hex)
		{
			contents = bytes;
			contentGUID = Guid.NewGuid().ToString();

			var dataObj = new DataObject();
			dataObj.SetData(bytes.GetType(), contentGUID);
			var str = "";
			if (hex)
				str = bytes.ToHexString();
			else
			{
				var sw = new StringBuilder((int)bytes.Length);
				for (var ctr = 0; ctr < bytes.Length; ctr++)
					sw.Append((char)bytes[ctr]);
				str = sw.ToString();
			}
			dataObj.SetText(str);
			System.Windows.Clipboard.SetDataObject(dataObj);
		}

		public BinaryData GetBinaryData(BinaryData.EncodingName encoding)
		{
			var dataObj = System.Windows.Clipboard.GetDataObject();
			var guid = dataObj.GetData(typeof(BinaryData));
			if ((guid is string) && (((string)guid).Equals(contentGUID)))
				return contents as BinaryData;

			var str = System.Windows.Clipboard.GetText();
			if (String.IsNullOrEmpty(str))
				return null;

			return BinaryData.FromString(encoding, str);
		}
	}
}
