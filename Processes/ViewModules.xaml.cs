using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using NeoEdit.Common;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Controls.ItemGridControl;
using NeoEdit.GUI.Misc;
using NeoEdit.Processes.Dialogs;

namespace NeoEdit.Processes
{
	class ModuleItemGrid : ItemGrid<ModuleItem> { }

	class ModuleItem : DependencyObject
	{
		[DepProp]
		public string Name { get { return UIHelper<ModuleItem>.GetPropValue<string>(this); } set { UIHelper<ModuleItem>.SetPropValue(this, value); } }
		[DepProp]
		public string FileName { get { return UIHelper<ModuleItem>.GetPropValue<string>(this); } set { UIHelper<ModuleItem>.SetPropValue(this, value); } }
		[DepProp]
		public long StartAddress { get { return UIHelper<ModuleItem>.GetPropValue<long>(this); } set { UIHelper<ModuleItem>.SetPropValue(this, value); } }
		[DepProp]
		public long EndAddress { get { return UIHelper<ModuleItem>.GetPropValue<long>(this); } set { UIHelper<ModuleItem>.SetPropValue(this, value); } }
		[DepProp]
		public long Size { get { return UIHelper<ModuleItem>.GetPropValue<long>(this); } set { UIHelper<ModuleItem>.SetPropValue(this, value); } }

		static ModuleItem() { UIHelper<ModuleItem>.Register(); }
	}

	partial class ViewModules
	{
		[DepProp]
		public string ProcessName { get { return UIHelper<ViewModules>.GetPropValue<string>(this); } set { UIHelper<ViewModules>.SetPropValue(this, value); } }
		[DepProp]
		public int PID { get { return UIHelper<ViewModules>.GetPropValue<int>(this); } set { UIHelper<ViewModules>.SetPropValue(this, value); } }
		[DepProp]
		ObservableCollection<ModuleItem> Modules { get { return UIHelper<ViewModules>.GetPropValue<ObservableCollection<ModuleItem>>(this); } set { UIHelper<ViewModules>.SetPropValue(this, value); } }
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

		static public void Run(int pid) => new ViewModules(pid).ShowDialog();

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
