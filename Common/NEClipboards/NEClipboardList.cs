using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;

namespace NeoEdit.Common.NEClipboards
{
	public class NEClipboardList : IEnumerable<NEClipboardItem>
	{
		List<NEClipboardItem> neClipboardItems;

		public List<string> Strings => neClipboardItems.Select(item => item.String).ToList();
		public List<object> Objects => neClipboardItems.Select(item => item.Object).ToList();
		public List<BitmapSource> Images => neClipboardItems.Select(item => item.Image).ToList();

		public int Count => neClipboardItems.Count;

		private NEClipboardList() { }

		public static NEClipboardList CreateString(string str) => CreateStrings(new List<string> { str });
		public static NEClipboardList CreateStrings(IEnumerable<string> strings) => new NEClipboardList { neClipboardItems = strings.Select(str => NEClipboardItem.CreateString(str)).ToList() };

		public static NEClipboardList CreateObject(object obj, string text = null) => CreateObjects(new List<object> { obj }, text);
		public static NEClipboardList CreateObjects(IEnumerable<object> objects, string text = null) => new NEClipboardList() { neClipboardItems = objects.Select(obj => NEClipboardItem.CreateObject(obj, text)).ToList() };

		public static NEClipboardList CreateImage(BitmapSource image) => CreateImages(new List<BitmapSource> { image });
		public static NEClipboardList CreateImages(IEnumerable<BitmapSource> images) => new NEClipboardList() { neClipboardItems = images.Select(image => NEClipboardItem.CreateImage(image)).ToList() };

		public IEnumerator<NEClipboardItem> GetEnumerator() => neClipboardItems.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
