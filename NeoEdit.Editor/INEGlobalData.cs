using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public interface INEGlobalData
	{
		int NESerial { get; }

		IReadOnlyOrderedHashSet<INEWindowData> NEWindowDatas { get; }
		IReadOnlyOrderedHashSet<NEWindow> NEWindows { get; }
	}
}
