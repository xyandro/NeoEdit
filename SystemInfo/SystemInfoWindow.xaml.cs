using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using NeoEdit.GUI.Common;

namespace NeoEdit.SystemInfo
{
	public partial class SystemInfoWindow : Window
	{
		public static RoutedCommand Command_File_Save = new RoutedCommand();
		public static RoutedCommand Command_View_InstalledPrograms = new RoutedCommand();

		[DepProp]
		string Text { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }

		static SystemInfoWindow() { UIHelper<SystemInfoWindow>.Register(); }

		readonly UIHelper<SystemInfoWindow> uiHelper;
		public SystemInfoWindow()
		{
			uiHelper = new UIHelper<SystemInfoWindow>(this);
			InitializeComponent();
			Transparency.MakeTransparent(this);

			Text = "Please select an item from the menu.";
		}

		void Command_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (e.Command == Command_File_Save)
			{
				var dialog = new SaveFileDialog { Filter = "Text files|*.txt" };
				if (dialog.ShowDialog() == true)
					File.WriteAllText(dialog.FileName, Text);
			}
			else if (e.Command == Command_View_InstalledPrograms)
				ListInstalledPrograms();
		}

		class InstalledProgram
		{
			public string Name { get; set; }
			public string BitDepth { get; set; }
			public string Version { get; set; }
			public string Publisher { get; set; }

			public InstalledProgram(RegistryKey key, string bitDepth)
			{
				Name = GetValue(key, "DisplayName");
				BitDepth = bitDepth;
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
				if (!String.IsNullOrWhiteSpace(BitDepth))
					result += String.Format(" {0}", BitDepth);
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
			var keys = new Dictionary<string, string>
			{
				{ @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", "64-bit" },
				{ @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall", "32-bit" },
			};
			foreach (var bitDepth in keys)
				using (var key = Registry.LocalMachine.OpenSubKey(bitDepth.Key))
					foreach (var subKeyName in key.GetSubKeyNames())
						using (var subkey = key.OpenSubKey(subKeyName))
							programs.Add(new InstalledProgram(subkey, bitDepth.Value));
			Text = String.Join("", programs.Where(prog => prog.HasData()).OrderBy(prog => prog.Name).Select(prog => prog.ToString() + "\r\n"));
		}
	}
}
