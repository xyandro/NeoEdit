using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
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
		[DepProp]
		ObservableCollection<ProcessItem> Processes { get { return UIHelper<ProcessesWindow>.GetPropValue(() => this.Processes); } set { UIHelper<ProcessesWindow>.SetPropValue(() => this.Processes, value); } }

		static ProcessesWindow() { UIHelper<ProcessesWindow>.Register(); }

		public ProcessesWindow(int? pid = null)
		{
			ProcessManager.WindowCreated();
			ProcessesMenuItem.RegisterCommands(this, (s, e, command) => RunCommand(command));
			InitializeComponent();

			foreach (var prop in UIHelper<ProcessItem>.GetProperties())
			{
				processes.Columns.Add(new ItemGridColumn(prop)
				{
					SortAscending = (prop.Name != "CPU") && (prop.Name != "Size"),
				});
			}
			processes.SortColumn = processes.Columns.First(col => col.Header == "Name");
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

		internal void RunCommand(ProcessesCommand command)
		{
			switch (command)
			{
				case ProcessesCommand.View_Refresh:
					Refresh();
					break;
				case ProcessesCommand.View_Handles:
					foreach (ProcessItem selected in processes.Selected)
						Launcher.Static.LaunchHandles(selected.PID);
					break;
				case ProcessesCommand.View_Memory:
					foreach (ProcessItem selected in processes.Selected)
						Launcher.Static.LaunchHexEditor(selected.PID);
					break;
				case ProcessesCommand.View_Modules:
					foreach (ProcessItem selected in processes.Selected)
						ViewModules.Run(selected.PID);
					break;
				case ProcessesCommand.Process_Suspend:
					foreach (ProcessItem selected in processes.Selected)
						Interop.SuspendProcess(selected.PID);
					break;
				case ProcessesCommand.Process_Resume:
					foreach (ProcessItem selected in processes.Selected)
						Interop.ResumeProcess(selected.PID);
					break;
				case ProcessesCommand.Process_Kill:
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
					break;
			}
		}
	}
}
