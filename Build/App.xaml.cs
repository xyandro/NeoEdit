using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Build
{
	partial class App
	{
		public static string Location { get; } = Path.GetDirectoryName(typeof(App).Assembly.Location);
		public static string GitHubToken { get; private set; }

		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern void SetDllDirectory(string lpPathName);

		public static bool EnsureGitHubTokenExists()
		{
			if (GitHubToken == null)
				GitHubToken = PasswordDialog.Run();
			return GitHubToken != null;
		}

		public App()
		{
			SetDllDirectory(Path.Combine(Path.Combine(Location, "Lib", "32")));
			AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
		}

		Assembly ResolveAssembly(object sender, ResolveEventArgs args)
		{
			var name = args.Name.Substring(0, args.Name.IndexOf(','));
			var path = Path.Combine(Location, "Lib", $"{name}.dll");
			if (File.Exists(path))
				return Assembly.LoadFrom(path);
			return null;
		}
	}
}
