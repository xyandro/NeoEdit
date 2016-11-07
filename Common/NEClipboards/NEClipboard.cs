using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace NeoEdit.Common.NEClipboards
{
	public static class NEClipboard
	{
		static NELocalClipboard clipboard = new NELocalClipboard();

		public static void SetObjects(IEnumerable<object> objects, string text = null) => clipboard.SetObjects(objects, text);

		public static string Text { get { return clipboard.Text; } set { clipboard.GlobalText = value; } }
		public static List<string> Strings { get { return clipboard.Strings; } set { clipboard.GlobalStrings = value; } }
		public static string CopiedFile { set { clipboard.GlobalCopiedFile = value; } }
		public static string CutFile { set { clipboard.GlobalCutFile = value; } }
		public static IEnumerable<string> CopiedFiles { set { clipboard.GlobalCopiedFiles = value; } }
		public static IEnumerable<string> CutFiles { set { clipboard.GlobalCutFiles = value; } }
		public static List<object> Objects { get { return clipboard.Objects; } set { clipboard.GlobalObjects = value; } }
		public static BitmapSource Image { get { return clipboard.Image; } set { clipboard.GlobalImage = value; } }
	}
}
