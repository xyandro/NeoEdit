using System.Collections.Generic;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public interface INEFileData
	{
		int NESerial { get; }
		NEFile NEFile { get; }

		NETextPoint NETextPoint { get; }
		IReadOnlyList<Range> Selections { get; }
		IReadOnlyList<Range>[] Regions { get; }

		INEFileData Undo { get; }
		INEFileData Redo { get; }
	}
}
