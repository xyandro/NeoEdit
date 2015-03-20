using NeoEdit.GUI.Common;

namespace NeoEdit.Processes
{
	class ProcessesMenuItem : NEMenuItem<ProcessesCommand> { }

	partial class ProcessesMenu
	{
		[DepProp]
		public new ProcessesWindow Parent { get { return UIHelper<ProcessesMenu>.GetPropValue<ProcessesWindow>(this); } set { UIHelper<ProcessesMenu>.SetPropValue(this, value); } }

		static ProcessesMenu() { UIHelper<ProcessesMenu>.Register(); }

		public ProcessesMenu()
		{
			InitializeComponent();
		}
	}
}
