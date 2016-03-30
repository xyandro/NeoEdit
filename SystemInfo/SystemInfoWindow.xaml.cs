using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Microsoft.Win32;
using NeoEdit.GUI.Controls;

namespace NeoEdit.SystemInfo
{
	partial class SystemInfoWindow
	{
		public static RoutedCommand Command_File_Save = new RoutedCommand();
		public static RoutedCommand Command_View_InstalledPrograms = new RoutedCommand();

		[DepProp]
		string Text { get { return UIHelper<SystemInfoWindow>.GetPropValue<string>(this); } set { UIHelper<SystemInfoWindow>.SetPropValue(this, value); } }

		static SystemInfoWindow() { UIHelper<SystemInfoWindow>.Register(); }

		public SystemInfoWindow()
		{
			InitializeComponent();

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
			public string Name { get; }
			public string BitDepth { get; }
			public string Version { get; }
			public string Publisher { get; }

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

			public bool HasData() => (!string.IsNullOrWhiteSpace(Name)) || (!string.IsNullOrWhiteSpace(Version)) || (!string.IsNullOrWhiteSpace(Publisher));

			public override string ToString()
			{
				var result = Name;
				if (!string.IsNullOrWhiteSpace(BitDepth))
					result += $" {BitDepth}";
				if (!string.IsNullOrWhiteSpace(Version))
					result += $" (Version: {Version})";
				if (!string.IsNullOrWhiteSpace(Publisher))
					result += $" ({Publisher})";
				return result;
			}
		}

		void ListInstalledPrograms()
		{
			var programs = new List<InstalledProgram>();
			var keys = new Dictionary<string, string>
			{
				[@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"] = "64-bit",
				[@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall"] = "32-bit",
			};
			foreach (var bitDepth in keys)
				using (var key = Registry.LocalMachine.OpenSubKey(bitDepth.Key))
					foreach (var subKeyName in key.GetSubKeyNames())
						using (var subkey = key.OpenSubKey(subKeyName))
							programs.Add(new InstalledProgram(subkey, bitDepth.Value));
			Text = string.Join("", programs.Where(prog => prog.HasData()).OrderBy(prog => prog.Name).Select(prog => $"{prog}\r\n"));
		}
	}
}
