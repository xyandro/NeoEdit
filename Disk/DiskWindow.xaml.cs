using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.ItemGridControl;

namespace NeoEdit.Disk
{
	public partial class DiskWindow : Window
	{
		public static RoutedCommand Command_View_Refresh = new RoutedCommand();

		[DepProp]
		string Location { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		ObservableCollection<DiskItem> Files { get { return uiHelper.GetPropValue<ObservableCollection<DiskItem>>(); } set { uiHelper.SetPropValue(value); } }

		static DiskWindow() { UIHelper<DiskWindow>.Register(); }

		readonly UIHelper<DiskWindow> uiHelper;
		public DiskWindow(string path = null)
		{
			if (path == null)
				path = Directory.GetCurrentDirectory();

			uiHelper = new UIHelper<DiskWindow>(this);
			InitializeComponent();
			uiHelper.AddCallback(ItemGridTree.LocationProperty, files, () => Location = files.Location.FullName);
			location.GotFocus += (s, e) => location.SelectAll();
			location.LostFocus += (s, e) => uiHelper.InvalidateBinding(location, TextBox.TextProperty);

			var keep = new List<string> { "Name", "Size", "WriteTime" };
			foreach (var prop in DiskItem.GetDepProps())
			{
				if (!keep.Contains(prop.Name))
					continue;
				files.Columns.Add(new ItemGridColumn(prop));
			}
			files.SortColumn = files.TextInputColumn = files.Columns.First(col => col.Header == "Name");
			Files = new ObservableCollection<DiskItem>();
			SetLocation(path ?? "");
		}

		bool altOnly { get { return (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt)) == ModifierKeys.Alt; } }
		bool noModifiers { get { return (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)) == ModifierKeys.None; } }

		bool SetLocation(string location)
		{
			var item = DiskItem.GetRoot().GetChild(DiskItem.Simplify(location));
			if (item == null)
			{
				MessageBox.Show("Invalid path.", "Error");
				return false;
			}

			files.Location = item;
			return true;
		}

		void Command_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (e.Command == Command_View_Refresh)
				files.Refresh();
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);

			var keySet = new KeySet
			{
				{ Key.Escape, () => files.Focus() },
			};

			if (keySet.Run(e))
				e.Handled = true;
		}

		void location_KeyDown(object sender, KeyEventArgs e)
		{
			e.Handled = true;
			switch (e.Key)
			{
				case Key.Enter:
					if (SetLocation(location.Text))
						files.Focus();
					break;
				default: e.Handled = false; break;
			}
		}
	}
}
