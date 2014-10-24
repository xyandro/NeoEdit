﻿using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NeoEdit.GUI;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.Dialogs;
using NeoEdit.GUI.ItemGridControl;
using NeoEdit.Win32;

namespace NeoEdit.Processes
{
	class ProcessItemGrid : ItemGrid<ProcessItem> { }

	public partial class ProcessesWindow : Window
	{
		public static RoutedCommand Command_View_Refresh = new RoutedCommand();
		public static RoutedCommand Command_View_Handles = new RoutedCommand();
		public static RoutedCommand Command_View_Memory = new RoutedCommand();
		public static RoutedCommand Command_Process_Suspend = new RoutedCommand();
		public static RoutedCommand Command_Process_Resume = new RoutedCommand();
		public static RoutedCommand Command_Process_Kill = new RoutedCommand();

		[DepProp]
		ObservableCollection<ProcessItem> Processes { get { return UIHelper<ProcessesWindow>.GetPropValue<ObservableCollection<ProcessItem>>(this); } set { UIHelper<ProcessesWindow>.SetPropValue(this, value); } }

		static ProcessesWindow() { UIHelper<ProcessesWindow>.Register(); }

		public ProcessesWindow(int? pid = null)
		{
			ProcessManager.WindowCreated();
			InitializeComponent();

			foreach (var prop in UIHelper<ProcessItem>.GetProperties())
			{
				processes.Columns.Add(new ItemGridColumn(prop)
				{
					SortAscending = (prop.Name != "CPU") && (prop.Name != "Size"),
				});
			}
			processes.SortColumn = processes.TextInputColumn = processes.Columns.First(col => col.Header == "Name");
			Processes = new ObservableCollection<ProcessItem>();
			Refresh();
			if (pid.HasValue)
			{
				processes.Focused = Processes.FirstOrDefault(proc => proc.PID == pid.Value);
				if (processes.Focused != null)
					processes.Selected.Add(processes.Focused);
			}
		}

		~ProcessesWindow()
		{
			ProcessManager.WindowDestroyed();
		}

		void Refresh()
		{
			processes.SyncItems(ProcessManager.GetProcesses(), UIHelper<ProcessItem>.GetProperty(a => a.PID));
		}

		void Command_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (e.Command == Command_View_Refresh)
				Refresh();
			else if (e.Command == Command_View_Handles)
			{
				foreach (ProcessItem selected in processes.Selected)
					Launcher.Static.LaunchHandles(selected.PID);
			}
			else if (e.Command == Command_View_Memory)
			{
				foreach (ProcessItem selected in processes.Selected)
					Launcher.Static.LaunchBinaryEditor(selected.PID);
			}
			else if (e.Command == Command_Process_Suspend)
			{
				foreach (ProcessItem selected in processes.Selected)
					Interop.SuspendProcess(selected.PID);
			}
			else if (e.Command == Command_Process_Resume)
			{
				foreach (ProcessItem selected in processes.Selected)
					Interop.ResumeProcess(selected.PID);
			}
			else if (e.Command == Command_Process_Kill)
			{
				if (processes.Selected.Count != 0)
				{
					if (new Message
					{
						Text = "Are you sure you want to kill these processes?",
						Options = Message.OptionsEnum.YesNo,
						DefaultAccept = Message.OptionsEnum.Yes,
						DefaultCancel = Message.OptionsEnum.No,
					}.Show() == Message.OptionsEnum.Yes)
					{
						foreach (ProcessItem selected in processes.Selected)
							Process.GetProcessById(selected.PID).Kill();
						Refresh();
					}
				}
			}
		}
	}
}
