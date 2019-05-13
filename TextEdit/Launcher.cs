using System;
using System.Windows;
using NeoEdit.TextEdit.Transform;

namespace NeoEdit.TextEdit
{
	public class Launcher
	{
		static Launcher launcher;
		public static Launcher Static => launcher;

		Func<Window> aboutLauncher;
		Func<string, string, byte[], Coder.CodePage, bool?, int?, int?, string, string, byte[], Coder.CodePage, bool?, int?, int?, string, Window> textEditorDiffLauncher;
		Func<string, string, byte[], Coder.CodePage, bool?, int?, int?, bool, string, Window> textEditorFileLauncher;
		Action updateLauncher;

		public static void Initialize(
			Func<Window> about = null
			, Func<string, string, byte[], Coder.CodePage, bool?, int?, int?, string, string, byte[], Coder.CodePage, bool?, int?, int?, string, Window> textEditorDiff = null
			, Func<string, string, byte[], Coder.CodePage, bool?, int?, int?, bool, string, Window> textEditorFile = null
			, Action update = null
		)
		{
			launcher = new Launcher
			{
				aboutLauncher = about,
				textEditorDiffLauncher = textEditorDiff,
				textEditorFileLauncher = textEditorFile,
				updateLauncher = update,
			};
		}

		public Window LaunchAbout() => aboutLauncher?.Invoke();
		public Window LaunchTextEditorDiff(string fileName1 = null, string displayName1 = null, byte[] bytes1 = null, Coder.CodePage codePage1 = Coder.CodePage.AutoByBOM, bool? modified1 = null, int? line1 = null, int? column1 = null, string fileName2 = null, string displayName2 = null, byte[] bytes2 = null, Coder.CodePage codePage2 = Coder.CodePage.AutoByBOM, bool? modified2 = null, int? line2 = null, int? column2 = null, string shutdownEvent = null) => textEditorDiffLauncher?.Invoke(fileName1, displayName1, bytes1, codePage1, modified1, line1, column1, fileName2, displayName2, bytes2, codePage2, modified2, line2, column2, shutdownEvent);
		public Window LaunchTextEditorFile(string fileName = null, string displayName = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, bool? modified = null, int? line = null, int? column = null, bool forceCreate = false, string shutdownEvent = null) => textEditorFileLauncher?.Invoke(fileName, displayName, bytes, codePage, modified, line, column, forceCreate, shutdownEvent);
		public void LaunchUpdate() => updateLauncher?.Invoke();
	}
}
