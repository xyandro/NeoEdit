using System.Collections.Generic;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public interface INEFileData
	{
		long NESerial { get; }
		NEFile NEFile { get; }

		NETextPoint NETextPoint { get; }
		IReadOnlyList<NERange> Selections { get; }
		IReadOnlyList<NERange>[] Regions { get; }
		bool AllowOverlappingSelections { get; }

		INEFileData Next();
	}
}
