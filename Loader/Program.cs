using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace NeoEdit.Loader
{
	static class Program
	{
		static readonly byte[] NeoEditTag = Encoding.UTF8.GetBytes("NeoEdit.Loader");
		const string LoaderSuffix = ".Loader";
		const string ExtractorSuffix = LoaderSuffix + ".Extractor";

		static IEnumerable<Resource> Resources
		{
			get
			{
				using (var file = File.OpenRead(typeof(Program).Assembly.Location))
				{
					using (var reader = new BinaryReader(file, Encoding.UTF8, true))
					{
						file.Position = file.Length - NeoEditTag.Length;
						if (!NeoEditTag.Zip(reader.ReadBytes(NeoEditTag.Length), (l, r) => l == r).All(x => x))
							yield break;

						file.Position = file.Length - NeoEditTag.Length - sizeof(long);
						file.Position = reader.ReadInt64();
					}

					using (var resourceReader = new ResourceReader(file))
						foreach (var resource in resourceReader.Cast<DictionaryEntry>())
							yield return Resource.CreateFromSerialized(resource.Value as byte[]);
				}
			}
		}

		static void DeleteDelayed(string path)
		{
			string command;
			if (File.Exists(path))
				command = String.Format(@"DEL ""{0}""", path);
			else if (Directory.Exists(path))
				command = String.Format(@"RD /S /Q ""{0}""", path);
			else
				return;

			Process.Start(new ProcessStartInfo()
			{
				FileName = "cmd.exe",
				Arguments = String.Format(@"/C choice /C Y /N /D Y /T 1 & {0}", command),
				WindowStyle = ProcessWindowStyle.Hidden,
				CreateNoWindow = true,
			});
		}

		static string TrimEnd(this string str, string trim, StringComparison comparison = StringComparison.Ordinal)
		{
			if (!str.EndsWith(trim, comparison))
				return str;
			return str.Substring(0, str.Length - trim.Length);
		}

		static void SaveResources()
		{
			var location = typeof(Program).Assembly.Location;

			var dllPath = Path.GetDirectoryName(location);
			var resources = new List<Resource>();
			foreach (var fileName in Directory.EnumerateFiles(dllPath))
			{
				if (!Resource.IsAssembly(fileName))
					continue;

				resources.Add(Resource.CreateFromFile(fileName));

				if (fileName != location)
					File.Delete(fileName);
			}

			var newLocation = Path.Combine(Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location).TrimEnd(LoaderSuffix) + Path.GetExtension(location));
			using (var output = File.Create(newLocation))
			using (var resourceWriter = new ResourceWriter(output))
			{
				using (var input = File.OpenRead(location))
					input.CopyTo(output);

				var startPosition = output.Position;

				foreach (var resource in resources)
					resourceWriter.AddResource(resource.Name, resource.SerializedData);

				resourceWriter.Generate();

				using (var outputWriter = new BinaryWriter(output))
				{
					outputWriter.Write(startPosition);
					outputWriter.Write(NeoEditTag);
				}
			}

			DeleteDelayed(location);
		}

		static void Extract()
		{
			var location = typeof(Program).Assembly.Location;
			var newLocation = Path.Combine(Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location) + ExtractorSuffix + Path.GetExtension(location));
			File.Copy(location, newLocation, true);
			Process.Start(newLocation, String.Format(@"{0} ""{1}""", Process.GetCurrentProcess().Id, location));
		}

		static void WaitForParentExit(int pid)
		{
			try { Process.GetProcessById(pid).WaitForExit(); } catch { }
		}

		static void RunNGen()
		{
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
				Arguments = @"install ""NeoEdit.exe""",
				Verb = "runas",
			});
		}

		static void Extractor(int parentPid, string parentPath)
		{
			WaitForParentExit(parentPid);
			File.Delete(parentPath);

			var exeFile = typeof(Program).Assembly.Location;
			var location = Path.GetDirectoryName(exeFile);
			foreach (var resource in Resources)
				resource.WriteToPath(location);

			RunNGen();

			DeleteDelayed(exeFile);
		}

		static Dictionary<string, Assembly> resolved = new Dictionary<string, Assembly>();
		static Assembly AssemblyResolve(object sender, ResolveEventArgs args)
		{
			var name = new AssemblyName(args.Name).Name;
			if (!resolved.ContainsKey(name))
			{
				var resource = Resources.FirstOrDefault(res => res.NameMatch(name));
				resolved[name] = resource == null ? null : resource.Assembly;
			}

			return resolved[name];
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		static extern void SetDllDirectory(string lpPathName);

		static void RunProgram()
		{
			var dllPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NeoEdit", "DLLs");
			Directory.CreateDirectory(dllPath);
			foreach (var resource in Resources.Where(resource => !resource.Managed))
				resource.WriteToPath(dllPath);
			SetDllDirectory(dllPath);

			AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;

			try { AppDomain.CurrentDomain.ExecuteAssemblyByName("NeoEdit.exe"); } catch { }

			DeleteDelayed(dllPath);
		}

		[STAThread]
		static void Main(string[] args)
		{
			try
			{
				if (!Resources.Any())
					SaveResources();
				else if (Path.GetFileNameWithoutExtension(typeof(Program).Assembly.Location).EndsWith(ExtractorSuffix))
					Extractor(int.Parse(args[0]), args[1]);
				else if ((Keyboard.Modifiers.HasFlag(ModifierKeys.Control | ModifierKeys.Shift)) || ((args.Length == 1) && (args[0] == "-extract")))
					Extract();
				else
					RunProgram();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.StackTrace, ex.Message);
			}
		}
	}
}
