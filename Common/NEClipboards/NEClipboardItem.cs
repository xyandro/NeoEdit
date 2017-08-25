using System.Windows.Media.Imaging;

namespace NeoEdit.Common.NEClipboards
{
	public class NEClipboardItem
	{
		public string Text { get; private set; }
		public object Data { get; private set; }

		private NEClipboardItem() { }

		public string String => Data as string;
		public object Object => Data;
		public BitmapSource Image => Data as BitmapSource;

		public static NEClipboardItem CreateString(string str) => new NEClipboardItem { Text = str ?? "<NULL>", Data = str };
		public static NEClipboardItem CreateObject(object obj, string text = null) => new NEClipboardItem { Text = text ?? obj?.ToString() ?? "<NULL>", Data = obj };
		public static NEClipboardItem CreateImage(BitmapSource image) => new NEClipboardItem() { Text = "Image", Data = image };
	}
}
