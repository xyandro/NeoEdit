using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace NeoEdit.Common.NEClipboards
{
	public class NELocalClipboard
	{
		public delegate void ClipboardChangedDelegate();
		ClipboardChangedDelegate clipboardChanged = null;
		public event ClipboardChangedDelegate ClipboardChanged
		{
			add { clipboardChanged += value; clipboardChanged(); }
			remove { clipboardChanged -= value; }
		}

		static WeakList<NELocalClipboard> clipboards = new WeakList<NELocalClipboard>();
		static ClipboardChangeNotifier clipboardChangeNotifier = new ClipboardChangeNotifier(() => { while (true) try { FetchSystemClipboard(); break; } catch { Thread.Sleep(100); } });

		static ClipboardData systemClipboard;
		ClipboardData localClipboard;

		static NELocalClipboard() { FetchSystemClipboard(); }

		public NELocalClipboard()
		{
			localClipboard = systemClipboard ?? new ClipboardData();
			clipboards.Add(this);
		}

		static void FetchSystemClipboard()
		{
			var clipboard = ClipboardData.GetSystem();
			if (clipboard == null)
				return;
			Save(null, clipboard, true, false);
		}

		static void Save(NELocalClipboard control, ClipboardData data, bool global = true, bool setClipboard = true)
		{
			systemClipboard = data;

			var controls = global ? clipboards.AsEnumerable() : new List<NELocalClipboard> { control };
			controls.NonNull().ForEach(clipboard => { clipboard.localClipboard = data; clipboard.clipboardChanged?.Invoke(); });

			if (setClipboard)
				data.SetSystem();
		}

		public void SetText(string text, bool global = true, bool setClipboard = true) => Save(this, ClipboardData.CreateText(text), global, setClipboard);
		public void SetStrings(IEnumerable<string> strings, bool global = true, bool setClipboard = true) => Save(this, ClipboardData.CreateStrings(strings), global, setClipboard);
		public void SetFile(string fileName, bool isCut, bool global = true, bool setClipboard = true) => Save(this, ClipboardData.CreateFile(fileName, isCut), global, setClipboard);
		public void SetFiles(IEnumerable<string> fileNames, bool isCut, bool global = true, bool setClipboard = true) => Save(this, ClipboardData.CreateFiles(fileNames, isCut), global, setClipboard);
		public void SetObjects(IEnumerable<object> objects, string text = null, bool global = true, bool setClipboard = true) => Save(this, ClipboardData.CreateObjects(objects, text), global, setClipboard);

		public string Text => localClipboard.Text;
		public List<string> Strings => localClipboard.Strings;
		public List<object> Objects => localClipboard.Objects;

		public string LocalText { set { SetText(value, false); } }
		public IEnumerable<string> LocalStrings { set { SetStrings(value, false); } }
		public string LocalCopiedFile { set { SetFile(value, false, false); } }
		public string LocalCutFile { set { SetFile(value, true, false); } }
		public IEnumerable<string> LocalCopiedFiles { set { SetFiles(value, false, false); } }
		public IEnumerable<string> LocalCutFiles { set { SetFiles(value, true, false); } }
		public IEnumerable<object> LocalObjects { set { SetObjects(value, global: false); } }
		public string GlobalText { set { SetText(value, true); } }
		public IEnumerable<string> GlobalStrings { set { SetStrings(value, true); } }
		public string GlobalCopiedFile { set { SetFile(value, false, true); } }
		public string GlobalCutFile { set { SetFile(value, true, true); } }
		public IEnumerable<string> GlobalCopiedFiles { set { SetFiles(value, false, true); } }
		public IEnumerable<string> GlobalCutFiles { set { SetFiles(value, true, true); } }
		public IEnumerable<object> GlobalObjects { set { SetObjects(value, global: true); } }
	}
}
