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
		static public void Command_Window_NewWindow() => new TabsWindow(true);

		void Command_Window_Full() => SetLayout(1, 1);

		void Command_Window_Grid() => SetLayout(maxColumns: 5, maxRows: 5);

		WindowCustomGridDialog.Result Command_Window_CustomGrid_Dialog() => WindowCustomGridDialog.Run(this, Columns, Rows, MaxColumns, MaxRows);

		void Command_Window_CustomGrid(WindowCustomGridDialog.Result result) => SetLayout(result.Columns, result.Rows, result.MaxColumns, result.MaxRows);

		void Command_Window_ActiveTabs() => WindowActiveTabsDialog.Run(this);

		void Command_Window_Font_Size() => WindowFontSizeDialog.Run(this);

		void Command_Window_Select_AllTabs() => SetActive(Tabs);

		void Command_Window_Select_NoTabs() => SetActive();

		void Command_Window_Select_TabsWithWithoutSelections(bool hasSelections) => SetActive(ActiveTabs.Where(tab => tab.HasSelections == hasSelections).ToList());

		void Command_Window_Select_ModifiedUnmodifiedTabs(bool modified) => SetActive(ActiveTabs.Where(tab => tab.IsModified == modified).ToList());

		void Command_Window_Select_InactiveTabs() => SetActive(Tabs.Except(ActiveTabs).ToList());

		void Command_Window_Close_TabsWithWithoutSelections(bool hasSelections)
		{
			var toClose = ActiveTabs.Where(tab => tab.HasSelections == hasSelections).ToList();
			if (!toClose.All(tab => tab.CanClose()))
				return;
			toClose.ForEach(tab => RemoveTextEditor(tab));
		}

		void Command_Window_Close_ModifiedUnmodifiedTabs(bool modified)
		{
			var toClose = ActiveTabs.Where(tab => tab.IsModified == modified).ToList();
			if (!toClose.All(tab => tab.CanClose()))
				return;
			toClose.ForEach(tab => RemoveTextEditor(tab));
		}

		void Command_Window_Close_ActiveInactiveTabs(bool active)
		{
			var toClose = (active ? ActiveTabs : Tabs.Except(ActiveTabs)).ToList();
			if (!toClose.All(tab => tab.CanClose()))
				return;
			toClose.ForEach(tab => RemoveTextEditor(tab));
		}

		void Command_Window_WordList()
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
			AddTextEditor(new TextEditorData(bytes: data, modified: false));
		}
	}
}
