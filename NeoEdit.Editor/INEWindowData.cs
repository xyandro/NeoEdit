using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public interface INEWindowData
	{
		int NESerial { get; }
		NEWindow NEWindow { get; }

		IReadOnlyOrderedHashSet<INEFileData> NEFileDatas { get; }
		IReadOnlyOrderedHashSet<NEFile> NEFiles { get; }
		IReadOnlyOrderedHashSet<NEFile> OrderedNEFiles { get; }
		IReadOnlyOrderedHashSet<NEFile> ActiveFiles { get; }
		NEFile Focused { get; }
	}
}
