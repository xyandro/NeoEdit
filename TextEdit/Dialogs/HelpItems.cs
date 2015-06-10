using System.Collections.Generic;

namespace NeoEdit.TextEdit.Dialogs
{
	class HelpItems
	{
		public string Name { get; set; }
		public List<HelpItem> Items { get; set; }
		public int Columns { get; set; }
		public int NameWidth { get; set; }
		public int DescWidth { get; set; }
		public HelpItems() { Items = new List<HelpItem>(); }
	}

	class HelpItem
	{
		public string Name { get; set; }
		public string Description { get; set; }
	}
}
