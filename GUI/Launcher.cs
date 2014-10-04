﻿using System;
using NeoEdit.Common.Transform;

namespace NeoEdit.GUI
{
	public class Launcher
	{
		static Launcher launcher;
		public static Launcher Static { get { return launcher; } }

		protected Action systemInfoLauncher;
		protected Action<string, byte[], Coder.Type> textEditorLauncher;
		protected Action<string, byte[], Coder.Type> fileBinaryEditorLauncher;
		protected Action<int> processBinaryEditorLauncher;
		protected Action diskLauncher;
		protected Action<int?> processesLauncher;
		protected Action<int?> handlesLauncher;
		protected Action<string> registryLauncher;
		protected Action dbViewerLauncher;
		public static void Initialize(
			Action systemInfo,
			Action<string, byte[], Coder.Type> textEditor,
			Action<string, byte[], Coder.Type> fileBinaryEditor,
			Action<int> processBinaryEditor,
			Action disk,
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

		public void LaunchTextEditor(string filename = null, byte[] bytes = null, Coder.Type encoding = Coder.Type.None)
		{
			textEditorLauncher(filename, bytes, encoding);
		}

		public void LaunchBinaryEditor(string filename = null, byte[] bytes = null, Coder.Type encoding = Coder.Type.None)
		{
			fileBinaryEditorLauncher(filename, bytes, encoding);
		}

		public void LaunchBinaryEditor(int pid)
		{
			processBinaryEditorLauncher(pid);
		}

		public void LaunchDisk()
		{
			diskLauncher();
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
