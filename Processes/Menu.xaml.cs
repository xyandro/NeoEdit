using NeoEdit.GUI.Common;

namespace NeoEdit.Processes
{
	class ProcessesMenuItem : NEMenuItem<ProcessesCommand> { }

	partial class ProcessesMenu
	{
		[DepProp]
		public new ProcessesWindow Parent { get { return UIHelper<ProcessesMenu>.GetPropValue(() => this.Parent); } set { UIHelper<ProcessesMenu>.SetPropValue(() => this.Parent, value); } }

		static ProcessesMenu() { UIHelper<ProcessesMenu>.Register(); }

		public ProcessesMenu()
		{
			InitializeComponent();
		}
	}
}
