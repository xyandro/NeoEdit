using System.IO;
using System.Linq;
using System.Text;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Dialogs;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program
{
	partial class Tabs
	{
		static public void Command_Window_NewWindow() => new Tabs(true);

		void Command_Window_Type(TabsLayout layout, WindowCustomGridDialog.Result result) => SetLayout(layout, result?.Columns, result?.Rows);

		WindowCustomGridDialog.Result Command_Window_Type_Dialog() => WindowCustomGridDialog.Run(WindowParent, Columns, Rows);

		void Command_Window_ActiveTabs()
		{
			WindowActiveTabsDialog.Run(this);
			UpdateTopMost();
		}

		void Command_Window_Font_Size() => WindowFontSizeDialog.Run(WindowParent);

		void Command_Window_Select_TabsWithWithoutSelections(bool hasSelections)
		{
			var topMost = TopMost;
			var active = Items.Where(tab => (tab.Active) && (tab.HasSelections == hasSelections)).ToList();
			Items.ToList().ForEach(tab => tab.Active = false);

			if (!active.Any())
				return;

			if (!active.Contains(topMost))
				topMost = active.First();
			TopMost = topMost;
			active.ForEach(tab => tab.Active = true);
		}

		void Command_Window_Select_ModifiedUnmodifiedTabs(bool modified)
		{
			var topMost = TopMost;
			var active = Items.Where(tab => (tab.Active) && (tab.IsModified == modified)).ToList();
			Items.ToList().ForEach(tab => tab.Active = false);

			if (!active.Any())
				return;

			if (!active.Contains(topMost))
				topMost = active.First();
			TopMost = topMost;
			active.ForEach(tab => tab.Active = true);
		}

		void Command_Window_Select_TabsWithSelectionsToTop()
		{
			var topMost = TopMost;
			var active = Items.Where(tab => tab.Active).ToList();
			var hasSelections = active.Where(tab => tab.HasSelections).ToList();
			if ((!active.Any()) || (!hasSelections.Any()))
				return;

			MoveToTop(hasSelections);
			if (!active.Contains(topMost))
				topMost = active.First();
			TopMost = topMost;
			active.ForEach(tab => tab.Active = true);
		}

		void Command_Window_Close_TabsWithWithoutSelections(bool hasSelections)
		{
			var topMost = TopMost;
			var active = Items.Where(tab => (tab.Active) && (tab.HasSelections != hasSelections)).ToList();

			var answer = new AnswerResult();
			var closeTabs = Items.Where(tab => (tab.Active) && (tab.HasSelections == hasSelections)).ToList();
			if (!closeTabs.All(tab => tab.CanClose(answer)))
				return;
			closeTabs.ForEach(tab => Remove(tab));

			if (!active.Any())
				return;

			if (!active.Contains(topMost))
				topMost = active.First();
			TopMost = topMost;
			active.ForEach(tab => tab.Active = true);
		}

		void Command_Window_Close_ModifiedUnmodifiedTabs(bool modified)
		{
			var topMost = TopMost;
			var active = Items.Where(tab => (tab.Active) && (tab.IsModified != modified)).ToList();

			var answer = new AnswerResult();
			var closeTabs = Items.Where(tab => (tab.Active) && (tab.IsModified == modified)).ToList();
			if (!closeTabs.All(tab => tab.CanClose(answer)))
				return;
			closeTabs.ForEach(tab => Remove(tab));

			if (!active.Any())
				return;

			if (!active.Contains(topMost))
				topMost = active.First();
			TopMost = topMost;
			active.ForEach(tab => tab.Active = true);
		}

		void Command_Window_Close_ActiveInactiveTabs(bool active)
		{
			var answer = new AnswerResult();
			var closeTabs = Items.Where(tab => tab.Active == active).ToList();
			if (!closeTabs.All(tab => tab.CanClose(answer)))
				return;
			closeTabs.ForEach(tab => Remove(tab));
		}

		void Command_Window_WordList()
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
			Add(bytes: data, modified: false);
		}
	}
}
