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
		protected Action processesLauncher;
		public static void Initialize(
			Action<string, byte[], Coder.Type> textEditor,
			Action<string, byte[]> fileBinaryEditor,
			Action<int> processBinaryEditor,
			Action browser,
			Action processes
		)
		{
			launcher = new Launcher
			{
				textEditorLauncher = textEditor,
				fileBinaryEditorLauncher = fileBinaryEditor,
				processBinaryEditorLauncher = processBinaryEditor,
				browserLauncher = browser,
				processesLauncher = processes,
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

		public void LaunchBinaryEditor(int PID)
		{
			processBinaryEditorLauncher(PID);
		}

		public void LaunchBrowser()
		{
			browserLauncher();
		}

		public void LaunchProcesses()
		{
			processesLauncher();
		}
	}
}
