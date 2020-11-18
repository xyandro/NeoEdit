using System.Collections.Generic;

namespace NeoEdit.Editor
{
	public class NEFilesResult
	{
		public List<NEFile> NewFiles { get; private set; }

		public void AddNewFile(NEFile neFile)
		{
			if (NewFiles == null)
				NewFiles = new List<NEFile>();
			NewFiles.Add(neFile);
		}
	}
}
