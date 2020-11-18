using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public class NEAllFilesData
	{
		public IReadOnlyOrderedHashSet<NEFiles> allNEFiles;

		public NEAllFilesData Clone() => new NEAllFilesData { allNEFiles = allNEFiles };
	}
}
