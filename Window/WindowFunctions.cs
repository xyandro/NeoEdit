using System.IO;
using System.Linq;
using System.Text;
using NeoEdit.Controls;
using NeoEdit.Dialogs;
using NeoEdit.Transform;

namespace NeoEdit
{
	public static class WindowFunctions
	{
		static public void Command_Window_NewWindow() => ITabsCreator.CreateTabs();

		static public void Command_Window_Type(ITabs tabs, TabsLayout layout, WindowCustomGridDialog.Result result) => tabs.SetLayout(layout, result?.Columns, result?.Rows);

		static public WindowCustomGridDialog.Result Command_Window_Type_Dialog(ITabs tabs) => WindowCustomGridDialog.Run(tabs.WindowParent, tabs.Columns, tabs.Rows);

		static public void Command_Window_ActiveTabs(ITabs tabs)
		{
			WindowActiveTabsDialog.Run(tabs);
			tabs.UpdateTopMost();
		}

		static public void Command_Window_TabIndex(ITextEditor te, bool activeOnly)
		{
			te.ReplaceSelections((te.TabsParent.GetIndex(te, activeOnly) + 1).ToString());
		}

		static public void Command_Window_FontSize(ITabs tabs) => WindowFontSizeDialog.Run(tabs.WindowParent);

		static public void Command_Window_SelectTabsWithSelections(ITabs tabs, bool hasSelections)
		{
			var topMost = tabs.TopMost;
			var active = tabs.Items.Where(tab => (tab.Active) && (tab.HasSelections == hasSelections)).ToList();
			tabs.Items.ToList().ForEach(tab => tab.Active = false);

			if (!active.Any())
				return;

			if (!active.Contains(topMost))
				topMost = active.First();
			tabs.TopMost = topMost;
			active.ForEach(tab => tab.Active = true);
		}

		static public void Command_Window_Select_TabsWithSelectionsToTop(ITabs tabs)
		{
			var topMost = tabs.TopMost;
			var active = tabs.Items.Where(tab => tab.Active).ToList();
			var hasSelections = active.Where(tab => tab.HasSelections).ToList();
			if ((!active.Any()) || (!hasSelections.Any()))
				return;

			tabs.MoveToTop(hasSelections);
			if (!active.Contains(topMost))
				topMost = active.First();
			tabs.TopMost = topMost;
			active.ForEach(tab => tab.Active = true);
		}

		static public void Command_Window_CloseTabsWithSelections(ITabs tabs, bool hasSelections)
		{
			var topMost = tabs.TopMost;
			var active = tabs.Items.Where(tab => (tab.Active) && (tab.HasSelections != hasSelections)).ToList();

			var answer = new AnswerResult();
			var closeTabs = tabs.Items.Where(tab => (tab.Active) && (tab.HasSelections == hasSelections)).ToList();
			if (!closeTabs.All(tab => tab.CanClose(answer)))
				return;
			closeTabs.ForEach(tab => tabs.Remove(tab));

			if (!active.Any())
				return;

			if (!active.Contains(topMost))
				topMost = active.First();
			tabs.TopMost = topMost;
			active.ForEach(tab => tab.Active = true);
		}

		static public void Command_Window_Close_ActiveTabs(ITabs tabs, bool active)
		{
			var answer = new AnswerResult();
			var closeTabs = tabs.Items.Where(tab => tab.Active == active).ToList();
			if (!closeTabs.All(tab => tab.CanClose(answer)))
				return;
			closeTabs.ForEach(tab => tabs.Remove(tab));
		}

		static public void Command_Window_WordList(ITabs tabs)
		{
			var type = tabs.GetType();
			byte[] data;
			var streamName = typeof(WindowFunctions).Assembly.GetManifestResourceNames().Where(name => name.EndsWith(".Words.txt.gz")).Single();
			using (var stream = type.Assembly.GetManifestResourceStream(streamName))
			using (var ms = new MemoryStream())
			{
				stream.CopyTo(ms);
				data = ms.ToArray();
			}

			data = Compressor.Decompress(data, Compressor.Type.GZip);
			data = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(data).Replace("\n", "\r\n"));
			tabs.Add(bytes: data, modified: false);
		}
	}
}
