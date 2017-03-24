using System;
using System.Collections.Generic;
using NeoEdit.Common.Transform;

namespace NeoEdit.GUI
{
	public class Launcher
	{
		static Launcher launcher;
		public static Launcher Static => launcher;

		Action aboutLauncher;
		Action<string, IEnumerable<string>, bool> diskLauncher;
		Action<int?> handlesLauncher;
		Action<string, bool> hexEditorDumpLauncher;
		Action<string, byte[], Coder.CodePage, bool, bool> hexEditorFileLauncher;
		Action<int> hexEditorProcessLauncher;
		Action<string> imageEditorLauncher;
		Action licenseLauncher;
		Action networkLauncher;
		Action<int?> processesLauncher;
		Action ripperLauncher;
		Action streamSaverLauncher;
		Action<string, string, byte[], Coder.CodePage, bool?, int?, int?, string, string, byte[], Coder.CodePage, bool?, int?, int?, string> textEditorDiffLauncher;
		Action<string, string, byte[], Coder.CodePage, bool?, int?, int?, bool, string> textEditorFileLauncher;
		Action<string, bool> textViewerLauncher;
		Action updateLauncher;

		public static void Initialize(
			Action about = null
			, Action<string, IEnumerable<string>, bool> disk = null
			, Action<int?> handles = null
			, Action<string, bool> hexEditorDump = null
			, Action<string, byte[], Coder.CodePage, bool, bool> hexEditorFile = null
			, Action<int> hexEditorProcess = null
			, Action<string> imageEditor = null
			, Action license = null
			, Action network = null
			, Action<int?> processes = null
			, Action ripper = null
			, Action streamSaver = null
			, Action<string, string, byte[], Coder.CodePage, bool?, int?, int?, string, string, byte[], Coder.CodePage, bool?, int?, int?, string> textEditorDiff = null
			, Action<string, string, byte[], Coder.CodePage, bool?, int?, int?, bool, string> textEditorFile = null
			, Action<string, bool> textViewer = null
			, Action update = null
		)
		{
			launcher = new Launcher
			{
				aboutLauncher = about,
				diskLauncher = disk,
				handlesLauncher = handles,
				hexEditorDumpLauncher = hexEditorDump,
				hexEditorFileLauncher = hexEditorFile,
				hexEditorProcessLauncher = hexEditorProcess,
				imageEditorLauncher = imageEditor,
				licenseLauncher = license,
				networkLauncher = network,
				processesLauncher = processes,
				ripperLauncher = ripper,
				streamSaverLauncher = streamSaver,
				textEditorDiffLauncher = textEditorDiff,
				textEditorFileLauncher = textEditorFile,
				textViewerLauncher = textViewer,
				updateLauncher = update,
			};
		}

		public void LaunchAbout() => aboutLauncher?.Invoke();
		public void LaunchDisk(string path = null, IEnumerable<string> files = null, bool forceCreate = false) => diskLauncher?.Invoke(path, files, forceCreate);
		public void LaunchHandles(int? pid = null) => handlesLauncher?.Invoke(pid);
		public void LaunchHexEditorDump(string fileName, bool forceCreate = false) => hexEditorDumpLauncher(fileName, forceCreate);
		public void LaunchHexEditorFile(string fileName = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, bool modified = false, bool forceCreate = false) => hexEditorFileLauncher?.Invoke(fileName, bytes, codePage, modified, forceCreate);
		public void LaunchHexEditorProcess(int pid) => hexEditorProcessLauncher?.Invoke(pid);
		public void LaunchImageEditor(string fileName = null) => imageEditorLauncher?.Invoke(fileName);
		public void LaunchLicense() => licenseLauncher?.Invoke();
		public void LaunchNetwork() => networkLauncher?.Invoke();
		public void LaunchProcesses(int? pid = null) => processesLauncher?.Invoke(pid);
		public void LaunchRipper() => ripperLauncher?.Invoke();
		public void LaunchStreamSaver() => streamSaverLauncher?.Invoke();
		public void LaunchTextEditorDiff(string fileName1 = null, string displayName1 = null, byte[] bytes1 = null, Coder.CodePage codePage1 = Coder.CodePage.AutoByBOM, bool? modified1 = null, int? line1 = null, int? column1 = null, string fileName2 = null, string displayName2 = null, byte[] bytes2 = null, Coder.CodePage codePage2 = Coder.CodePage.AutoByBOM, bool? modified2 = null, int? line2 = null, int? column2 = null, string shutdownEvent = null) => textEditorDiffLauncher?.Invoke(fileName1, displayName1, bytes1, codePage1, modified1, line1, column1, fileName2, displayName2, bytes2, codePage2, modified2, line2, column2, shutdownEvent);
		public void LaunchTextEditorFile(string fileName = null, string displayName = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, bool? modified = null, int? line = null, int? column = null, bool forceCreate = false, string shutdownEvent = null) => textEditorFileLauncher?.Invoke(fileName, displayName, bytes, codePage, modified, line, column, forceCreate, shutdownEvent);
		public void LaunchTextViewer(string fileName = null, bool forceCreate = false) => textViewerLauncher?.Invoke(fileName, forceCreate);
		public void LaunchUpdate() => updateLauncher?.Invoke();
	}
}
