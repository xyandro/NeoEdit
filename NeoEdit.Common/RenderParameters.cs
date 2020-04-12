using System.Collections.Generic;

namespace NeoEdit.Common
{
	public class RenderParameters
	{
		public IReadOnlyList<ITab> AllTabs { get; }
		public IReadOnlyList<ITab> ActiveTabs { get; }
		public ITab FocusedTab { get; }
		public int Count { get; }
		public WindowLayout WindowLayout { get; }

		public RenderParameters(IReadOnlyList<ITab> allTabs, IReadOnlyList<ITab> activeTabs, ITab focusedTab, int count, WindowLayout windowLayout)
		{
			AllTabs = allTabs;
			ActiveTabs = activeTabs;
			FocusedTab = focusedTab;
			Count = count;
			WindowLayout = windowLayout;
		}
	}
}
