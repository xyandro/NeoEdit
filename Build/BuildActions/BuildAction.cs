using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Build.BuildActions
{
	class BuildAction : BaseAction
	{
		public override string Name => "Build";

		public override void Run(WriteTextDelegate writeText, string configuration)
		{
			var versions = new List<string> { "Community", "Enterprise", "Professional" };
			var template = @"C:\Program Files (x86)\Microsoft Visual Studio\2019\{0}\Common7\IDE\devenv.com";
			var devenv = versions.Select(version => string.Format(template, version)).FirstOrDefault(path => File.Exists(path));
			if (devenv == null)
				throw new Exception("Unable to find devenv");

			writeText($"Building {configuration}...");
			var arguments = $@"""{App.Location}\NeoEdit.sln"" /build ""{configuration}|x64"" /project NeoEdit";
			RunCommand(writeText, devenv, arguments);
		}
	}
}
