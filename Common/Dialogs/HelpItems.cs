using System.Collections.Generic;

namespace NeoEdit.Common.Dialogs
{
	public class HelpItems
	{
		public string Name { get; set; }
		public List<HelpItem> Items { get; } = new List<HelpItem>();
		public int Columns { get; set; }
		public int NameWidth { get; set; }
		public int DescWidth { get; set; }
	}

	public class HelpItem
	{
		public string Name { get; set; }
		public string Description { get; set; }
	}
}
