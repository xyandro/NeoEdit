using System.Collections.Generic;

namespace NeoEdit.Common
{
	public class RenderParameters
	{
		public IReadOnlyList<INEFile> NEFiles { get; set; }
		public IReadOnlyList<INEFile> ActiveFiles { get; set; }
		public INEFile FocusedFile { get; set; }
		public WindowLayout WindowLayout { get; set; }
		public bool WorkMode { get; set; }
		public List<string> StatusBar { get; set; }
		public Dictionary<string, bool?> MenuStatus { get; set; }
	}
}
