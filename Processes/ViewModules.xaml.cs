using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using NeoEdit.Common;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.ItemGridControl;
using NeoEdit.Processes.Dialogs;

namespace NeoEdit.Processes
{
	class ModuleItemGrid : ItemGrid<ModuleItem> { }

	class ModuleItem : DependencyObject
	{
		[DepProp]
		public string Name { get { return UIHelper<ModuleItem>.GetPropValue(() => this.Name); } set { UIHelper<ModuleItem>.SetPropValue(() => this.Name, value); } }
		[DepProp]
		public string FileName { get { return UIHelper<ModuleItem>.GetPropValue(() => this.FileName); } set { UIHelper<ModuleItem>.SetPropValue(() => this.FileName, value); } }
		[DepProp]
		public long StartAddress { get { return UIHelper<ModuleItem>.GetPropValue(() => this.StartAddress); } set { UIHelper<ModuleItem>.SetPropValue(() => this.StartAddress, value); } }
		[DepProp]
		public long EndAddress { get { return UIHelper<ModuleItem>.GetPropValue(() => this.EndAddress); } set { UIHelper<ModuleItem>.SetPropValue(() => this.EndAddress, value); } }
		[DepProp]
		public long Size { get { return UIHelper<ModuleItem>.GetPropValue(() => this.Size); } set { UIHelper<ModuleItem>.SetPropValue(() => this.Size, value); } }

		static ModuleItem() { UIHelper<ModuleItem>.Register(); }
	}

	partial class ViewModules
	{
		[DepProp]
		public string ProcessName { get { return UIHelper<ViewModules>.GetPropValue(() => this.ProcessName); } set { UIHelper<ViewModules>.SetPropValue(() => this.ProcessName, value); } }
		[DepProp]
		public int PID { get { return UIHelper<ViewModules>.GetPropValue(() => this.PID); } set { UIHelper<ViewModules>.SetPropValue(() => this.PID, value); } }
		[DepProp]
		ObservableCollection<ModuleItem> Modules { get { return UIHelper<ViewModules>.GetPropValue(() => this.Modules); } set { UIHelper<ViewModules>.SetPropValue(() => this.Modules, value); } }
		[DepProp]
		public FontFamily ListFont { get { return UIHelper<ViewModules>.GetPropValue(() => this.ListFont); } set { UIHelper<ViewModules>.SetPropValue(() => this.ListFont, value); } }

		static ViewModules() { UIHelper<ViewModules>.Register(); }

		ViewModules(int pid)
		{
			InitializeComponent();

			ListFont = Font.FontFamily;

			PID = pid;
			var process = Process.GetProcessById(PID);
			ProcessName = process.ProcessName;

			foreach (var prop in UIHelper<ModuleItem>.GetProperties())
			{
				var col = new ItemGridColumn(prop) { SortAscending = true };
				if (col.Header.EndsWith("Address"))
					col.StringFormat = (StringFormatDelegate)(a => ((long)a).ToSpacedHex());
				modules.Columns.Add(col);
			}
			modules.SortColumn = modules.Columns.First(col => col.Header == "StartAddress");

			Modules = new ObservableCollection<ModuleItem>();
			foreach (ProcessModule module in process.Modules)
			{
				Modules.Add(new ModuleItem
				{
					Name = module.ModuleName,
					FileName = module.FileName,
					StartAddress = module.BaseAddress.ToInt64(),
					EndAddress = module.BaseAddress.ToInt64() + module.ModuleMemorySize,
					Size = module.ModuleMemorySize,
				});
			}
		}

		static public void Run(int pid)
		{
			new ViewModules(pid).ShowDialog();
		}

		private void GotoCommandExecuted(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
		{
			var result = GotoDialog.Run(UIHelper.FindParent<Window>(this));
			if (!result.HasValue)
				return;

			var find = result.Value;
			modules.Selected.Clear();
			foreach (var module in Modules)
				if ((find >= module.StartAddress) && (find < module.EndAddress))
					modules.Selected.Add(module);
		}
	}
}
