using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using NeoEdit.Common.Transform;

namespace NeoEdit.Common
{
	static public class ITabsCreator
	{
		public delegate ITabs CreateTabsDelegate(bool addEmpty = false);
		static public CreateTabsDelegate CreateTabs { get; set; }
	}

	public interface ITabs
	{
		ObservableCollection<TextEditor> Items { get; }
		TextEditor TopMost { get; set; }
		List<string> GetClipboard(TextEditor textEditor);
		void AddClipboardStrings(IEnumerable<string> strings, bool? isCut = null);
		void QueueDoActivated();
		void QueueUpdateCounts();
		void Add(TextEditor te, int? index = null);
		TextEditor Add(string fileName = null, string displayName = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, ParserType contentType = ParserType.None, bool? modified = null, int? line = null, int? column = null, ShutdownData shutdownData = null, int? index = null);
		Window AddDiff(string fileName1 = null, string displayName1 = null, byte[] bytes1 = null, Coder.CodePage codePage1 = Coder.CodePage.AutoByBOM, ParserType contentType1 = ParserType.None, bool? modified1 = null, int? line1 = null, int? column1 = null, ShutdownData shutdownData1 = null, string fileName2 = null, string displayName2 = null, byte[] bytes2 = null, Coder.CodePage codePage2 = Coder.CodePage.AutoByBOM, ParserType contentType2 = ParserType.None, bool? modified2 = null, int? line2 = null, int? column2 = null, ShutdownData shutdownData2 = null);
		bool TabIsActive(TextEditor item);
		int GetIndex(TextEditor item, bool activeOnly = false);
		void Remove(TextEditor item);
		int ActiveCount { get; }
		void NotifyActiveChanged();
		Dispatcher Dispatcher { get; }
		Window WindowParent { get; }
		TabsLayout Layout { get; }
		int? Columns { get; }
		int? Rows { get; }
		void SetLayout(TabsLayout layout, int? columns = null, int? rows = null);
		void UpdateTopMost();
		void MoveToTop(IEnumerable<TextEditor> tabs);
		bool HandleKey(Key key, bool shiftDown, bool controlDown, bool altDown);
		bool HandleText(string text);
		bool HandleCommand(NECommand command, bool shiftDown, object dialogResult, bool? multiStatus);
		Macro RecordingMacro { get; set; }
		Macro MacroPlaying { get; set; }
	}
}
