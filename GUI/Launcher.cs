using System;
using NeoEdit.Common.Transform;

namespace NeoEdit.GUI
{
	public class Launcher
	{
		static Launcher launcher;
		public static Launcher Static { get { return launcher; } }

		protected Action systemInfoLauncher;
		protected Action<string, byte[], StrCoder.CodePage, bool> textEditorLauncher;
		protected Action<string, byte[], StrCoder.CodePage, bool> fileBinaryEditorLauncher;
		protected Action<int> processBinaryEditorLauncher;
		protected Action diskLauncher;
		protected Action consoleLauncher;
		protected Action<int?> processesLauncher;
		protected Action<int?> handlesLauncher;
		protected Action<string> registryLauncher;
		protected Action dbViewerLauncher;
		public static void Initialize(
			Action systemInfo,
			Action<string, byte[], StrCoder.CodePage, bool> textEditor,
			Action<string, byte[], StrCoder.CodePage, bool> fileBinaryEditor,
			Action<int> processBinaryEditor,
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
				systemInfoLauncher = systemInfo,
				textEditorLauncher = textEditor,
				fileBinaryEditorLauncher = fileBinaryEditor,
				processBinaryEditorLauncher = processBinaryEditor,
				diskLauncher = disk,
				consoleLauncher = console,
				processesLauncher = processes,
				handlesLauncher = handles,
				registryLauncher = registry,
				dbViewerLauncher = dbViewer,
			};
		}

		public void LaunchSystemInfo()
		{
			systemInfoLauncher();
		}

		public void LaunchTextEditor(string filename = null, byte[] bytes = null, StrCoder.CodePage codePage = StrCoder.CodePage.AutoByBOM, bool createNew = false)
		{
			textEditorLauncher(filename, bytes, codePage, createNew);
		}

		public void LaunchBinaryEditor(string filename = null, byte[] bytes = null, StrCoder.CodePage codePage = StrCoder.CodePage.AutoByBOM, bool createNew = false)
		{
			fileBinaryEditorLauncher(filename, bytes, codePage, createNew);
		}

		public void LaunchBinaryEditor(int pid)
		{
			processBinaryEditorLauncher(pid);
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
