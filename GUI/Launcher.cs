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
			Func<bool> getMinimizeToTray,
			Action<bool> setMinimizeToTray,

			Action systemInfo,
			Action<string, byte[], Coder.CodePage, bool?, bool> textEditor,
			Action<string, bool> textViewer,
			Action<string, byte[], Coder.CodePage, bool, bool> fileHexEditor,
			Action<int> processHexEditor,
			Action disk,
			Action console,
			Action<int?> processes,
			Action<int?> handles,
			Action<string> registry,
			Action dbViewer
		)
		{
			launcher = new Launcher
			{
				getMinimizeToTrayLauncher = getMinimizeToTray,
				setMinimizeToTrayLauncher = setMinimizeToTray,

				systemInfoLauncher = systemInfo,
				textEditorLauncher = textEditor,
				textViewerLauncher = textViewer,
				fileHexEditorLauncher = fileHexEditor,
				processHexEditorLauncher = processHexEditor,
				diskLauncher = disk,
				consoleLauncher = console,
				processesLauncher = processes,
				handlesLauncher = handles,
				registryLauncher = registry,
				dbViewerLauncher = dbViewer,
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
