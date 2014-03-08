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
		public static void Initialize(Action<Record, TextData> _textEditorLauncher)
		{
			launcher = new Launcher
			{
				textEditorLauncher = _textEditorLauncher,
			};
		}

		public void LauncherTextEditor(Record record = null, TextData textdata = null)
		{
			textEditorLauncher(record, textdata);
		}
	}
}
