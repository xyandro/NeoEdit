using System.IO;
using System.Linq;
using System.Text;
using NeoEdit.Program.Dialogs;
using NeoEdit.Program.Models;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program
{
	partial class Tabs
	{
		static public void Execute_Window_NewWindow() => new TabsWindow(true);

		void Execute_Window_Full() => SetLayout(1, 1);

		void Execute_Window_Grid() => SetLayout(maxColumns: 5, maxRows: 5);

		WindowCustomGridDialogResult Configure_Window_CustomGrid() => WindowCustomGridDialog.Run(state.Window, Columns, Rows, MaxColumns, MaxRows);

		void Execute_Window_CustomGrid(WindowCustomGridDialogResult result) => SetLayout(result.Columns, result.Rows, result.MaxColumns, result.MaxRows);

		void Execute_Window_ActiveTabs()
		{
			//TODOWindowActiveTabsDialog.Run(TabsWindow);
		}

		void Execute_Window_Font_Size() => WindowFontSizeDialog.Run(state.Window);

		void Execute_Window_Font_ShowSpecial(bool? multiStatus) => Font.ShowSpecialChars = multiStatus != true;

		void Execute_Window_Select_AllTabs() => AllTabs.ForEach(tab => SetActive(tab));

		void Execute_Window_Select_NoTabs() => ClearAllActive();

		void Execute_Window_Select_TabsWithWithoutSelections(bool hasSelections) => UnsortedActiveTabs.ForEach(tab => SetActive(tab, tab.Selections.Any() == hasSelections));

		void Execute_Window_Select_ModifiedUnmodifiedTabs(bool modified) => UnsortedActiveTabs.ForEach(tab => SetActive(tab, tab.IsModified == modified));

		void Execute_Window_Select_InactiveTabs() => AllTabs.ForEach(tab => SetActive(tab, !UnsortedActiveTabs.Contains(tab)));

		void Execute_Window_Close_TabsWithWithoutSelections(bool hasSelections)
		{
			foreach (var tab in SortedActiveTabs.Where(tab => tab.Selections.Any() == hasSelections))
			{
				tab.VerifyCanClose();
				RemoveTab(tab);
			}
		}

		void Execute_Window_Close_ModifiedUnmodifiedTabs(bool modified)
		{
			foreach (var tab in SortedActiveTabs.Where(tab => tab.IsModified == modified))
			{
				tab.VerifyCanClose();
				RemoveTab(tab);
			}
		}

		void Execute_Window_Close_ActiveInactiveTabs(bool active)
		{
			foreach (var tab in (active ? SortedActiveTabs : AllTabs.Except(UnsortedActiveTabs)))
			{
				tab.VerifyCanClose();
				RemoveTab(tab);
			}
		}

		void Execute_Window_WordList()
		{
			byte[] data;
			var streamName = typeof(Tabs).Assembly.GetManifestResourceNames().Where(name => name.EndsWith(".Words.txt.gz")).Single();
			using (var stream = typeof(Tabs).Assembly.GetManifestResourceStream(streamName))
			using (var ms = new MemoryStream())
			{
				stream.CopyTo(ms);
				data = ms.ToArray();
			}

			data = Compressor.Decompress(data, Compressor.Type.GZip);
			data = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(data).Replace("\n", "\r\n"));
			AddTab(new Tab(bytes: data, modified: false));
		}
	}
}
