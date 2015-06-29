using System;
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
		protected Action<string, bool> textViewerLauncher;
		protected Action<string, byte[], Coder.CodePage, bool, bool> fileHexEditorLauncher;
		protected Action<int> processHexEditorLauncher;
		protected Action diskLauncher;
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
#endif
#if BuildTextView
			, Action<string, bool> textViewer
#endif
#if BuildHexEdit
			, Action<string, byte[], Coder.CodePage, bool, bool> fileHexEditor
			, Action<int> processHexEditor
#endif
#if BuildDisk
			, Action disk
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
			systemInfoLauncher();
		}

		public void LaunchTextEditor(string filename = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, bool? modified = null, bool createNew = false)
		{
			textEditorLauncher(filename, bytes, codePage, modified, createNew);
		}

		public void LaunchTextViewer(string filename = null, bool createNew = false)
		{
			textViewerLauncher(filename, createNew);
		}

		public void LaunchHexEditor(string filename = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, bool modified = false, bool createNew = false)
		{
			fileHexEditorLauncher(filename, bytes, codePage, modified, createNew);
		}

		public void LaunchHexEditor(int pid)
		{
			processHexEditorLauncher(pid);
		}

		public void LaunchDisk()
		{
			diskLauncher();
		}

		public void LaunchConsole()
		{
			consoleLauncher();
		}

		public void LaunchProcesses(int? pid = null)
		{
			processesLauncher(pid);
		}

		public void LaunchHandles(int? pid = null)
		{
			handlesLauncher(pid);
		}

		public void LaunchRegistry(string key = null)
		{
			registryLauncher(key);
		}

		public void LaunchDBViewer()
		{
			dbViewerLauncher();
		}
	}
}
