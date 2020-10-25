using System.IO;
using System.Linq;
using System.Text;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Models;
using NeoEdit.Common.Transform;

namespace NeoEdit.Editor
{
	partial class Tabs
	{
		static void Execute_Window_New_NewWindow() => new Tabs(true);

		void Execute_Window_New_FromClipboards()
		{
			var tabs = new Tabs();
			tabs.BeginTransaction(state);
			AddTabsFromClipboards(tabs);
			tabs.Commit();
		}

		void Execute_Window_New_FromClipboardSelections()
		{
			var tabs = new Tabs();
			tabs.BeginTransaction(state);
			AddTabsFromClipboardSelections(tabs);
			tabs.Commit();
		}

		void Execute_Window_Full() => SetLayout(new WindowLayout(1, 1));

		void Execute_Window_Grid() => SetLayout(new WindowLayout(maxColumns: 4, maxRows: 4));

		static Configuration_Window_CustomGrid Configure_Window_CustomGrid(EditorExecuteState state) => state.Tabs.TabsWindow.Configure_Window_CustomGrid(state.Tabs.WindowLayout);

		void Execute_Window_CustomGrid() => SetLayout((state.Configuration as Configuration_Window_CustomGrid).WindowLayout);

		void Execute_Window_ActiveTabs()
		{
			var data = new WindowActiveTabsDialogData();
			void RecalculateData()
			{
				data.AllTabs = AllTabs.Select(tab => tab.TabLabel).ToList();
				data.ActiveIndexes = ActiveTabs.Select(tab => AllTabs.IndexOf(tab)).ToList();
				data.FocusedIndex = AllTabs.IndexOf(Focused);
			}
			RecalculateData();
			data.SetActiveIndexes = list =>
			{
				ClearAllActive();
				list.Select(index => AllTabs[index]).ForEach(tab => SetActive(tab));
				RecalculateData();
				RenderTabsWindow();
			};
			data.CloseTabs = list =>
			{
				var tabs = list.Select(index => AllTabs[index]).ToList();
				tabs.ForEach(tab => tab.VerifyCanClose());
				tabs.ForEach(tab => RemoveTab(tab));
				RecalculateData();
				RenderTabsWindow();
			};
			data.DoMoves = moves =>
			{
				moves.ForEach(((int oldIndex, int newIndex) move) => MoveTab(AllTabs[move.oldIndex], move.newIndex));
				RecalculateData();
				RenderTabsWindow();
			};

			TabsWindow.RunWindowActiveTabsDialog(data);
		}

		void Execute_Window_Font_Size() => TabsWindow.RunWindowFontSizeDialog();

		void Execute_Window_Font_ShowSpecial(bool? multiStatus) => Font.ShowSpecialChars = multiStatus != true;

		void Execute_Window_Select_AllTabs()
		{
			AllTabs.ForEach(tab => SetActive(tab));
			Focused = AllTabs.FirstOrDefault();
		}

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
			data = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(data));
			AddTab(new Tab(displayName: "Word List", bytes: data, modified: false));
		}
	}
}
