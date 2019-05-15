using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using NeoEdit.Content;
using NeoEdit.Transform;

namespace NeoEdit
{
	public interface ITabs
	{
		ObservableCollection<ITextEditor> Items { get; }
		ITextEditor TopMost { get; set; }
		List<string> GetClipboard(ITextEditor textEditor);
		void AddClipboardStrings(IEnumerable<string> strings, bool? isCut = null);
		void QueueDoActivated();
		void QueueUpdateCounts();
		ITextEditor Add(string fileName = null, string displayName = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, ParserType contentType = ParserType.None, bool? modified = null, int? line = null, int? column = null, ShutdownData shutdownData = null, int? index = null);
		bool TabIsActive(ITextEditor item);
		int GetIndex(ITextEditor item, bool activeOnly = false);
		void Remove(ITextEditor item);
		int ActiveCount { get; }
		void NotifyActiveChanged();
		Dispatcher Dispatcher { get; }
	}
}
