using System.Collections.Generic;

namespace NeoEdit.Editor
{
	public class NEFilesHandlerResult
	{
		public List<NEFileHandler> NewFiles { get; private set; }

		public void AddNewFile(NEFileHandler neFile)
		{
			if (NewFiles == null)
				NewFiles = new List<NEFileHandler>();
			NewFiles.Add(neFile);
		}
	}
}
