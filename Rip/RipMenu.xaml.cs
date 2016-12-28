using NeoEdit.GUI.Controls;

namespace NeoEdit.Rip
{
	class RipMenuItem : NEMenuItem<RipCommand> { }

	partial class RipMenu
	{
		static RipMenu() { UIHelper<RipMenu>.Register(); }

		public RipMenu() { InitializeComponent(); }
	}
}
