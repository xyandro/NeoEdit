using System.Windows.Media.Imaging;

namespace NeoEdit.Program.NEClipboards
{
	public class NEClipboardItem
	{
		object data;
		public bool IsString { get; }
		public string String => IsString ? data as string : null;
		public object Object => !IsString ? data : null;

		public NEClipboardItem(string str) { data = str ?? ""; IsString = true; }
		public NEClipboardItem(object obj) { data = obj; IsString = false; }

		static public NEClipboardItem Create(string str) => new NEClipboardItem(str);
		static public NEClipboardItem Create(object obj) => new NEClipboardItem(obj);
		static public NEClipboardItem Create(BitmapSource image) => new NEClipboardItem(image);
	}
}
