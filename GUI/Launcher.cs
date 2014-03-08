using System;
using NeoEdit.Common.Data;
using NeoEdit.Records;

namespace NeoEdit.GUI
{
	public class Launcher
	{
		static Launcher launcher;
		public static Launcher Static { get { return launcher; } }

		protected Action<Record, TextData> textEditorLauncher;
		protected Action<Record, BinaryData> binaryEditorLauncher;
		public static void Initialize(Action<Record, TextData> _textEditorLauncher, Action<Record, BinaryData> _binaryEditorLauncher)
		{
			launcher = new Launcher
			{
				textEditorLauncher = _textEditorLauncher,
				binaryEditorLauncher = _binaryEditorLauncher,
			};
		}

		public void LaunchTextEditor(Record record = null, TextData textdata = null)
		{
			textEditorLauncher(record, textdata);
		}

		public void LaunchBinaryEditor(Record record = null, BinaryData binarydata = null)
		{
			binaryEditorLauncher(record, binarydata);
		}
	}
}
