using System.Collections.Generic;

namespace NeoEdit.GUI.Dialogs
{
	public class HelpItems
	{
		public string Name { get; set; }
		public List<HelpItem> Items { get; set; }
		public int Columns { get; set; }
		public int NameWidth { get; set; }
		public int DescWidth { get; set; }
		public HelpItems() { Items = new List<HelpItem>(); }
	}

	public class HelpItem
	{
		public string Name { get; set; }
		public string Description { get; set; }
	}
}
