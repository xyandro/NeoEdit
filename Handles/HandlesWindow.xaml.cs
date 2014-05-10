using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NeoEdit.GUI;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.ItemGridControl;
using NeoEdit.Win32;

namespace NeoEdit.Handles
{
	public partial class HandlesWindow : Window
	{
		public static RoutedCommand Command_View_Refresh = new RoutedCommand();
		public static RoutedCommand Command_View_GotoProcess = new RoutedCommand();
		public static RoutedCommand Command_View_FilterByType = new RoutedCommand();

		[DepProp]
		ObservableCollection<HandleItem> Handles { get { return uiHelper.GetPropValue<ObservableCollection<HandleItem>>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		string SubTitle { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		string HandleType { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		List<string> HandleTypes { get { return uiHelper.GetPropValue<List<string>>(); } set { uiHelper.SetPropValue(value); } }

		static HandlesWindow() { UIHelper<HandlesWindow>.Register(); }

		readonly UIHelper<HandlesWindow> uiHelper;
		readonly int? pid;
		public HandlesWindow(int? pid = null)
		{
			this.pid = pid;

			uiHelper = new UIHelper<HandlesWindow>(this);
			InitializeComponent();

			uiHelper.AddCallback(a => a.HandleType, (o, n) => Refresh());
			HandleTypes = Interop.GetHandleTypes().Where(type => !String.IsNullOrEmpty(type)).OrderBy(type => type).ToList();
			HandleTypes.Insert(0, "");

			foreach (var prop in HandleItem.StaticGetDepProps())
				handles.Columns.Add(new ItemGridColumn(prop));
			handles.SortColumn = handles.TextInputColumn = handles.Columns.First(col => col.Header == "Type");
			Handles = new ObservableCollection<HandleItem>();

			Refresh();
		}

		void Refresh()
		{
			if (pid.HasValue)
			{
				var process = Process.GetProcessById(pid.Value);
				SubTitle = String.Format("{0} ({1}) Process {2}", process.ProcessName, process.MainWindowTitle, pid.Value);
			}
			else
				SubTitle = "All handles";

			var handles = Interop.GetAllHandles();
			if (pid.HasValue)
				Interop.GetProcessHandles(handles, pid.Value);
			if (!String.IsNullOrEmpty(HandleType))
				Interop.GetTypeHandles(handles, HandleType);

			var handleInfo = Interop.GetHandleInfo(handles);
			Handles = new ObservableCollection<HandleItem>(handleInfo.Select(info => new HandleItem(info)));
		}

		void Command_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (e.Command == Command_View_Refresh)
				Refresh();
			else if (e.Command == Command_View_GotoProcess)
			{
				if (handles.Selected.Count == 1)
					Launcher.Static.LaunchProcesses((handles.Selected.First() as HandleItem).PID);
			}
			else if (e.Command == Command_View_FilterByType)
			{
				if (handles.Selected.Count == 1)
					HandleType = (handles.Selected.First() as HandleItem).Type;
			}
		}
	}
}
