using System.Windows.Media.Imaging;

namespace NeoEdit.Program.NEClipboards
{
	public class NEClipboardItem
	{
		public enum NEClipboardItemType
		{
			String,
			Object,
			Image,
		}

		object data;
		public NEClipboardItemType Type { get; }
		public string String => Type == NEClipboardItemType.String ? data as string : null;
		public object Object => Type == NEClipboardItemType.Object ? data : null;
		public BitmapSource Image => Type == NEClipboardItemType.Image ? data as BitmapSource : null;

		public NEClipboardItem(string str) { data = str ?? ""; Type = NEClipboardItemType.String; }
		public NEClipboardItem(object obj) { data = obj; Type = NEClipboardItemType.Object; }
		public NEClipboardItem(BitmapSource image) { data = image; Type = NEClipboardItemType.Image; }

		static public NEClipboardItem Create(string str) => new NEClipboardItem(str);
		static public NEClipboardItem Create(object obj) => new NEClipboardItem(obj);
		static public NEClipboardItem Create(BitmapSource image) => new NEClipboardItem(image);
	}
}
