using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Build.BuildActions
{
	class BuildAction : BaseAction
	{
		public override string Name => "Build";

		public override void Run(WriteTextDelegate writeText)
		{
			var versions = new List<string> { "Community", "Enterprise", "Professional" };
			var devEnvTemplate = @"C:\Program Files\Microsoft Visual Studio\2022\{0}\Common7\IDE\devenv.com";
			var devEnv = versions.Select(version => string.Format(devEnvTemplate, version)).FirstOrDefault(path => File.Exists(path));
			if (devEnv == null)
				throw new Exception("Unable to find devenv");

			RunCommand(writeText, devEnv, $@"""{App.Location}\NeoEdit.sln"" /build ""Release|Any CPU"" /project NeoEdit.Setup");
		}
	}
}
