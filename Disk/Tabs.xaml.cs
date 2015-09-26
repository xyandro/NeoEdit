using System.Collections.Generic;
using System.IO;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.Disk
{
	public class Tabs : Tabs<DiskWindow> { }
	public class TabsWindow : TabsWindow<DiskWindow> { }

	public partial class DiskTabs
	{
		static DiskTabs() { UIHelper<DiskTabs>.Register(); }

		static List<DiskWindow> Lists = new List<DiskWindow> { new DiskWindow(list: 1), new DiskWindow(list: 2), new DiskWindow(list: 3), new DiskWindow(list: 4), new DiskWindow(list: 5), new DiskWindow(list: 6), new DiskWindow(list: 7), new DiskWindow(list: 8), new DiskWindow(list: 9) };

		public static void Create(string path = null, IEnumerable<string> files = null, DiskTabs diskTabs = null, bool forceCreate = false)
		{
			CreateTab(new DiskWindow(path, listFiles: files), diskTabs, forceCreate);
		}

		DiskTabs()
		{
			DiskMenuItem.RegisterCommands(this, (s, e, command) => RunCommand(command, shiftDown));
			InitializeComponent();
			ItemTabs = tabs;
			UIHelper.AuditMenu(menu);
		}

		public static DiskWindow GetList(int list)
		{
			return Lists[list - 1];
		}

		void Command_File_New(bool newWindow)
		{
			if (newWindow)
				new DiskTabs();
			else
				CreateTab(new DiskWindow(Directory.GetCurrentDirectory()), this, newWindow);
		}

		void Command_View_List(int list)
		{
			var listObj = GetList(list);
			var parent = listObj.TabsParent;
			if (parent != null)
				parent.Items.Remove(listObj);

			tabs.CreateTab(listObj);
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

			if (ItemTabs.TopMost == null)
				return;

			switch (command)
			{
				case DiskCommand.File_Close: ItemTabs.Items.Remove(ItemTabs.TopMost); break;
				case DiskCommand.File_Rename: ItemTabs.TopMost.Command_File_Rename(); break;
				case DiskCommand.File_Identify: ItemTabs.TopMost.Command_File_Identify(); break;
				case DiskCommand.File_Hash: ItemTabs.TopMost.Command_File_Hash(); break;
				case DiskCommand.File_VCS: ItemTabs.TopMost.Command_File_VCS(); break;
				case DiskCommand.File_Delete: ItemTabs.TopMost.Command_File_Delete(); break;
				case DiskCommand.Edit_Cut: ItemTabs.TopMost.Command_Edit_CutCopy(true); break;
				case DiskCommand.Edit_Copy: ItemTabs.TopMost.Command_Edit_CutCopy(false); break;
				case DiskCommand.Edit_Paste: ItemTabs.TopMost.Command_Edit_Paste(); break;
				case DiskCommand.Edit_Find: ItemTabs.TopMost.Command_Edit_Find(); break;
				case DiskCommand.Edit_FindBinary: ItemTabs.TopMost.Command_Edit_FindBinary(); break;
				case DiskCommand.Edit_FindText: ItemTabs.TopMost.Command_Edit_FindText(); break;
				case DiskCommand.Edit_ToList1: ItemTabs.TopMost.Command_Edit_ToList(1); break;
				case DiskCommand.Edit_ToList2: ItemTabs.TopMost.Command_Edit_ToList(2); break;
				case DiskCommand.Edit_ToList3: ItemTabs.TopMost.Command_Edit_ToList(3); break;
				case DiskCommand.Edit_ToList4: ItemTabs.TopMost.Command_Edit_ToList(4); break;
				case DiskCommand.Edit_ToList5: ItemTabs.TopMost.Command_Edit_ToList(5); break;
				case DiskCommand.Edit_ToList6: ItemTabs.TopMost.Command_Edit_ToList(6); break;
				case DiskCommand.Edit_ToList7: ItemTabs.TopMost.Command_Edit_ToList(7); break;
				case DiskCommand.Edit_ToList8: ItemTabs.TopMost.Command_Edit_ToList(8); break;
				case DiskCommand.Edit_ToList9: ItemTabs.TopMost.Command_Edit_ToList(9); break;
				case DiskCommand.Edit_TextEdit: ItemTabs.TopMost.Command_Edit_TextEdit(); break;
				case DiskCommand.Edit_HexEdit: ItemTabs.TopMost.Command_Edit_HexEdit(); break;
				case DiskCommand.Select_All: ItemTabs.TopMost.Command_Select_All(); break;
				case DiskCommand.Select_None: ItemTabs.TopMost.Command_Select_None(); break;
				case DiskCommand.Select_Invert: ItemTabs.TopMost.Command_Select_Invert(); break;
				case DiskCommand.Select_Directories: ItemTabs.TopMost.Command_Select_Directories(); break;
				case DiskCommand.Select_Files: ItemTabs.TopMost.Command_Select_Files(); break;
				case DiskCommand.Select_Unique: ItemTabs.TopMost.Command_Select_Unique(); break;
				case DiskCommand.Select_Duplicates: ItemTabs.TopMost.Command_Select_Duplicates(); break;
				case DiskCommand.Select_AddCopiedCut: ItemTabs.TopMost.Command_Select_AddCopiedCut(); break;
				case DiskCommand.Select_Remove: ItemTabs.TopMost.Command_Select_Remove(); break;
				case DiskCommand.Select_RemoveWithChildren: ItemTabs.TopMost.Command_Select_RemoveWithChildren(); break;
				case DiskCommand.View_DiskUsage: ItemTabs.TopMost.Command_View_DiskUsage(); break;
			}
		}

		internal void ToggleColumn(DependencyProperty property)
		{
			if (ItemTabs.TopMost != null)
				ItemTabs.TopMost.ToggleColumn(property);
		}

		internal void SetSort(DependencyProperty property)
		{
			if (ItemTabs.TopMost != null)
				ItemTabs.TopMost.SetSort(property);
		}
	}
}
