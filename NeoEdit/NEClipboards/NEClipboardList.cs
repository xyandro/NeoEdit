using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;

namespace NeoEdit.Program.NEClipboards
{
	public class NEClipboardList : IEnumerable<NEClipboardItem>
	{
		List<NEClipboardItem> neClipboardItems = new List<NEClipboardItem>();

		public List<string> Strings => neClipboardItems.Where(item => item.Type == NEClipboardItem.NEClipboardItemType.String).Select(item => item.String).ToList();
		public List<object> Objects => neClipboardItems.Where(item => item.Type == NEClipboardItem.NEClipboardItemType.Object).Select(item => item.Object).ToList();
		public List<BitmapSource> Images => neClipboardItems.Where(item => item.Type == NEClipboardItem.NEClipboardItemType.Image).Select(item => item.Image).ToList();

		public int Count => neClipboardItems.Count;

		internal NEClipboardList() { }

		public void Add(NEClipboardItem item) => neClipboardItems.Add(item);
		public void Add(IEnumerable<NEClipboardItem> item) => neClipboardItems.AddRange(item);

		public static NEClipboardList Create(string str) => Create(new List<string> { str });
		public static NEClipboardList Create(IEnumerable<string> strings) => new NEClipboardList { neClipboardItems = strings.Select(str => NEClipboardItem.Create(str)).ToList() };

		public static NEClipboardList Create(object obj) => Create(new List<object> { obj });
		public static NEClipboardList Create(IEnumerable<object> objects) => new NEClipboardList { neClipboardItems = objects.Select(str => NEClipboardItem.Create(str)).ToList() };

		public static NEClipboardList Create(BitmapSource image) => Create(new List<BitmapSource> { image });
		public static NEClipboardList Create(IEnumerable<BitmapSource> images) => new NEClipboardList { neClipboardItems = images.Select(str => NEClipboardItem.Create(str)).ToList() };

		public IEnumerator<NEClipboardItem> GetEnumerator() => neClipboardItems.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
