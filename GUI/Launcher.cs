using System;
using NeoEdit.Common.Transform;

namespace NeoEdit.GUI
{
	public class Launcher
	{
		static Launcher launcher;
		public static Launcher Static { get { return launcher; } }

		protected Action systemInfoLauncher;
		protected Action<string, byte[], Coder.Type> textEditorLauncher;
		protected Action<string, byte[]> fileBinaryEditorLauncher;
		protected Action<int> processBinaryEditorLauncher;
		protected Action browserLauncher;
		protected Action<int?> processesLauncher;
		protected Action<int?> handlesLauncher;
		protected Action<string> registryLauncher;
		public static void Initialize(
			Action systemInfo,
			Action<string, byte[], Coder.Type> textEditor,
			Action<string, byte[]> fileBinaryEditor,
			Action<int> processBinaryEditor,
			Action browser,
			Action<int?> processes,
			Action<int?> handles,
			Action<string> registry
		)
		{
			launcher = new Launcher
			{
				systemInfoLauncher = systemInfo,
				textEditorLauncher = textEditor,
				fileBinaryEditorLauncher = fileBinaryEditor,
				processBinaryEditorLauncher = processBinaryEditor,
				browserLauncher = browser,
				processesLauncher = processes,
				handlesLauncher = handles,
				registryLauncher = registry,
			};
		}

		public void LaunchSystemInfo()
		{
			systemInfoLauncher();
		}

		public void LaunchTextEditor(string filename = null, byte[] bytes = null, Coder.Type encoding = Coder.Type.None)
		{
			textEditorLauncher(filename, bytes, encoding);
		}

		public void LaunchBinaryEditor(string filename = null, byte[] bytes = null)
		{
			fileBinaryEditorLauncher(filename, bytes);
		}

		public void LaunchBinaryEditor(int pid)
		{
			processBinaryEditorLauncher(pid);
		}

		public void LaunchBrowser()
		{
			browserLauncher();
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
	}
}
