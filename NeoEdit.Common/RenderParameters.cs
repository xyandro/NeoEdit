using System.Collections.Generic;

namespace NeoEdit.Common
{
	public class RenderParameters
	{
		public IReadOnlyList<ITab> AllTabs { get; set; }
		public IReadOnlyList<ITab> ActiveTabs { get; set; }
		public ITab FocusedTab { get; set; }
		public int Count { get; set; }
		public WindowLayout WindowLayout { get; set; }
		public List<string> StatusBar { get; set; }
		public Dictionary<string, bool?> MenuStatus { get; set; }
	}
}
