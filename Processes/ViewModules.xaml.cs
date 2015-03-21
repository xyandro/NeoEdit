using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;
using NeoEdit.GUI.Common;

namespace NeoEdit.Processes
{
	partial class ViewModules
	{
		[DepProp]
		public string ProcessName { get { return UIHelper<ViewModules>.GetPropValue<string>(this); } set { UIHelper<ViewModules>.SetPropValue(this, value); } }
		[DepProp]
		public int PID { get { return UIHelper<ViewModules>.GetPropValue<int>(this); } set { UIHelper<ViewModules>.SetPropValue(this, value); } }
		[DepProp]
		public List<Dictionary<string, string>> Modules { get { return UIHelper<ViewModules>.GetPropValue<List<Dictionary<string, string>>>(this); } set { UIHelper<ViewModules>.SetPropValue(this, value); } }
		[DepProp]
		public FontFamily ListFont { get { return UIHelper<ViewModules>.GetPropValue<FontFamily>(this); } set { UIHelper<ViewModules>.SetPropValue(this, value); } }

		static ViewModules() { UIHelper<ViewModules>.Register(); }

		ViewModules(int pid)
		{
			InitializeComponent();

			ListFont = Font.FontFamily;

			PID = pid;
			var process = Process.GetProcessById(PID);
			ProcessName = process.ProcessName;

			Modules = new List<Dictionary<string, string>>();
			foreach (ProcessModule module in process.Modules)
			{
				var modDict = new Dictionary<string, string>();
				modDict["Name"] = module.ModuleName;
				modDict["FileName"] = module.FileName;
				modDict["StartAddress"] = module.BaseAddress.ToString("x16");
				modDict["EndAddress"] = (module.BaseAddress + module.ModuleMemorySize).ToString("x16");

				var size = (double)module.ModuleMemorySize;
				var inc = 0;
				while (size >= 1000)
				{
					size /= 1024;
					++inc;
				}

				var sizes = new List<string> { "", "KB", "MB", "GB", "TB" };
				var sizeStr = size.ToString("0.00").PadLeft(6) + " " + sizes[inc];

				modDict["Size"] = sizeStr;

				Modules.Add(modDict);
			}

			Modules = Modules.OrderBy(entry => entry["StartAddress"]).ToList();
		}

		static public void Run(int pid)
		{
			new ViewModules(pid).ShowDialog();
		}
	}
}
