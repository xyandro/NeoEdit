using System.Collections.Generic;

namespace NeoEdit.UI.BinaryEditorUI
{
	public class FindResult
	{
		public string FindText;
		public List<byte[]> FindData;
		public List<bool> CaseSensitive;
	}
}
