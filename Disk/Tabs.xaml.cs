using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using NeoEdit.GUI.Controls;

namespace NeoEdit.Disk
{
	public class Tabs : Tabs<DiskWindow> { }

	public partial class DiskTabs
	{
		[DepProp]
		public ObservableCollection<DiskWindow> DiskWindows { get { return UIHelper<DiskTabs>.GetPropValue<ObservableCollection<DiskWindow>>(this); } set { UIHelper<DiskTabs>.SetPropValue(this, value); } }
		[DepProp]
		public DiskWindow TopMost { get { return UIHelper<DiskTabs>.GetPropValue<DiskWindow>(this); } set { UIHelper<DiskTabs>.SetPropValue(this, value); } }
		[DepProp]
		public bool Tiles { get { return UIHelper<DiskTabs>.GetPropValue<bool>(this); } set { UIHelper<DiskTabs>.SetPropValue(this, value); } }

		static DiskTabs() { UIHelper<DiskTabs>.Register(); }

		static List<DiskWindow> Lists = new List<DiskWindow> { new DiskWindow(list: 1), new DiskWindow(list: 2), new DiskWindow(list: 3), new DiskWindow(list: 4), new DiskWindow(list: 5), new DiskWindow(list: 6), new DiskWindow(list: 7), new DiskWindow(list: 8), new DiskWindow(list: 9) };

		public DiskTabs(string path = null, IEnumerable<string> files = null)
		{
			DiskMenuItem.RegisterCommands(this, (s, e, command) => RunCommand(command, shiftDown));
			InitializeComponent();
			UIHelper.AuditMenu(menu);

			DiskWindows = new ObservableCollection<DiskWindow>();
			Add(new DiskWindow(path, listFiles: files));
		}

		public static DiskWindow GetList(int list)
		{
			return Lists[list - 1];
		}

		bool shiftDown { get { return (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None; } }

		void Command_File_New(bool newWindow)
		{
			if (newWindow)
				new DiskTabs();
			else
				Add(new DiskWindow(Directory.GetCurrentDirectory()));
		}

		void Command_View_List(int list)
		{
			var listObj = GetList(list);
			var parent = listObj.GetValue(Tabs.TabParentProperty) as Tabs;
			if (parent != null)
				parent.Items.Remove(listObj);

			Add(listObj);
		}

		void RunCommand(DiskCommand command, bool shiftDown)
		{
			switch (command)
			{
				case DiskCommand.File_NewTab: Command_File_New(shiftDown); break;
				case DiskCommand.File_Exit: Close(); break;
				case DiskCommand.View_List1: Command_View_List(1); break;
				case DiskCommand.View_List2: Command_View_List(2); break;
				case DiskCommand.View_List3: Command_View_List(3); break;
				case DiskCommand.View_List4: Command_View_List(4); break;
				case DiskCommand.View_List5: Command_View_List(5); break;
				case DiskCommand.View_List6: Command_View_List(6); break;
				case DiskCommand.View_List7: Command_View_List(7); break;
				case DiskCommand.View_List8: Command_View_List(8); break;
				case DiskCommand.View_List9: Command_View_List(9); break;
			}

			if (TopMost == null)
				return;

			switch (command)
			{
				case DiskCommand.File_Close: DiskWindows.Remove(TopMost); break;
				case DiskCommand.File_Rename: TopMost.Command_File_Rename(); break;
				case DiskCommand.File_Identify: TopMost.Command_File_Identify(); break;
				case DiskCommand.File_Hash: TopMost.Command_File_Hash(); break;
				case DiskCommand.File_VCS: TopMost.Command_File_VCS(); break;
				case DiskCommand.File_Delete: TopMost.Command_File_Delete(); break;
				case DiskCommand.Edit_Cut: TopMost.Command_Edit_CutCopy(true); break;
				case DiskCommand.Edit_Copy: TopMost.Command_Edit_CutCopy(false); break;
				case DiskCommand.Edit_Paste: TopMost.Command_Edit_Paste(); break;
				case DiskCommand.Edit_Find: TopMost.Command_Edit_Find(); break;
				case DiskCommand.Edit_FindBinary: TopMost.Command_Edit_FindBinary(); break;
				case DiskCommand.Edit_FindText: TopMost.Command_Edit_FindText(); break;
				case DiskCommand.Edit_ToList1: TopMost.Command_Edit_ToList(1); break;
				case DiskCommand.Edit_ToList2: TopMost.Command_Edit_ToList(2); break;
				case DiskCommand.Edit_ToList3: TopMost.Command_Edit_ToList(3); break;
				case DiskCommand.Edit_ToList4: TopMost.Command_Edit_ToList(4); break;
				case DiskCommand.Edit_ToList5: TopMost.Command_Edit_ToList(5); break;
				case DiskCommand.Edit_ToList6: TopMost.Command_Edit_ToList(6); break;
				case DiskCommand.Edit_ToList7: TopMost.Command_Edit_ToList(7); break;
				case DiskCommand.Edit_ToList8: TopMost.Command_Edit_ToList(8); break;
				case DiskCommand.Edit_ToList9: TopMost.Command_Edit_ToList(9); break;
				case DiskCommand.Edit_TextEdit: TopMost.Command_Edit_TextEdit(); break;
				case DiskCommand.Edit_HexEdit: TopMost.Command_Edit_HexEdit(); break;
				case DiskCommand.Select_All: TopMost.Command_Select_All(); break;
				case DiskCommand.Select_None: TopMost.Command_Select_None(); break;
				case DiskCommand.Select_Invert: TopMost.Command_Select_Invert(); break;
				case DiskCommand.Select_Directories: TopMost.Command_Select_Directories(); break;
				case DiskCommand.Select_Files: TopMost.Command_Select_Files(); break;
				case DiskCommand.Select_Unique: TopMost.Command_Select_Unique(); break;
				case DiskCommand.Select_Duplicates: TopMost.Command_Select_Duplicates(); break;
				case DiskCommand.Select_AddCopiedCut: TopMost.Command_Select_AddCopiedCut(); break;
				case DiskCommand.Select_Remove: TopMost.Command_Select_Remove(); break;
				case DiskCommand.Select_RemoveWithChildren: TopMost.Command_Select_RemoveWithChildren(); break;
				case DiskCommand.View_DiskUsage: TopMost.Command_View_DiskUsage(); break;
			}
		}

		internal void ToggleColumn(DependencyProperty property)
		{
			if (TopMost != null)
				TopMost.ToggleColumn(property);
		}

		internal void SetSort(DependencyProperty property)
		{
			if (TopMost != null)
				TopMost.SetSort(property);
		}

		void Add(DiskWindow diskWindow)
		{
			DiskWindows.Add(diskWindow);
			TopMost = diskWindow;
		}
	}
}
