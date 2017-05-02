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

			var resourceHeaders = ResourceReader.GetResourceHeaders(bitDepth).ToList();
			foreach (var resourceHeader in resourceHeaders)
				Resource.CreateFromHeader(resourceHeader).WriteToPath(location);
			foreach (var resourceHeader in resourceHeaders)
				resourceHeader.SetDate(location);

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

			var resourceHeader = ResourceReader.ResourceHeaders.SingleOrDefault(res => res.NameMatch(name));
			if (resourceHeader == null)
				return resolved[name] = null;
			var resource = Resource.CreateFromHeader(resourceHeader);
			if (resourceHeader.FileType == FileTypes.Managed)
				return resolved[name] = Assembly.Load(resource.RawData);
			if (resourceHeader.FileType == FileTypes.Mixed)
			{
				resource.WriteToPath(dllPath);
				return resolved[name] = Assembly.LoadFile(Path.Combine(dllPath, resourceHeader.Name));
			}
			return resolved[name] = null;
		}

		public void RunProgram(string[] args)
		{
			var start = Environment.Is64BitProcess ? ResourceReader.Config.X64Start : ResourceReader.Config.X32Start;
			var dllPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), start, "DLLs");

			var entry = ResourceReader.ResourceHeaders.Single(resourceHeader => resourceHeader.Name.Equals(start, StringComparison.OrdinalIgnoreCase));

			var found = false;
			foreach (var resourceHeader in ResourceReader.ResourceHeaders)
			{
				if ((entry.FileType == FileTypes.Managed) && (resourceHeader.FileType != FileTypes.Native))
					continue;

				if (!found)
				{
					Directory.CreateDirectory(dllPath);
					Native.SetDllDirectory(dllPath);
					found = true;
				}

				Resource.CreateFromHeader(resourceHeader).WriteToPath(dllPath);
			}

			if (entry.FileType != FileTypes.Managed)
			{
				var proc = Process.Start(Path.Combine(dllPath, entry.Name), Environment.CommandLine);
				proc.WaitForExit();
				return;
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
