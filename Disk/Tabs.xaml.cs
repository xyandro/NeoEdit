using System.Collections.ObjectModel;
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
		public Tabs.ItemData Active { get { return UIHelper<DiskTabs>.GetPropValue<Tabs.ItemData>(this); } set { UIHelper<DiskTabs>.SetPropValue(this, value); } }
		[DepProp]
		public Tabs.ViewType View { get { return UIHelper<DiskTabs>.GetPropValue<Tabs.ViewType>(this); } set { UIHelper<DiskTabs>.SetPropValue(this, value); } }

		static DiskTabs() { UIHelper<DiskTabs>.Register(); }

		public DiskTabs(string path = null)
		{
			DiskMenuItem.RegisterCommands(this, (s, e, command) => RunCommand(command, shiftDown));
			InitializeComponent();
			UIHelper.AuditMenu(menu);

			DiskWindows = new ObservableCollection<Tabs<DiskWindow>.ItemData>();
			Add(new DiskWindow(path));
		}

		bool shiftDown { get { return (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None; } }

		void Command_File_New(bool newWindow)
		{
			if (newWindow)
				new DiskTabs();
			else
				Add(new DiskWindow());
		}

		void RunCommand(DiskCommand command, bool shiftDown)
		{
			switch (command)
			{
				case DiskCommand.File_NewTab: Command_File_New(shiftDown); break;
				case DiskCommand.File_Exit: Close(); break;
				case DiskCommand.View_Tiles: View = View == Tabs.ViewType.Tiles ? Tabs.ViewType.Tabs : Tabs.ViewType.Tiles; break;
			}

			if (Active == null)
				return;

			switch (command)
			{
				case DiskCommand.File_Close: DiskWindows.Remove(Active); break;
				case DiskCommand.File_Rename: Active.Item.Command_File_Rename(); break;
				case DiskCommand.File_Identify: Active.Item.Command_File_Identify(); break;
				case DiskCommand.File_MD5: Active.Item.Command_File_MD5(); break;
				case DiskCommand.File_SHA1: Active.Item.Command_File_SHA1(); break;
				case DiskCommand.File_Svn: Active.Item.Command_File_Svn(); break;
				case DiskCommand.File_Delete: Active.Item.Command_File_Delete(); break;
				case DiskCommand.Edit_Cut: Active.Item.Command_Edit_CutCopy(true); break;
				case DiskCommand.Edit_Copy: Active.Item.Command_Edit_CutCopy(false); break;
				case DiskCommand.Edit_Paste: Active.Item.Command_Edit_Paste(); break;
				case DiskCommand.Edit_Find: Active.Item.Command_Edit_Find(); break;
				case DiskCommand.Edit_FindBinary: Active.Item.Command_Edit_FindBinary(); break;
				case DiskCommand.Edit_FindText: Active.Item.Command_Edit_FindText(); break;
				case DiskCommand.Edit_TextEdit: Active.Item.Command_Edit_TextEdit(); break;
				case DiskCommand.Edit_HexEdit: Active.Item.Command_Edit_HexEdit(); break;
				case DiskCommand.Select_All: Active.Item.Command_Select_All(); break;
				case DiskCommand.Select_None: Active.Item.Command_Select_None(); break;
				case DiskCommand.Select_Invert: Active.Item.Command_Select_Invert(); break;
				case DiskCommand.Select_Directories: Active.Item.Command_Select_Directories(); break;
				case DiskCommand.Select_Files: Active.Item.Command_Select_Files(); break;
				case DiskCommand.Select_AddCopiedCut: Active.Item.Command_Select_AddCopiedCut(); break;
				case DiskCommand.Select_Remove: Active.Item.Command_Select_Remove(); break;
				case DiskCommand.Select_RemoveWithChildren: Active.Item.Command_Select_RemoveWithChildren(); break;
				case DiskCommand.View_DiskUsage: Active.Item.Command_View_DiskUsage(); break;
			}
		}

		internal void ToggleColumn(DependencyProperty property)
		{
			if (Active != null)
				Active.Item.ToggleColumn(property);
		}

		internal void SetSort(DependencyProperty property)
		{
			if (Active != null)
				Active.Item.SetSort(property);
		}

		void Add(DiskWindow diskWindow)
		{
			var add = new Tabs.ItemData(diskWindow);
			DiskWindows.Add(add);
			Active = add;
		}
	}
}
