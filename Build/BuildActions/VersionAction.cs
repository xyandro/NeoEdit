using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Build.BuildActions
{
	class VersionAction : BaseAction
	{
		public override string Name => "Version";

		public override void Run(WriteTextDelegate writeText)
		{
			var version = WriteVersionProject();
			WriteGlobalAssemblyInfo(version);
			WriteSetupProject(version);
		}

		string Replace(string text, string pattern, string replacement)
		{
			var match = Regex.Match(text, pattern);
			if (!match.Success)
				throw new Exception($"Failed to find pattern {pattern}");
			return $"{text[..match.Index]}{replacement}{text[(match.Index + match.Length)..]}";
		}

		string WriteVersionProject()
		{
			var path = Path.Combine(App.Location, "Version.csproj");
			var text = File.ReadAllText(path);

			var match = Regex.Match(text, @"<AssemblyVersion>(\d+)\.(\d+)\.1\.1</AssemblyVersion>");
			if (!match.Success)
				throw new Exception("Can't find AssemblyVersion tag");

			var version = $"{match.Groups[1].Value}.{match.Groups[2].Value}.1.{Git.CommitCount()}";

			text = $@"{text[..match.Index]}<AssemblyVersion>{version}</AssemblyVersion>{text[(match.Index + match.Length)..]}";
			text = Replace(text, @"<FileVersion>1\.1\.1\.1</FileVersion>", $"<FileVersion>{version}</FileVersion>");
			text = Replace(text, @"<Version>1\.1\.1\.1</Version>", $"<Version>{version}</Version>");
			text = Replace(text, @"<Copyright>© Randon Spackensen 2013-2013</Copyright>", $"<Copyright>© Randon Spackensen 2013-{DateTime.Now.Year}</Copyright>");

			File.WriteAllText(path, text, Encoding.UTF8);

			return version;
		}

		void WriteGlobalAssemblyInfo(string version)
		{
			var path = Path.Combine(App.Location, "GlobalAssemblyInfo.cs");
			var text = File.ReadAllText(path);

			text = Replace(text, @"\[assembly: AssemblyCopyright\(""© Randon Spackensen 2013-2013""\)]", $@"[assembly: AssemblyCopyright(""© Randon Spackensen 2013-{DateTime.Now.Year}"")]");
			text = Replace(text, @"\[assembly: AssemblyVersion\(""1\.1\.1\.1""\)]", $@"[assembly: AssemblyVersion(""{version}"")]");
			text = Replace(text, @"\[assembly: AssemblyFileVersion\(""1\.1\.1\.1""\)]", $@"[assembly: AssemblyFileVersion(""{version}"")]");

			File.WriteAllText(path, text, Encoding.UTF8);
		}

		string ShortVersion(string version) => string.Join(".", version.Split(".").Where((x, index) => index != 2));

		void WriteSetupProject(string version)
		{
			var path = Path.Combine(App.Location, "NeoEdit.Setup", "NeoEdit.Setup.vdproj");
			var text = File.ReadAllText(path);

			text = Replace(text, @"""ProductCode"" = ""8:{00000000-0000-0000-0000-000000000000}""", $@"""ProductCode"" = ""8:{{{Guid.NewGuid().ToString().ToUpper()}}}""");
			text = Replace(text, @"""PackageCode"" = ""8:{00000000-0000-0000-0000-000000000000}""", $@"""PackageCode"" = ""8:{{{Guid.NewGuid().ToString().ToUpper()}}}""");
			text = Replace(text, @"""ProductVersion"" = ""8:1\.1\.1""", $@"""ProductVersion"" = ""8:{ShortVersion(version)}""");

			File.WriteAllText(path, text, Encoding.UTF8);
		}
	}
}
