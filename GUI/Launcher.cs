using System;
using System.Collections.Generic;
using NeoEdit.Common.Transform;

namespace NeoEdit.GUI
{
	public class Launcher
	{
		static Launcher launcher;
		public static Launcher Static { get { return launcher; } }

		Func<bool> getMinimizeToTrayLauncher;
		Action<bool> setMinimizeToTrayLauncher;

		protected Action systemInfoLauncher;
		protected Action<string, byte[], Coder.CodePage, bool?, bool> textEditorLauncher;
		protected Action<string, string> diffLauncher;
		protected Action<string, bool> textViewerLauncher;
		protected Action<string, byte[], Coder.CodePage, bool, bool> fileHexEditorLauncher;
		protected Action<int> processHexEditorLauncher;
		protected Action<string, IEnumerable<string>> diskLauncher;
		protected Action consoleLauncher;
		protected Action<int?> processesLauncher;
		protected Action<int?> handlesLauncher;
		protected Action<string> registryLauncher;
		protected Action dbViewerLauncher;

		public static void Initialize(
			Func<bool> getMinimizeToTray
			, Action<bool> setMinimizeToTray

#if BuildSystemInfo
			, Action systemInfo
#endif
#if BuildTextEdit
			, Action<string, byte[], Coder.CodePage, bool?, bool> textEditor
			, Action<string, string> diff
#endif
#if BuildTextView
			, Action<string, bool> textViewer
#endif
#if BuildHexEdit
			, Action<string, byte[], Coder.CodePage, bool, bool> fileHexEditor
			, Action<int> processHexEditor
#endif
#if BuildDisk
			, Action<string, IEnumerable<string>> disk
#endif
#if BuildConsole
			, Action console
#endif
#if BuildProcesses
			, Action<int?> processes
#endif
#if BuildHandles
			, Action<int?> handles
#endif
#if BuildRegistry
			, Action<string> registry
#endif
#if BuildDBViewer
			, Action dbViewer
#endif
		)
		{
			launcher = new Launcher
			{
				getMinimizeToTrayLauncher = getMinimizeToTray,
				setMinimizeToTrayLauncher = setMinimizeToTray,

#if BuildSystemInfo
				systemInfoLauncher = systemInfo,
#endif
#if BuildTextEdit
				textEditorLauncher = textEditor,
				diffLauncher = diff,
#endif
#if BuildTextView
				textViewerLauncher = textViewer,
#endif
#if BuildHexEdit
				fileHexEditorLauncher = fileHexEditor,
				processHexEditorLauncher = processHexEditor,
#endif
#if BuildDisk
				diskLauncher = disk,
#endif
#if BuildConsole
				consoleLauncher = console,
#endif
#if BuildProcesses
				processesLauncher = processes,
#endif
#if BuildHandles
				handlesLauncher = handles,
#endif
#if BuildRegistry
				registryLauncher = registry,
#endif
#if BuildDBViewer
				dbViewerLauncher = dbViewer,
#endif
			};
		}

		public bool MinimizeToTray
		{
			get { return getMinimizeToTrayLauncher(); }
			set { setMinimizeToTrayLauncher(value); }
		}

		public void LaunchSystemInfo()
		{
			if (systemInfoLauncher != null)
				systemInfoLauncher();
		}

		public void LaunchTextEditor(string filename = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, bool? modified = null, bool createNew = false)
		{
			if (textEditorLauncher != null)
				textEditorLauncher(filename, bytes, codePage, modified, createNew);
		}

		public void LaunchDiff(string filename1 = null, string filename2 = null)
		{
			if (diffLauncher != null)
				diffLauncher(filename1, filename2);
		}

		public void LaunchTextViewer(string filename = null, bool createNew = false)
		{
			if (textViewerLauncher != null)
				textViewerLauncher(filename, createNew);
		}

		public void LaunchHexEditor(string filename = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, bool modified = false, bool createNew = false)
		{
			if (fileHexEditorLauncher != null)
				fileHexEditorLauncher(filename, bytes, codePage, modified, createNew);
		}

		public void LaunchHexEditor(int pid)
		{
			if (processHexEditorLauncher != null)
				processHexEditorLauncher(pid);
		}

		public void LaunchDisk(string path = null, IEnumerable<string> files = null)
		{
			if (diskLauncher != null)
				diskLauncher(path, files);
		}

		public void LaunchConsole()
		{
			if (consoleLauncher != null)
				consoleLauncher();
		}

		public void LaunchProcesses(int? pid = null)
		{
			if (processesLauncher != null)
				processesLauncher(pid);
		}

		public void LaunchHandles(int? pid = null)
		{
			if (handlesLauncher != null)
				handlesLauncher(pid);
		}

		public void LaunchRegistry(string key = null)
		{
			if (registryLauncher != null)
				registryLauncher(key);
		}

		public void LaunchDBViewer()
		{
			if (dbViewerLauncher != null)
				dbViewerLauncher();
		}
	}
}
