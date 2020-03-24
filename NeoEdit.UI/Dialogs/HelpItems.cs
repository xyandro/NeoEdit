using System.Collections.Generic;

namespace NeoEdit.UI.Dialogs
{
	public class HelpItems
	{
		public string Name { get; set; }
		public List<HelpItem> Items { get; } = new List<HelpItem>();
		public int Columns { get; set; }
		public int NameWidth { get; set; }
		public int DescriptionWidth { get; set; }
		public int ExampleWidth { get; set; }
	}

	public class HelpItem
	{
		public string Name { get; set; }
		public string Description { get; set; }
		public string Example { get; set; }
	}
}
