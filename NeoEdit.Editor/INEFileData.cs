using System.Collections.Generic;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public interface INEFileData
	{
		int NESerial { get; }
		NEFile NEFile { get; }

		NETextPoint NETextPoint { get; }
		IReadOnlyList<NERange> Selections { get; }
		IReadOnlyList<NERange>[] Regions { get; }
		bool AllowOverlappingSelections { get; }

		INEFileData Undo { get; }
		INEFileData Redo { get; }
		INEFileData RedoText { get; }

		INEFileData Next();
	}
}
