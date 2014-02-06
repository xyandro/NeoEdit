using System.Collections.Generic;
using NeoEdit.Common;

namespace NeoEdit.UI.BinaryEditorUI
{
	public class FindResult
	{
		public string FindText;
		public List<BinaryData> FindData;
		public List<bool> CaseSensitive;
	}
}
