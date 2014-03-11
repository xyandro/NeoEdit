using System;
using NeoEdit.Common.Transform;

namespace NeoEdit.GUI
{
	public class Launcher
	{
		static Launcher launcher;
		public static Launcher Static { get { return launcher; } }

		protected Action<string, byte[], Coder.Type> textEditorLauncher;
		protected Action<string, byte[]> fileBinaryEditorLauncher;
		protected Action<int> processBinaryEditorLauncher;
		protected Action browserLauncher;
		protected Action<int?> processesLauncher;
		protected Action<int?> handlesLauncher;
		public static void Initialize(
			Action<string, byte[], Coder.Type> textEditor,
			Action<string, byte[]> fileBinaryEditor,
			Action<int> processBinaryEditor,
			Action browser,
			Action<int?> processes,
			Action<int?> handles
		)
		{
			launcher = new Launcher
			{
				textEditorLauncher = textEditor,
				fileBinaryEditorLauncher = fileBinaryEditor,
				processBinaryEditorLauncher = processBinaryEditor,
				browserLauncher = browser,
				processesLauncher = processes,
				handlesLauncher = handles,
			};
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
	}
}
