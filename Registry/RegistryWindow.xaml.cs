﻿using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NeoEdit.GUI.Common;
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
			uiHelper.AddCallback(ItemGridTree.LocationProperty, keys, () => Location = keys.Location.FullName);
			location.GotFocus += (s, e) => location.SelectAll();
			location.LostFocus += (s, e) => uiHelper.InvalidateBinding(location, TextBox.TextProperty);

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
			if ((location.Length >= 2) && (location.StartsWith("\"")) && (location.EndsWith("\"")))
				location = location.Substring(1, location.Length - 2);
			location = Regex.Replace(location.Trim(), @"[\\/]+", @"\");
			var regItem = new RegistryItem().GetChild(location);
			if (regItem == null)
			{
				MessageBox.Show("Invalid key.", "Error");
				return false;
			}

			keys.Location = regItem;
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
