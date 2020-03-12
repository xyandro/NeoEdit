using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Dialogs;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program
{
	partial class TabsWindow
	{
		static public void Execute_Window_NewWindow() => new TabsWindow(true);

		void Execute_Window_Full() => SetLayout(1, 1);

		void Execute_Window_Grid() => SetLayout(maxColumns: 5, maxRows: 5);

		WindowCustomGridDialog.Result ConfigureExecute_Window_CustomGrid() => WindowCustomGridDialog.Run(this, Columns, Rows, MaxColumns, MaxRows);

		void Execute_Window_CustomGrid(WindowCustomGridDialog.Result result) => SetLayout(result.Columns, result.Rows, result.MaxColumns, result.MaxRows);

		void Execute_Window_ActiveTabs()
		{
			//TODOWindowActiveTabsDialog.Run(this);
		}

		void Execute_Window_Font_Size() => WindowFontSizeDialog.Run(this);

		void Execute_Window_Select_AllTabs() => ActiveTabs = Tabs;

		void Execute_Window_Select_NoTabs() => ActiveTabs = new List<TextEditor>();

		void Execute_Window_Select_TabsWithWithoutSelections(bool hasSelections) => ActiveTabs = ActiveTabs.Where(tab => tab.HasSelections == hasSelections).ToList();

		void Execute_Window_Select_ModifiedUnmodifiedTabs(bool modified) => ActiveTabs = ActiveTabs.Where(tab => tab.IsModified == modified).ToList();

		void Execute_Window_Select_InactiveTabs() => ActiveTabs = Tabs.Except(ActiveTabs).ToList();

		void Execute_Window_Close_TabsWithWithoutSelections(bool hasSelections)
		{
			var toClose = ActiveTabs.Where(tab => tab.HasSelections == hasSelections).ToList();
			if (!toClose.All(tab => tab.CanClose()))
				return;
			toClose.ForEach(tab => RemoveTextEditor(tab));
		}

		void Execute_Window_Close_ModifiedUnmodifiedTabs(bool modified)
		{
			var toClose = ActiveTabs.Where(tab => tab.IsModified == modified).ToList();
			if (!toClose.All(tab => tab.CanClose()))
				return;
			toClose.ForEach(tab => RemoveTextEditor(tab));
		}

		void Execute_Window_Close_ActiveInactiveTabs(bool active)
		{
			var toClose = (active ? ActiveTabs : Tabs.Except(ActiveTabs)).ToList();
			if (!toClose.All(tab => tab.CanClose()))
				return;
			toClose.ForEach(tab => RemoveTextEditor(tab));
		}

		void Execute_Window_WordList()
		{
			byte[] data;
			var streamName = typeof(TabsWindow).Assembly.GetManifestResourceNames().Where(name => name.EndsWith(".Words.txt.gz")).Single();
			using (var stream = typeof(TabsWindow).Assembly.GetManifestResourceStream(streamName))
			using (var ms = new MemoryStream())
			{
				stream.CopyTo(ms);
				data = ms.ToArray();
			}

			data = Compressor.Decompress(data, Compressor.Type.GZip);
			data = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(data).Replace("\n", "\r\n"));
			AddTextEditor(new TextEditor(bytes: data, modified: false));
		}
	}
}
