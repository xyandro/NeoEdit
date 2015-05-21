﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Controls.ItemGridControl;
using NeoEdit.GUI.Misc;

namespace NeoEdit.Registry
{
	public partial class RegistryWindow : Window
	{
		public static RoutedCommand Command_View_Refresh = new RoutedCommand();

		[DepProp]
		string Location { get { return UIHelper<RegistryWindow>.GetPropValue<string>(this); } set { UIHelper<RegistryWindow>.SetPropValue(this, value); } }
		[DepProp]
		ObservableCollection<RegistryItem> Keys { get { return UIHelper<RegistryWindow>.GetPropValue<ObservableCollection<RegistryItem>>(this); } set { UIHelper<RegistryWindow>.SetPropValue(this, value); } }

		static RegistryWindow()
		{
			UIHelper<RegistryWindow>.Register();
			UIHelper<RegistryWindow>.AddCallback(a => a.keys, ItemGridTree.LocationProperty, obj => obj.Location = obj.keys.Location.FullName);
		}

		List<PropertyChangeNotifier> localCallbacks;
		public RegistryWindow(string key)
		{
			InitializeComponent();

			localCallbacks = UIHelper<RegistryWindow>.GetLocalCallbacks(this);
			location.GotFocus += (s, e) => location.SelectAll();
			location.LostFocus += (s, e) => location.InvalidateBinding(TextBox.TextProperty);

			foreach (var prop in UIHelper<RegistryItem>.GetProperties())
			{
				if (prop.Name == "FullName")
					continue;
				keys.Columns.Add(new ItemGridColumn(prop));
			}
			keys.SortColumn = keys.Columns.First(col => col.Header == "Name");
			Keys = new ObservableCollection<RegistryItem>();
			SetLocation(key ?? "");
		}

		bool altOnly { get { return (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt)) == ModifierKeys.Alt; } }
		bool noModifiers { get { return (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)) == ModifierKeys.None; } }

		bool SetLocation(string location)
		{
			location = RegistryItem.Simplify(location);
			var item = new RegistryItem().GetChild(location);
			if (item == null)
			{
				MessageBox.Show("Invalid key.", "Error");
				return false;
			}

			keys.Location = item;
			return true;
		}

		void Command_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (e.Command == Command_View_Refresh)
				keys.Refresh();
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);

			var keySet = new KeySet
			{
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
