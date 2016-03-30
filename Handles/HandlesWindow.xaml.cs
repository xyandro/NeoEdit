using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using NeoEdit.Common;
using NeoEdit.GUI;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Controls.ItemGridControl;
using NeoEdit.Win32;

namespace NeoEdit.Handles
{
	class HandlesItemGrid : ItemGrid<HandleItem> { }

	partial class HandlesWindow
	{
		public static RoutedCommand Command_View_Refresh = new RoutedCommand();
		public static RoutedCommand Command_View_GotoProcess = new RoutedCommand();
		public static RoutedCommand Command_View_FilterByType = new RoutedCommand();

		[DepProp]
		ObservableCollection<HandleItem> Handles { get { return UIHelper<HandlesWindow>.GetPropValue<ObservableCollection<HandleItem>>(this); } set { UIHelper<HandlesWindow>.SetPropValue(this, value); } }
		[DepProp]
		string SubTitle { get { return UIHelper<HandlesWindow>.GetPropValue<string>(this); } set { UIHelper<HandlesWindow>.SetPropValue(this, value); } }
		[DepProp]
		string HandleType { get { return UIHelper<HandlesWindow>.GetPropValue<string>(this); } set { UIHelper<HandlesWindow>.SetPropValue(this, value); } }
		[DepProp]
		List<string> HandleTypes { get { return UIHelper<HandlesWindow>.GetPropValue<List<string>>(this); } set { UIHelper<HandlesWindow>.SetPropValue(this, value); } }

		static HandlesWindow()
		{
			UIHelper<HandlesWindow>.Register();
			UIHelper<HandlesWindow>.AddCallback(a => a.HandleType, (obj, o, n) => obj.Refresh());
		}

		readonly int? pid;
		public HandlesWindow(int? pid = null)
		{
			this.pid = pid;

			InitializeComponent();

			HandleTypes = Interop.GetHandleTypes().Where(type => !string.IsNullOrEmpty(type)).OrderBy().ToList();
			HandleTypes.Insert(0, "");

			foreach (var prop in UIHelper<HandleItem>.GetProperties())
				handles.Columns.Add(new ItemGridColumn(prop));
			handles.SortColumn = handles.Columns.First(col => col.Header == "Type");
			Handles = new ObservableCollection<HandleItem>();

			Refresh();
		}

		void Refresh()
		{
			if (pid.HasValue)
			{
				var process = Process.GetProcessById(pid.Value);
				SubTitle = $"{process.ProcessName} ({process.MainWindowTitle}) Process {pid.Value}";
			}
			else
				SubTitle = "All handles";

			var handles = Interop.GetAllHandles();
			if (pid.HasValue)
				Interop.GetProcessHandles(handles, pid.Value);
			if (!string.IsNullOrEmpty(HandleType))
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
