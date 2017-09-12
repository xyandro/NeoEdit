using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.GUI;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Controls.ItemGridControl;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.Processes
{
	class ProcessItemGrid : ItemGrid<ProcessItem> { }

	partial class ProcessesWindow
	{
		[DepProp]
		ObservableCollection<ProcessItem> Processes { get { return UIHelper<ProcessesWindow>.GetPropValue<ObservableCollection<ProcessItem>>(this); } set { UIHelper<ProcessesWindow>.SetPropValue(this, value); } }

		static ProcessesWindow() { UIHelper<ProcessesWindow>.Register(); }

		public ProcessesWindow(int? pid = null)
		{
			ProcessManager.WindowCreated();
			ProcessesMenuItem.RegisterCommands(this, (command, multiStatus) => RunCommand(command));
			InitializeComponent();
			UIHelper.AuditMenu(menu);

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

		~ProcessesWindow() { ProcessManager.WindowDestroyed(); }

		void Refresh() => processes.SyncItems(ProcessManager.GetProcesses(), UIHelper<ProcessItem>.GetProperty(a => a.PID));

		internal void RunCommand(ProcessesCommand command)
		{
			switch (command)
			{
				case ProcessesCommand.View_Refresh:
					Refresh();
					break;
				case ProcessesCommand.View_Memory:
					foreach (ProcessItem selected in processes.Selected)
						Launcher.Static.LaunchHexEditorProcess(selected.PID);
					break;
				case ProcessesCommand.View_Modules:
					foreach (ProcessItem selected in processes.Selected)
						ViewModules.Run(selected.PID);
					break;
				case ProcessesCommand.Process_Suspend:
					foreach (ProcessItem selected in processes.Selected)
						Process.GetProcessById(selected.PID).Suspend();
					break;
				case ProcessesCommand.Process_Resume:
					foreach (ProcessItem selected in processes.Selected)
						Process.GetProcessById(selected.PID).Resume();
					break;
				case ProcessesCommand.Process_Kill:
					if (processes.Selected.Count != 0)
					{
						if (new Message(this)
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
