using System;
using System.Collections.Generic;
using System.Windows;
using NeoEdit.Common.Transform;

namespace NeoEdit.GUI
{
	public class Launcher
	{
		static Launcher launcher;
		public static Launcher Static => launcher;

		Func<Window> aboutLauncher;
		Func<string, IEnumerable<string>, bool, Window> diskLauncher;
		Func<string, byte[], Coder.CodePage, bool, bool, Window> hexEditorLauncher;
		Func<List<string>, bool, Window> streamSaverLauncher;
		Func<string, string, byte[], Coder.CodePage, bool?, int?, int?, string, string, byte[], Coder.CodePage, bool?, int?, int?, string, Window> textEditorDiffLauncher;
		Func<string, string, byte[], Coder.CodePage, bool?, int?, int?, bool, string, Window> textEditorFileLauncher;
		Func<string, bool, Window> textViewerLauncher;
		Action updateLauncher;

		public static void Initialize(
			Func<Window> about = null
			, Func<string, IEnumerable<string>, bool, Window> disk = null
			, Func<string, byte[], Coder.CodePage, bool, bool, Window> hexEditor = null
			, Func<List<string>, bool, Window> streamSaver = null
			, Func<string, string, byte[], Coder.CodePage, bool?, int?, int?, string, string, byte[], Coder.CodePage, bool?, int?, int?, string, Window> textEditorDiff = null
			, Func<string, string, byte[], Coder.CodePage, bool?, int?, int?, bool, string, Window> textEditorFile = null
			, Func<string, bool, Window> textViewer = null
			, Action update = null
		)
		{
			launcher = new Launcher
			{
				aboutLauncher = about,
				diskLauncher = disk,
				hexEditorLauncher = hexEditor,
				streamSaverLauncher = streamSaver,
				textEditorDiffLauncher = textEditorDiff,
				textEditorFileLauncher = textEditorFile,
				textViewerLauncher = textViewer,
				updateLauncher = update,
			};
		}

		public Window LaunchAbout() => aboutLauncher?.Invoke();
		public Window LaunchDisk(string path = null, IEnumerable<string> files = null, bool forceCreate = false) => diskLauncher?.Invoke(path, files, forceCreate);
		public Window LaunchHexEditor(string fileName = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, bool modified = false, bool forceCreate = false) => hexEditorLauncher?.Invoke(fileName, bytes, codePage, modified, forceCreate);
		public Window LaunchStreamSaver(List<string> urls = null, bool isPlaylist = false) => streamSaverLauncher?.Invoke(urls, isPlaylist);
		public Window LaunchTextEditorDiff(string fileName1 = null, string displayName1 = null, byte[] bytes1 = null, Coder.CodePage codePage1 = Coder.CodePage.AutoByBOM, bool? modified1 = null, int? line1 = null, int? column1 = null, string fileName2 = null, string displayName2 = null, byte[] bytes2 = null, Coder.CodePage codePage2 = Coder.CodePage.AutoByBOM, bool? modified2 = null, int? line2 = null, int? column2 = null, string shutdownEvent = null) => textEditorDiffLauncher?.Invoke(fileName1, displayName1, bytes1, codePage1, modified1, line1, column1, fileName2, displayName2, bytes2, codePage2, modified2, line2, column2, shutdownEvent);
		public Window LaunchTextEditorFile(string fileName = null, string displayName = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, bool? modified = null, int? line = null, int? column = null, bool forceCreate = false, string shutdownEvent = null) => textEditorFileLauncher?.Invoke(fileName, displayName, bytes, codePage, modified, line, column, forceCreate, shutdownEvent);
		public Window LaunchTextViewer(string fileName = null, bool forceCreate = false) => textViewerLauncher?.Invoke(fileName, forceCreate);
		public void LaunchUpdate() => updateLauncher?.Invoke();
	}
}
