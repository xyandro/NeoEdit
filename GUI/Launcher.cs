using System;
using NeoEdit.Common.Data;
using NeoEdit.Common.Transform;

namespace NeoEdit.GUI
{
	public class Launcher
	{
		static Launcher launcher;
		public static Launcher Static { get { return launcher; } }

		protected Action<string, byte[], Coder.Type> textEditorLauncher;
		protected Action<string, BinaryData> binaryEditorLauncher;
		protected Action browserLauncher;
		public static void Initialize(Action<string, byte[], Coder.Type> _textEditorLauncher, Action<string, BinaryData> _binaryEditorLauncher, Action _browserLauncher)
		{
			launcher = new Launcher
			{
				textEditorLauncher = _textEditorLauncher,
				binaryEditorLauncher = _binaryEditorLauncher,
				browserLauncher = _browserLauncher,
			};
		}

		public void LaunchTextEditor(string filename = null, byte[] bytes = null, Coder.Type encoding = Coder.Type.None)
		{
			textEditorLauncher(filename, bytes, encoding);
		}

		public void LaunchBinaryEditor(string filename = null, BinaryData binarydata = null)
		{
			binaryEditorLauncher(filename, binarydata);
		}

		public void LaunchBrowser()
		{
			browserLauncher();
		}
	}
}
