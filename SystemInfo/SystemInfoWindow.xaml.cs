using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using NeoEdit.GUI.Common;

namespace NeoEdit.SystemInfo
{
	public partial class SystemInfoWindow : Window
	{
		public static RoutedCommand Command_View_InstalledPrograms = new RoutedCommand();

		[DepProp]
		string Text { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }

		static SystemInfoWindow() { UIHelper<SystemInfoWindow>.Register(); }

		readonly UIHelper<SystemInfoWindow> uiHelper;
		public SystemInfoWindow()
		{
			uiHelper = new UIHelper<SystemInfoWindow>(this);
			InitializeComponent();

			Text = "Please select an item from the menu.";
		}

		void Command_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (e.Command == Command_View_InstalledPrograms)
				ListInstalledPrograms();
		}

		class InstalledProgram
		{
			public string Name { get; set; }
			public string Version { get; set; }
			public string Publisher { get; set; }

			public InstalledProgram(RegistryKey key)
			{
				Name = GetValue(key, "DisplayName");
				Version = GetValue(key, "Version");
				Publisher = GetValue(key, "Publisher");
			}

			string GetValue(RegistryKey key, string name)
			{
				var value = key.GetValue(name);
				if (value == null)
					return "";
				return value.ToString().Trim();
			}

			public bool HasData()
			{
				return (!String.IsNullOrWhiteSpace(Name)) || (!String.IsNullOrWhiteSpace(Version)) || (!String.IsNullOrWhiteSpace(Publisher));
			}

			public override string ToString()
			{
				var result = Name;
				if (!String.IsNullOrWhiteSpace(Version))
					result += String.Format(" (Version: {0})", Version);
				if (!String.IsNullOrWhiteSpace(Publisher))
					result += String.Format(" ({0})", Publisher);
				return result;
			}
		}

		void ListInstalledPrograms()
		{
			var programs = new List<InstalledProgram>();
			using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
				foreach (var subKeyName in key.GetSubKeyNames())
					using (var subkey = key.OpenSubKey(subKeyName))
						programs.Add(new InstalledProgram(subkey));
			Text = String.Join("", programs.Where(prog => prog.HasData()).OrderBy(prog => prog.Name).Select(prog => prog.ToString() + "\r\n"));
		}
	}
}
