using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Build.BuildActions
{
	class RestorePackagesAction : BaseAction
	{
		public override string Name => "Restore packages";

		public override void Run(WriteTextDelegate writeText)
		{
			var versions = new List<string> { "Community", "Enterprise", "Professional" };
			var msBuildTemplate = @"C:\Program Files (x86)\Microsoft Visual Studio\2019\{0}\MSBuild\Current\Bin\msbuild.exe";
			var msBuild = versions.Select(version => string.Format(msBuildTemplate, version)).FirstOrDefault(path => File.Exists(path));
			if (msBuild == null)
				throw new Exception("Unable to find msbuild");

			RunCommand(writeText, msBuild, $@"/m /t:restore /p:RestorePackagesConfig=true ""{App.Location}\NeoEdit.sln""");
		}
	}
}
