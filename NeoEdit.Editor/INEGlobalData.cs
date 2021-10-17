using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public interface INEGlobalData
	{
		long NESerial { get; }

		IReadOnlyOrderedHashSet<INEWindowData> NEWindowDatas { get; }
		IReadOnlyOrderedHashSet<NEWindow> NEWindows { get; }

		INEGlobalData Next();
	}
}
