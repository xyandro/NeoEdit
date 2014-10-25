using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NeoEdit.GUI.Common;

namespace NeoEdit.Disk
{
	public class Tabs : Tabs<DiskWindow> { }

	public partial class DiskTabs
	{
		[DepProp]
		public ObservableCollection<DiskWindow> DiskWindows { get { return UIHelper<DiskTabs>.GetPropValue<ObservableCollection<DiskWindow>>(this); } set { UIHelper<DiskTabs>.SetPropValue(this, value); } }
		[DepProp]
		public DiskWindow Active { get { return UIHelper<DiskTabs>.GetPropValue<DiskWindow>(this); } set { UIHelper<DiskTabs>.SetPropValue(this, value); } }
		[DepProp]
		public Tabs.ViewType View { get { return UIHelper<DiskTabs>.GetPropValue<Tabs.ViewType>(this); } set { UIHelper<DiskTabs>.SetPropValue(this, value); } }

		static DiskTabs() { UIHelper<DiskTabs>.Register(); }

		public DiskTabs(string path = null)
		{
			DiskMenuItem.RegisterCommands(this, (s, e, command) => RunCommand(command));
			InitializeComponent();

			DiskWindows = new ObservableCollection<DiskWindow>();
			Add(new DiskWindow(path));
		}

		bool shiftDown { get { return (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None; } }

		void RunCommand(DiskCommand command)
		{
			switch (command)
			{
				case DiskCommand.File_New: Add(new DiskWindow()); break;
				case DiskCommand.File_Exit: Close(); break;
				case DiskCommand.View_Tiles: View = View == Tabs.ViewType.Tiles ? Tabs.ViewType.Tabs : Tabs.ViewType.Tiles; break;
			}

			if (Active == null)
				return;

			switch (command)
			{
				case DiskCommand.File_Close: DiskWindows.Remove(Active); break;
				case DiskCommand.File_Rename: Active.Command_File_Rename(); break;
				case DiskCommand.File_Identify: Active.Command_File_Identify(); break;
				case DiskCommand.File_MD5: Active.Command_File_MD5(); break;
				case DiskCommand.File_SHA1: Active.Command_File_SHA1(); break;
				case DiskCommand.File_Delete: Active.Command_File_Delete(); break;
				case DiskCommand.Edit_Cut: Active.Command_Edit_Cut(); break;
				case DiskCommand.Edit_Copy: Active.Command_Edit_Copy(); break;
				case DiskCommand.Edit_Paste: Active.Command_Edit_Paste(); break;
				case DiskCommand.Edit_Find: Active.Command_Edit_Find(); break;
				case DiskCommand.Edit_TextEdit: Active.Command_Edit_TextEdit(); break;
				case DiskCommand.Edit_BinaryEdit: Active.Command_Edit_BinaryEdit(); break;
				case DiskCommand.Select_All: Active.Command_Select_All(); break;
				case DiskCommand.Select_None: Active.Command_Select_None(); break;
				case DiskCommand.Select_Invert: Active.Command_Select_Invert(); break;
				case DiskCommand.Select_Directories: Active.Command_Select_Directories(); break;
				case DiskCommand.Select_Files: Active.Command_Select_Files(); break;
				case DiskCommand.Select_Expression: Active.Command_Select_Expression(shiftDown); break;
				case DiskCommand.Select_Remove: Active.Command_Select_Remove(); break;
				case DiskCommand.View_Refresh: Active.Command_View_Refresh(); break;
			}
		}

		internal void ToggleColumn(DependencyProperty property)
		{
			if (Active != null)
				Active.ToggleColumn(property);
		}

		internal void SetSort(DependencyProperty property)
		{
			if (Active != null)
				Active.SetSort(property);
		}

		void Add(DiskWindow diskWindow)
		{
			DiskWindows.Add(diskWindow);
			Active = diskWindow;
		}

		Label GetLabel(DiskWindow diskWindow)
		{
			return diskWindow.GetLabel();
		}
	}
}
