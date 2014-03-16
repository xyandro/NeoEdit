using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NeoEdit.Common;
using NeoEdit.GUI;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.Dialogs;
using System.Diagnostics;
using NeoEdit.GUI.ItemGridControl;

namespace NeoEdit.Registry
{
	public partial class RegistryWindow : Window
	{
		public static RoutedCommand Command_View_Refresh = new RoutedCommand();

		[DepProp]
		string Location { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		ObservableCollection<RegistryItem> Keys { get { return uiHelper.GetPropValue<ObservableCollection<RegistryItem>>(); } set { uiHelper.SetPropValue(value); } }

		static RegistryWindow() { UIHelper<RegistryWindow>.Register(); }

		readonly UIHelper<RegistryWindow> uiHelper;
		public RegistryWindow(string key)
		{
			uiHelper = new UIHelper<RegistryWindow>(this);
			InitializeComponent();
			uiHelper.AddCallback(a => a.Location, (o, n) => Refresh());
			location.GotFocus += (s, e) => location.SelectAll();
			location.LostFocus += (s, e) => uiHelper.InvalidateBinding(location, TextBox.TextProperty);
			keys.Accept += (s, e) =>
			{
				if (keys.Selected.Count != 1)
					return;

				var item = keys.Selected[0] as RegistryItem;
				if (item.IsKey)
					SetLocation(item.FullName);
			};

			foreach (var prop in RegistryItem.GetDepProps())
			{
				if (prop.Name == "FullName")
					continue;
				keys.Columns.Add(new ItemGridColumn(prop));
			}
			keys.SortColumn = keys.TextInputColumn = keys.Columns.First(col => col.Header == "Name");
			Keys = new ObservableCollection<RegistryItem>();
			SetLocation(key ?? "");
		}

		bool altOnly { get { return (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt)) == ModifierKeys.Alt; } }
		bool noModifiers { get { return (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)) == ModifierKeys.None; } }

		bool SetLocation(string location)
		{
			location = RegistryItem.GetProperKey(location);
			if (location == null)
			{
				MessageBox.Show("Invalid key.", "Error");
				return false;
			}

			var oldLocation = Location;
			Location = location;
			keys.ResetScroll();
			keys.Focused = keys.Items.FirstOrDefault(item => (item as RegistryItem).FullName == oldLocation);
			keys.Selected.Add(keys.Focused);
			return true;
		}

		void Refresh()
		{
			keys.SyncItems(RegistryItem.GetKeys(Location), RegistryItem.StaticGetDepProp("FullName"));
		}

		void Command_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (e.Command == Command_View_Refresh)
				Refresh();
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);

			var keySet = new KeySet
			{
				{ ModifierKeys.Alt, Key.Up, () => SetLocation(RegistryItem.GetParent(Location)) },
				{ Key.Escape, () => keys.Focus() },
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
						keys.Focus();
					break;
				default: e.Handled = false; break;
			}
		}
	}
}
