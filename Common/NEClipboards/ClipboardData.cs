using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace NeoEdit.Common.NEClipboards
{
	class ClipboardData
	{
		static readonly int PID = Process.GetCurrentProcess().Id;

		public string Text { get; private set; } = "";
		public List<string> Strings { get; private set; } = new List<string>();
		public List<object> Objects { get; private set; } = new List<object>();
		public bool? IsCut { get; private set; } = null;

		public ClipboardData() { }

		public static ClipboardData CreateText(string text)
		{
			var data = new ClipboardData();
			data.Text = text ?? "<NULL>";
			data.Strings = new List<string> { data.Text };
			data.Objects = data.Strings.Cast<object>().ToList();
			return data;
		}

		public static ClipboardData CreateStrings(IEnumerable<string> strings)
		{
			var data = new ClipboardData();
			data.Strings = strings.ToList();
			data.Text = string.Join("\r\n", data.Strings.Select(str => str ?? "<NULL>"));
			data.Objects = data.Strings.Cast<object>().ToList();
			return data;
		}

		public static ClipboardData CreateFile(string fileName, bool isCut) => CreateFiles(new List<string> { fileName }, isCut);

		public static ClipboardData CreateFiles(IEnumerable<string> fileNames, bool isCut)
		{
			var data = new ClipboardData();
			data.Strings = fileNames.ToList();
			data.Text = string.Join(" ", data.Strings.Select(file => string.Format(@"""{0}""", file)));
			data.Objects = data.Strings.Cast<object>().ToList();
			data.IsCut = isCut;
			return data;
		}

		public static ClipboardData CreateObjects(IEnumerable<object> objects, string text = null)
		{
			var data = new ClipboardData();
			data.Objects = objects.ToList();
			data.Strings = data.Objects.Select(obj => (obj ?? "<NULL>").ToString()).ToList();
			data.Text = text ?? string.Join(" ", data.Strings);
			return data;
		}

		public static ClipboardData GetSystem()
		{
			var dataObj = Clipboard.GetDataObject();
			if (dataObj.GetData(typeof(NELocalClipboard)) as int? == PID)
				return null;

			var data = new ClipboardData();
			data.Text = dataObj.GetData(DataFormats.UnicodeText) as string ?? dataObj.GetData(DataFormats.OemText) as string ?? dataObj.GetData(DataFormats.Text) as string ?? dataObj.GetData(typeof(string)) as string;
			data.Strings = new List<string>();
			if (data.Text == null)
				data.Text = "";
			else
				data.Strings.Add(data.Text);

			var dropList = (dataObj.GetData(DataFormats.FileDrop) as string[])?.OrderBy(Helpers.SmartComparer(false)).ToList();
			if ((dropList != null) && (dropList.Count != 0))
			{
				data.Strings = dropList;
				var dropEffectStream = dataObj.GetData("Preferred DropEffect");
				if (dropEffectStream is MemoryStream)
				{
					try
					{
						var dropEffect = (DragDropEffects)BitConverter.ToInt32(((MemoryStream)dropEffectStream).ToArray(), 0);
						data.IsCut = dropEffect.HasFlag(DragDropEffects.Move);
					}
					catch { }
				}
			}

			return data;
		}

		public void SetSystem()
		{
			var dataObj = new DataObject();

			dataObj.SetText(Text, TextDataFormat.UnicodeText);
			dataObj.SetData(typeof(NELocalClipboard), PID);

			if (IsCut.HasValue)
			{
				var dropList = new StringCollection();
				dropList.AddRange(Strings.ToArray());
				dataObj.SetFileDropList(dropList);
				dataObj.SetData("Preferred DropEffect", new MemoryStream(BitConverter.GetBytes((int)(IsCut == true ? DragDropEffects.Move : DragDropEffects.Copy | DragDropEffects.Link))));
			}

			Clipboard.SetDataObject(dataObj, true);
		}
	}
}
