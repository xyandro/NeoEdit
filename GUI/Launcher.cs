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

		protected Action consoleLauncher;
		protected Action<string, string> diffLauncher;
		protected Action<string, IEnumerable<string>> diskLauncher;
		protected Action<string, byte[], Coder.CodePage, bool, bool> fileHexEditorLauncher;
		protected Action<int?> handlesLauncher;
		protected Action<int?> processesLauncher;
		protected Action<int> processHexEditorLauncher;
		protected Action<string> registryLauncher;
		protected Action systemInfoLauncher;
		protected Action<string, bool> tablesLauncher;
		protected Action<string, byte[], Coder.CodePage, bool?, bool> textEditorLauncher;
		protected Action<string, bool> textViewerLauncher;

		public static void Initialize(
			Func<bool> getMinimizeToTray
			, Action<bool> setMinimizeToTray

			, Action console
			, Action<string, string> diff
			, Action<string, IEnumerable<string>> disk
			, Action<string, byte[], Coder.CodePage, bool, bool> fileHexEditor
			, Action<int?> handles
			, Action<int?> processes
			, Action<int> processHexEditor
			, Action<string> registry
			, Action systemInfo
			, Action<string, bool> tables
			, Action<string, byte[], Coder.CodePage, bool?, bool> textEditor
			, Action<string, bool> textViewer
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
				processesLauncher = processes,
				processHexEditorLauncher = processHexEditor,
				registryLauncher = registry,
				systemInfoLauncher = systemInfo,
				tablesLauncher = tables,
				textEditorLauncher = textEditor,
				textViewerLauncher = textViewer,
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

		public void LaunchTables(string filename = null, bool createNew = false)
		{
			if (tablesLauncher != null)
				tablesLauncher(filename, createNew);
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
	}
}
