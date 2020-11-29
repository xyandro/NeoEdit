using System.Collections.Generic;
using NeoEdit.Common.Enums;

namespace NeoEdit.Editor.PreExecution
{
	public class PreExecution_File_New_FromSelections_AllFilesSelections : IPreExecution
	{
		public Dictionary<NEFile, (IReadOnlyList<string> selections, string name, ParserType contentType)> Selections { get; set; }
	}
}
