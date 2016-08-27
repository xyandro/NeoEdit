using System;
using System.Collections.Generic;
using NeoEdit.Common.Transform;

namespace NeoEdit.GUI
{
	public class Launcher
	{
		static Launcher launcher;
		public static Launcher Static => launcher;

		Func<bool> getMinimizeToTrayLauncher;
		Action<bool> setMinimizeToTrayLauncher;

		Action<bool> consoleLauncher;
		Action diffLauncher;
		Action<string, IEnumerable<string>, bool> diskLauncher;
		Action<string, byte[], Coder.CodePage, bool, bool> fileHexEditorLauncher;
		Action<int?> handlesLauncher;
		Action networkLauncher;
		Action<int?> processesLauncher;
		Action<int> processHexEditorLauncher;
		Action systemInfoLauncher;
		Action<string, string, byte[], Coder.CodePage, bool?, bool> textEditorLauncher;
		Action<string, bool> textViewerLauncher;
		Action toolsLauncher;

		public static void Initialize(
			Func<bool> getMinimizeToTray
			, Action<bool> setMinimizeToTray

			, Action<bool> console
			, Action diff
			, Action<string, IEnumerable<string>, bool> disk
			, Action<string, byte[], Coder.CodePage, bool, bool> fileHexEditor
			, Action<int?> handles
			, Action network
			, Action<int?> processes
			, Action<int> processHexEditor
			, Action systemInfo
			, Action<string, string, byte[], Coder.CodePage, bool?, bool> textEditor
			, Action<string, bool> textViewer
			, Action tools
		)
		{
			launcher = new Launcher
			{
				getMinimizeToTrayLauncher = getMinimizeToTray,
				setMinimizeToTrayLauncher = setMinimizeToTray,

				consoleLauncher = console,
				diffLauncher = diff,
				diskLauncher = disk,
				fileHexEditorLauncher = fileHexEditor,
				handlesLauncher = handles,
				networkLauncher = network,
				processesLauncher = processes,
				processHexEditorLauncher = processHexEditor,
				systemInfoLauncher = systemInfo,
				textEditorLauncher = textEditor,
				textViewerLauncher = textViewer,
				toolsLauncher = tools,
			};
		}

		public bool MinimizeToTray
		{
			get { return getMinimizeToTrayLauncher(); }
			set { setMinimizeToTrayLauncher(value); }
		}

		public void LaunchSystemInfo() => systemInfoLauncher?.Invoke();
		public void LaunchTextEditor(string fileName = null, string displayName = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, bool? modified = null, bool forceCreate = false) => textEditorLauncher?.Invoke(fileName, displayName, bytes, codePage, modified, forceCreate);
		public void LaunchDiff() => diffLauncher?.Invoke();
		public void LaunchTextViewer(string fileName = null, bool forceCreate = false) => textViewerLauncher?.Invoke(fileName, forceCreate);
		public void LaunchTools() => toolsLauncher?.Invoke();
		public void LaunchHexEditor(string fileName = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, bool modified = false, bool forceCreate = false) => fileHexEditorLauncher?.Invoke(fileName, bytes, codePage, modified, forceCreate);
		public void LaunchHexEditor(int pid) => processHexEditorLauncher?.Invoke(pid);
		public void LaunchDisk(string path = null, IEnumerable<string> files = null, bool forceCreate = false) => diskLauncher?.Invoke(path, files, forceCreate);
		public void LaunchConsole(bool forceCreate = false) => consoleLauncher?.Invoke(forceCreate);
		public void LaunchNetwork() => networkLauncher?.Invoke();
		public void LaunchProcesses(int? pid = null) => processesLauncher?.Invoke(pid);
		public void LaunchHandles(int? pid = null) => handlesLauncher?.Invoke(pid);
	}
}
