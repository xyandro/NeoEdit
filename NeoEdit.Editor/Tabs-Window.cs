using System.IO;
using System.Linq;
using System.Text;
using NeoEdit.Common;
using NeoEdit.Common.Transform;

namespace NeoEdit.Editor
{
	partial class Tabs
	{
		static public void Execute_Window_NewWindow() => new Tabs(true);

		void Execute_Window_Full() => SetLayout(new WindowLayout(1, 1));

		void Execute_Window_Grid() => SetLayout(new WindowLayout(maxColumns: 5, maxRows: 5));

		WindowLayout Configure_Window_CustomGrid() => TabsWindow.RunWindowCustomGridDialog(WindowLayout);

		void Execute_Window_CustomGrid(WindowLayout windowLayout) => SetLayout(windowLayout);

		void Execute_Window_ActiveTabs()
		{
			//TODOWindowActiveTabsDialog.Run(TabsWindow);
		}

		void Execute_Window_Font_Size() => TabsWindow.RunWindowFontSizeDialog();

		void Execute_Window_Font_ShowSpecial(bool? multiStatus) => Font.ShowSpecialChars = multiStatus != true;

		void Execute_Window_Select_AllTabs() => AllTabs.ForEach(tab => SetActive(tab));

		void Execute_Window_Select_NoTabs() => ClearAllActive();

		void Execute_Window_Select_TabsWithWithoutSelections(bool hasSelections) => ActiveTabs.ForEach(tab => SetActive(tab, tab.Selections.Any() == hasSelections));

		void Execute_Window_Select_ModifiedUnmodifiedTabs(bool modified) => ActiveTabs.ForEach(tab => SetActive(tab, tab.IsModified == modified));

		void Execute_Window_Select_InactiveTabs() => AllTabs.ForEach((System.Action<Tab>)(tab => SetActive(tab, !Enumerable.Contains<Tab>(this.ActiveTabs, (Tab)tab))));

		void Execute_Window_Close_TabsWithWithoutSelections(bool hasSelections)
		{
			foreach (var tab in ActiveTabs.Where(tab => tab.Selections.Any() == hasSelections))
			{
				tab.VerifyCanClose();
				RemoveTab(tab);
			}
		}

		void Execute_Window_Close_ModifiedUnmodifiedTabs(bool modified)
		{
			foreach (var tab in ActiveTabs.Where(tab => tab.IsModified == modified))
			{
				tab.VerifyCanClose();
				RemoveTab(tab);
			}
		}

		void Execute_Window_Close_ActiveInactiveTabs(bool active)
		{
			foreach (var tab in (active ? ActiveTabs : AllTabs.Except(ActiveTabs)))
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
