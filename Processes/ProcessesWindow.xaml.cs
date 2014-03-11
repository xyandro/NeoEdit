using NeoEdit.GUI;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.Dialogs;
using NeoEdit.GUI.ItemGridControl;
using NeoEdit.Win32;

namespace NeoEdit.Processes
{
	public partial class ProcessesWindow : Window
	{
		public static RoutedCommand Command_View_Refresh = new RoutedCommand();
		public static RoutedCommand Command_Process_Open = new RoutedCommand();
		public static RoutedCommand Command_Process_Suspend = new RoutedCommand();
		public static RoutedCommand Command_Process_Resume = new RoutedCommand();
		public static RoutedCommand Command_Process_Kill = new RoutedCommand();

		[DepProp]
		ObservableCollection<ProcessItem> Processes { get { return uiHelper.GetPropValue<ObservableCollection<ProcessItem>>(); } set { uiHelper.SetPropValue(value); } }

		static ProcessesWindow() { UIHelper<ProcessesWindow>.Register(); }

		readonly UIHelper<ProcessesWindow> uiHelper;
		public ProcessesWindow()
		{
			uiHelper = new UIHelper<ProcessesWindow>(this);
			InitializeComponent();

			foreach (ProcessItem.Property prop in Enum.GetValues(typeof(ProcessItem.Property)))
			{
				processes.Columns.Add(new ItemGridColumn
				{
					Header = prop.ToString(),
					DepProp = ProcessItem.GetDepProp(prop),
					HorizontalAlignment = (ProcessItem.PropertyType(prop) == typeof(int?)) || (ProcessItem.PropertyType(prop) == typeof(long?)) || (ProcessItem.PropertyType(prop) == typeof(DateTime?)) ? HorizontalAlignment.Right : HorizontalAlignment.Left,
					StringFormat = ProcessItem.PropertyType(prop) == typeof(long?) ? "n0" : ProcessItem.PropertyType(prop) == typeof(DateTime?) ? "yyyy/MM/dd HH:mm:ss 'GMT'" : null,
					SortAscending = (ProcessItem.PropertyType(prop) != typeof(int?)) && (ProcessItem.PropertyType(prop) != typeof(long?)),
				});
			}
			processes.SortColumn = processes.TextInputColumn = processes.Columns.First(col => col.Header == "Name");
			Processes = new ObservableCollection<ProcessItem>();
			Refresh();
			processes.Sort();
			processes.Focused = null;
		}

		void Refresh()
		{
			ProcessItem.UpdateProcesses(Processes);
		}

		void Command_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (e.Command == Command_View_Refresh)
				Refresh();
			else if (e.Command == Command_Process_Open)
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
