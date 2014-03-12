using System;
using System.Linq;

namespace NeoEdit
{
	class InstanceManager : Microsoft.VisualBasic.ApplicationServices.WindowsFormsApplicationBase
	{
		[STAThread]
		static void Main(string[] args) { new InstanceManager().Run(args); }

		App app;

		InstanceManager() { IsSingleInstance = true; }
		protected override bool OnStartup(Microsoft.VisualBasic.ApplicationServices.StartupEventArgs e)
		{
			app = new App();
			app.Run();
			return false;
		}

		protected override void OnStartupNextInstance(Microsoft.VisualBasic.ApplicationServices.StartupNextInstanceEventArgs e)
		{
			base.OnStartupNextInstance(e);
			var window = app.GetWindowFromArgs(e.CommandLine.ToArray());
			if (window != null)
			{
				window.Activate();
				window.Topmost = true;
				window.Topmost = false;
				window.Focus();
			}
		}
	}
}
