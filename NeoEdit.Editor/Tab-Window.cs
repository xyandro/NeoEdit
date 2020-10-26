using System.IO;
using System.Linq;
using System.Text;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Models;
using NeoEdit.Common.Transform;
using NeoEdit.Editor.PreExecution;
using NeoEdit.TaskRunning;

namespace NeoEdit.Editor
{
	partial class Tab
	{
		static PreExecutionStop PreExecute_Window_New_NewWindow(EditorExecuteState state)
		{
			new Tabs(true);
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_New_FromSelections(EditorExecuteState state)
		{
			var newTabs = state.Tabs.ActiveTabs.AsTaskRunner().SelectMany(tab => tab.Selections.AsTaskRunner().Select(range => tab.Text.GetString(range)).Select(str => new Tab(bytes: Coder.StringToBytes(str, Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, contentType: tab.ContentType, modified: false)).ToList()).ToList();
			newTabs.ForEach((tab, index) =>
			{
				tab.BeginTransaction(state);
				tab.DisplayName = $"Selection {index + 1}";
				tab.Commit();
			});

			var tabs = new Tabs();
			tabs.BeginTransaction(state);
			newTabs.ForEach(tab => tabs.AddTab(tab));
			tabs.Commit();

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_New_FromClipboards(EditorExecuteState state)
		{
			var tabs = new Tabs();
			tabs.BeginTransaction(state);
			Tabs.AddTabsFromClipboards(tabs);
			tabs.Commit();

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_New_FromClipboardSelections(EditorExecuteState state)
		{
			var tabs = new Tabs();
			tabs.BeginTransaction(state);
			Tabs.AddTabsFromClipboardSelections(tabs);
			tabs.Commit();

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_Full(EditorExecuteState state)
		{
			state.Tabs.SetLayout(new WindowLayout(1, 1));
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_Grid(EditorExecuteState state)
		{
			state.Tabs.SetLayout(new WindowLayout(maxColumns: 4, maxRows: 4));
			return PreExecutionStop.Stop;
		}

		static Configuration_Window_CustomGrid Configure_Window_CustomGrid(EditorExecuteState state) => state.Tabs.TabsWindow.Configure_Window_CustomGrid(state.Tabs.WindowLayout);

		static PreExecutionStop PreExecute_Window_CustomGrid(EditorExecuteState state)
		{
			state.Tabs.SetLayout((state.Configuration as Configuration_Window_CustomGrid).WindowLayout);
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_ActiveOnly(EditorExecuteState state, bool? multiStatus)
		{
			state.Tabs.ActiveOnly = multiStatus != true;
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_ActiveTabs(EditorExecuteState state)
		{
			var data = new WindowActiveTabsDialogData();
			void RecalculateData()
			{
				data.AllTabs = state.Tabs.AllTabs.Select(tab => tab.TabLabel).ToList();
				data.ActiveIndexes = state.Tabs.ActiveTabs.Select(tab => state.Tabs.AllTabs.IndexOf(tab)).ToList();
				data.FocusedIndex = state.Tabs.AllTabs.IndexOf(state.Tabs.Focused);
			}
			RecalculateData();
			data.SetActiveIndexes = list =>
			{
				state.Tabs.ClearAllActive();
				list.Select(index => state.Tabs.AllTabs[index]).ForEach(tab => state.Tabs.SetActive(tab));
				RecalculateData();
				state.Tabs.RenderTabsWindow();
			};
			data.CloseTabs = list =>
			{
				var tabs = list.Select(index => state.Tabs.AllTabs[index]).ToList();
				tabs.ForEach(tab => tab.VerifyCanClose());
				tabs.ForEach(tab => state.Tabs.RemoveTab(tab));
				RecalculateData();
				state.Tabs.RenderTabsWindow();
			};
			data.DoMoves = moves =>
			{
				moves.ForEach(((int oldIndex, int newIndex) move) => state.Tabs.MoveTab(state.Tabs.AllTabs[move.oldIndex], move.newIndex));
				RecalculateData();
				state.Tabs.RenderTabsWindow();
			};

			state.Tabs.TabsWindow.RunWindowActiveTabsDialog(data);

			return PreExecutionStop.Stop;
		}

		void Execute_Window_TabIndex(bool activeOnly)
		{
			ReplaceSelections((Tabs.GetTabIndex(this, activeOnly) + 1).ToString());
		}

		static PreExecutionStop PreExecute_Window_Font_Size(EditorExecuteState state)
		{
			state.Tabs.TabsWindow.RunWindowFontSizeDialog();
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_Font_ShowSpecial(EditorExecuteState state, bool? multiStatus)
		{
			Font.ShowSpecialChars = multiStatus != true;
			return PreExecutionStop.Stop;
		}

		void Execute_Window_ViewBinary() => ViewBinary = state.MultiStatus != true;

		static Configuration_Window_ViewBinaryCodePages Configure_Window_ViewBinaryCodePages(EditorExecuteState state) => state.Tabs.TabsWindow.Configure_Window_ViewBinaryCodePages(state.Tabs.Focused.ViewBinaryCodePages);

		void Execute_Window_ViewBinaryCodePages() => ViewBinaryCodePages = (state.Configuration as Configuration_Window_ViewBinaryCodePages).CodePages;

		static PreExecutionStop PreExecute_Window_Select_AllTabs(EditorExecuteState state)
		{
			state.Tabs.AllTabs.ForEach(tab => state.Tabs.SetActive(tab));
			state.Tabs.Focused = state.Tabs.AllTabs.FirstOrDefault();

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_Select_NoTabs(EditorExecuteState state)
		{
			state.Tabs.ClearAllActive();
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_Select_TabsWithWithoutSelections(EditorExecuteState state, bool hasSelections)
		{
			state.Tabs.ActiveTabs.ForEach(tab => state.Tabs.SetActive(tab, tab.Selections.Any() == hasSelections));
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_Select_ModifiedUnmodifiedTabs(EditorExecuteState state, bool modified)
		{
			state.Tabs.ActiveTabs.ForEach(tab => state.Tabs.SetActive(tab, tab.IsModified == modified));
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_Select_InactiveTabs(EditorExecuteState state)
		{
			state.Tabs.AllTabs.ForEach(tab => state.Tabs.SetActive(tab, !Enumerable.Contains<Tab>(state.Tabs.ActiveTabs, tab)));
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_Close_TabsWithWithoutSelections(EditorExecuteState state, bool hasSelections)
		{
			foreach (var tab in state.Tabs.ActiveTabs.Where(tab => tab.Selections.Any() == hasSelections))
			{
				tab.VerifyCanClose();
				state.Tabs.RemoveTab(tab);
			}

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_Close_ModifiedUnmodifiedTabs(EditorExecuteState state, bool modified)
		{
			foreach (var tab in state.Tabs.ActiveTabs.Where(tab => tab.IsModified == modified))
			{
				tab.VerifyCanClose();
				state.Tabs.RemoveTab(tab);
			}

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_Close_ActiveInactiveTabs(EditorExecuteState state, bool active)
		{
			foreach (var tab in (active ? state.Tabs.ActiveTabs : state.Tabs.AllTabs.Except(state.Tabs.ActiveTabs)))
			{
				tab.VerifyCanClose();
				state.Tabs.RemoveTab(tab);
			}

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_WordList(EditorExecuteState state)
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
			state.Tabs.AddTab(new Tab(displayName: "Word List", bytes: data, modified: false));

			return PreExecutionStop.Stop;
		}
	}
}
