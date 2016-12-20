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
		Action diffLauncher;
		Action<string, IEnumerable<string>, bool> diskLauncher;
		Action<string, byte[], Coder.CodePage, bool, bool> fileHexEditorLauncher;
		Action<int?> handlesLauncher;
		Action<string> imageEditorLauncher;
		Action licenseLauncher;
		Action networkLauncher;
		Action<int?> processesLauncher;
		Action<int> processHexEditorLauncher;
		Action<string, string, byte[], Coder.CodePage, bool?, bool> textEditorLauncher;
		Action<string, bool> textViewerLauncher;

		public static void Initialize(
			Action about
			, Action diff
			, Action<string, IEnumerable<string>, bool> disk
			, Action<string, byte[], Coder.CodePage, bool, bool> fileHexEditor
			, Action<int?> handles
			, Action<string> imageEditor
			, Action license
			, Action network
			, Action<int?> processes
			, Action<int> processHexEditor
			, Action<string, string, byte[], Coder.CodePage, bool?, bool> textEditor
			, Action<string, bool> textViewer
		)
		{
			launcher = new Launcher
			{
				aboutLauncher = about,
				diffLauncher = diff,
				diskLauncher = disk,
				fileHexEditorLauncher = fileHexEditor,
				handlesLauncher = handles,
				imageEditorLauncher = imageEditor,
				licenseLauncher = license,
				networkLauncher = network,
				processesLauncher = processes,
				processHexEditorLauncher = processHexEditor,
				textEditorLauncher = textEditor,
				textViewerLauncher = textViewer,
			};
		}

		public void LaunchAbout() => aboutLauncher?.Invoke();
		public void LaunchDiff() => diffLauncher?.Invoke();
		public void LaunchDisk(string path = null, IEnumerable<string> files = null, bool forceCreate = false) => diskLauncher?.Invoke(path, files, forceCreate);
		public void LaunchHandles(int? pid = null) => handlesLauncher?.Invoke(pid);
		public void LaunchHexEditor(string fileName = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, bool modified = false, bool forceCreate = false) => fileHexEditorLauncher?.Invoke(fileName, bytes, codePage, modified, forceCreate);
		public void LaunchHexEditor(int pid) => processHexEditorLauncher?.Invoke(pid);
		public void LaunchImageEditor(string fileName = null) => imageEditorLauncher?.Invoke(fileName);
		public void LaunchLicense() => licenseLauncher?.Invoke();
		public void LaunchNetwork() => networkLauncher?.Invoke();
		public void LaunchProcesses(int? pid = null) => processesLauncher?.Invoke(pid);
		public void LaunchTextEditor(string fileName = null, string displayName = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, bool? modified = null, bool forceCreate = false) => textEditorLauncher?.Invoke(fileName, displayName, bytes, codePage, modified, forceCreate);
		public void LaunchTextViewer(string fileName = null, bool forceCreate = false) => textViewerLauncher?.Invoke(fileName, forceCreate);
	}
}
