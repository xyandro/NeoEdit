namespace NeoEdit.Common
{
	public class WindowLayout
	{
		public int? Columns { get; }
		public int? Rows { get; }
		public int? MaxColumns { get; }
		public int? MaxRows { get; }
		public bool ActiveOnly { get; }

		public WindowLayout(int? columns = null, int? rows = null, int? maxColumns = null, int? maxRows = null, bool onlyActive = false)
		{
			Columns = columns;
			Rows = rows;
			MaxColumns = maxColumns;
			MaxRows = maxRows;
			ActiveOnly = onlyActive;
		}
	}
}
