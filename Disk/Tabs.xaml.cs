using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NeoEdit.GUI.Controls;

namespace NeoEdit.Disk
{
	public class Tabs : Tabs<DiskWindow> { }

	public partial class DiskTabs
	{
		[DepProp]
		public ObservableCollection<Tabs.ItemData> DiskWindows { get { return UIHelper<DiskTabs>.GetPropValue<ObservableCollection<Tabs.ItemData>>(this); } set { UIHelper<DiskTabs>.SetPropValue(this, value); } }
		[DepProp]
		public Tabs.ItemData TopMost { get { return UIHelper<DiskTabs>.GetPropValue<Tabs.ItemData>(this); } set { UIHelper<DiskTabs>.SetPropValue(this, value); } }
		[DepProp]
		public bool Tiles { get { return UIHelper<DiskTabs>.GetPropValue<bool>(this); } set { UIHelper<DiskTabs>.SetPropValue(this, value); } }

		static DiskTabs() { UIHelper<DiskTabs>.Register(); }

		static List<DiskWindow> Lists = new List<DiskWindow> { new DiskWindow(list: 1), new DiskWindow(list: 2), new DiskWindow(list: 3), new DiskWindow(list: 4), new DiskWindow(list: 5), new DiskWindow(list: 6), new DiskWindow(list: 7), new DiskWindow(list: 8), new DiskWindow(list: 9) };

		public DiskTabs(string path = null)
		{
			DiskMenuItem.RegisterCommands(this, (s, e, command) => RunCommand(command, shiftDown));
			InitializeComponent();
			UIHelper.AuditMenu(menu);

			DiskWindows = new ObservableCollection<Tabs<DiskWindow>.ItemData>();
			Add(new DiskWindow(path));
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
			{
				var itemData = parent.Items.Where(item => item.Item == listObj).SingleOrDefault();
				if (itemData != null)
					parent.Items.Remove(itemData);
			}

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
				case DiskCommand.File_Rename: TopMost.Item.Command_File_Rename(); break;
				case DiskCommand.File_Identify: TopMost.Item.Command_File_Identify(); break;
				case DiskCommand.File_MD5: TopMost.Item.Command_File_MD5(); break;
				case DiskCommand.File_SHA1: TopMost.Item.Command_File_SHA1(); break;
				case DiskCommand.File_Svn: TopMost.Item.Command_File_Svn(); break;
				case DiskCommand.File_Delete: TopMost.Item.Command_File_Delete(); break;
				case DiskCommand.Edit_Cut: TopMost.Item.Command_Edit_CutCopy(true); break;
				case DiskCommand.Edit_Copy: TopMost.Item.Command_Edit_CutCopy(false); break;
				case DiskCommand.Edit_Paste: TopMost.Item.Command_Edit_Paste(); break;
				case DiskCommand.Edit_Find: TopMost.Item.Command_Edit_Find(); break;
				case DiskCommand.Edit_FindBinary: TopMost.Item.Command_Edit_FindBinary(); break;
				case DiskCommand.Edit_FindText: TopMost.Item.Command_Edit_FindText(); break;
				case DiskCommand.Edit_ToList1: TopMost.Item.Command_Edit_ToList(1); break;
				case DiskCommand.Edit_ToList2: TopMost.Item.Command_Edit_ToList(2); break;
				case DiskCommand.Edit_ToList3: TopMost.Item.Command_Edit_ToList(3); break;
				case DiskCommand.Edit_ToList4: TopMost.Item.Command_Edit_ToList(4); break;
				case DiskCommand.Edit_ToList5: TopMost.Item.Command_Edit_ToList(5); break;
				case DiskCommand.Edit_ToList6: TopMost.Item.Command_Edit_ToList(6); break;
				case DiskCommand.Edit_ToList7: TopMost.Item.Command_Edit_ToList(7); break;
				case DiskCommand.Edit_ToList8: TopMost.Item.Command_Edit_ToList(8); break;
				case DiskCommand.Edit_ToList9: TopMost.Item.Command_Edit_ToList(9); break;
				case DiskCommand.Edit_TextEdit: TopMost.Item.Command_Edit_TextEdit(); break;
				case DiskCommand.Edit_HexEdit: TopMost.Item.Command_Edit_HexEdit(); break;
				case DiskCommand.Select_All: TopMost.Item.Command_Select_All(); break;
				case DiskCommand.Select_None: TopMost.Item.Command_Select_None(); break;
				case DiskCommand.Select_Invert: TopMost.Item.Command_Select_Invert(); break;
				case DiskCommand.Select_Directories: TopMost.Item.Command_Select_Directories(); break;
				case DiskCommand.Select_Files: TopMost.Item.Command_Select_Files(); break;
				case DiskCommand.Select_Unique: TopMost.Item.Command_Select_Unique(); break;
				case DiskCommand.Select_Duplicates: TopMost.Item.Command_Select_Duplicates(); break;
				case DiskCommand.Select_AddCopiedCut: TopMost.Item.Command_Select_AddCopiedCut(); break;
				case DiskCommand.Select_Remove: TopMost.Item.Command_Select_Remove(); break;
				case DiskCommand.Select_RemoveWithChildren: TopMost.Item.Command_Select_RemoveWithChildren(); break;
				case DiskCommand.View_DiskUsage: TopMost.Item.Command_View_DiskUsage(); break;
			}
		}

		internal void ToggleColumn(DependencyProperty property)
		{
			if (TopMost != null)
				TopMost.Item.ToggleColumn(property);
		}

		internal void SetSort(DependencyProperty property)
		{
			if (TopMost != null)
				TopMost.Item.SetSort(property);
		}

		void Add(DiskWindow diskWindow)
		{
			var add = new Tabs.ItemData(diskWindow);
			DiskWindows.Add(add);
			TopMost = add;
		}
	}
}
