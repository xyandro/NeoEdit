using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using NeoEdit.GUI.Common;

namespace NeoEdit.Disk
{
	public class Tabs : Tabs<DiskWindow> { }

	public partial class DiskTabs
	{
		[DepProp]
		public ObservableCollection<DiskWindow> DiskWindows { get { return uiHelper.GetPropValue<ObservableCollection<DiskWindow>>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public DiskWindow Active { get { return uiHelper.GetPropValue<DiskWindow>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public Tabs.ViewType View { get { return uiHelper.GetPropValue<Tabs.ViewType>(); } set { uiHelper.SetPropValue(value); } }

		static DiskTabs()
		{
			UIHelper<DiskTabs>.Register();
		}

		readonly UIHelper<DiskTabs> uiHelper;
		public DiskTabs(string path = null)
		{
			if (String.IsNullOrEmpty(path))
				path = Directory.GetCurrentDirectory();

			uiHelper = new UIHelper<DiskTabs>(this);
			DiskMenuItem.RegisterCommands(this, (s, e, command) => RunCommand(command));
			InitializeComponent();

			DiskWindows = new ObservableCollection<DiskWindow>();
			Add(new DiskWindow(path));
		}

		bool shiftDown { get { return (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None; } }

		void RunCommand(DiskCommand command)
		{
			if (Active == null)
				return;

			switch (command)
			{
				case DiskCommand.File_Rename: Active.Command_File_Rename(); break;
				case DiskCommand.File_Identify: Active.Command_File_Identify(); break;
				case DiskCommand.File_MD5: Active.Command_File_MD5(); break;
				case DiskCommand.File_SHA1: Active.Command_File_SHA1(); break;
				case DiskCommand.File_Delete: Active.Command_File_Delete(); break;
				case DiskCommand.Edit_Cut: Active.Command_Edit_Cut(); break;
				case DiskCommand.Edit_Copy: Active.Command_Edit_Copy(); break;
				case DiskCommand.Edit_Paste: Active.Command_Edit_Paste(); break;
				case DiskCommand.View_Refresh: Active.Command_View_Refresh(); break;
			}
		}

		void Add(DiskWindow diskWindow)
		{
			if (Active != null)
			{
				var index = DiskWindows.IndexOf(Active);
				Active = DiskWindows[index] = diskWindow;
				return;
			}

			DiskWindows.Add(diskWindow);
			Active = diskWindow;
		}

		Label GetLabel(DiskWindow diskWindow)
		{
			return diskWindow.GetLabel();
		}
	}
}
