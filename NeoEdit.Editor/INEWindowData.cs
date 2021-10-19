using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public interface INEWindowData
	{
		long NESerial { get; }
		NEWindow NEWindow { get; }

		IReadOnlyOrderedHashSet<INEFileData> NEFileDatas { get; }
		IReadOnlyOrderedHashSet<NEFile> NEFiles { get; }
		IReadOnlyOrderedHashSet<NEFile> ActiveFiles { get; }
		NEFile Focused { get; }
		WindowLayout WindowLayout { get; }
		bool WorkMode { get; }

		INEWindowData Undo { get; }
		INEWindowData Redo { get; }
		INEWindowData RedoText { get; }
		bool TextChanged { get; }

		INEWindowData Next();
	}
}
