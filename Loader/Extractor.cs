using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace Loader
{
	class Extractor
	{
		static void DeleteDelayed(string path)
		{
			string command;
			if (File.Exists(path))
				command = $"DEL \"{path}\"";
			else if (Directory.Exists(path))
				command = $"RD /S /Q \"{path}\"";
			else
				return;

			Process.Start(new ProcessStartInfo()
			{
				FileName = "cmd.exe",
				Arguments = $"/C choice /C Y /N /D Y /T 1 & {command}",
				WindowStyle = ProcessWindowStyle.Hidden,
				CreateNoWindow = true,
			});
		}

		public void Extract(BitDepths bitDepth)
		{
			var location = typeof(Program).Assembly.Location;
			var newLocation = Path.Combine(Path.GetDirectoryName(location), $"{Path.GetFileNameWithoutExtension(location)}.Extractor{Path.GetExtension(location)}");
			File.Copy(location, newLocation, true);
			Process.Start(newLocation, $"-extractor {bitDepth} {Process.GetCurrentProcess().Id} \"{location}\"");
		}

		static void WaitForParentExit(int pid) { try { Process.GetProcessById(pid).WaitForExit(); } catch { } }

		void RunNGen()
		{
			if (!ResourceReader.Config.NGen)
				return;

			if (MessageBox.Show("Run ngen.exe to improve load performance?", "Confirm", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Yes) != MessageBoxResult.Yes)
				return;

			var locations = new List<string> { @"C:\Windows\Microsoft.NET" };
			for (var ctr = 0; ctr < locations.Count; ++ctr)
				locations.AddRange(Directory.GetDirectories(locations[ctr]));

			var ngen = locations.SelectMany(location => Directory.EnumerateFiles(location)).Where(fileName => Path.GetFileName(fileName).Equals("ngen.exe", StringComparison.OrdinalIgnoreCase)).OrderByDescending(fileName => fileName).ToList().FirstOrDefault();
			if (ngen == null)
			{
				MessageBox.Show("Failed to find ngen.exe", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			Process.Start(new ProcessStartInfo()
			{
				FileName = ngen,
				Arguments = $"install \"{(Environment.Is64BitProcess ? ResourceReader.Config.X64Start : ResourceReader.Config.X32Start)}\"",
				Verb = "runas",
			});
		}

		public void RunExtractor(BitDepths bitDepth, int? parentPid, string parentPath)
		{
			if (parentPid.HasValue)
				WaitForParentExit(parentPid.Value);
			if (!string.IsNullOrEmpty(parentPath))
				File.Delete(parentPath);

			var exeFile = typeof(Program).Assembly.Location;
			var location = Path.GetDirectoryName(exeFile);
			foreach (var resource in ResourceReader.GetResources(bitDepth))
				resource.WriteToPath(location);
			foreach (var resource in ResourceReader.GetResources(bitDepth))
				resource.SetDate(location);

			RunNGen();

			DeleteDelayed(exeFile);
		}

		public void RunUpdate(int pid, string dest)
		{
			WaitForParentExit(pid);
			var src = typeof(Program).Assembly.Location;
			File.Copy(src, dest, true);
			DeleteDelayed(src);
		}

		Dictionary<string, Assembly> resolved = new Dictionary<string, Assembly>();
		Assembly AssemblyResolve(ResolveEventArgs args, string dllPath)
		{
			var name = new AssemblyName(args.Name).Name;
			if (resolved.ContainsKey(name))
				return resolved[name];

			var resource = ResourceReader.Resources.Where(res => res.NameMatch(name)).SingleOrDefault();
			if (resource == null)
				return resolved[name] = null;
			if (resource.FileType == FileTypes.Managed)
				return resolved[name] = Assembly.Load(resource.UncompressedData);
			if (resource.FileType == FileTypes.Mixed)
			{
				resource.WriteToPath(dllPath);
				return resolved[name] = Assembly.LoadFile(Path.Combine(dllPath, resource.Name));
			}
			return resolved[name] = null;
		}

		public void RunProgram(string[] args)
		{
			var start = Environment.Is64BitProcess ? ResourceReader.Config.X64Start : ResourceReader.Config.X32Start;
			var dllPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), start, "DLLs");

			var self = typeof(Extractor).Assembly.Location;
			var dir = Path.GetDirectoryName(self);
			var existing = ResourceReader.Resources.Select(resource => Path.Combine(dir, resource.Name)).Where(file => (file != self) && (File.Exists(file))).ToList();
			if (existing.Any())
			{
				var result = MessageBox.Show($"{existing.Count} files from a previous version exist which may make this program work incorrectly.  Delete them?", "Warning", MessageBoxButton.YesNoCancel);
				switch (result)
				{
					case MessageBoxResult.Yes: existing.ForEach(file => File.Delete(file)); break;
					case MessageBoxResult.Cancel: return;
				}
			}

			var entry = ResourceReader.Resources.Where(resource => resource.Name.Equals(start, StringComparison.OrdinalIgnoreCase)).Single();
			if (entry.FileType != FileTypes.Managed)
			{
				Directory.CreateDirectory(dllPath);
				foreach (var resource in ResourceReader.Resources)
					resource.WriteToPath(dllPath);
				var proc = Process.Start(Path.Combine(dllPath, entry.Name), Environment.CommandLine);
				proc.WaitForExit();
				return;
			}

			var found = false;
			foreach (var resource in ResourceReader.Resources.Where(resource => resource.FileType == FileTypes.Native))
			{
				if (!found)
				{
					Directory.CreateDirectory(dllPath);
					Native.SetDllDirectory(dllPath);
					found = true;
				}

				resource.WriteToPath(dllPath);
			}

			AppDomain.CurrentDomain.AssemblyResolve += (s, info) => AssemblyResolve(info, dllPath);

			var startTime = DateTime.Now;
			try { AppDomain.CurrentDomain.ExecuteAssemblyByName(start, args); }
			catch
			{
				if ((DateTime.Now - startTime).TotalSeconds < 10)
					throw;
			}
			finally { DeleteDelayed(dllPath); }
		}
	}
}
