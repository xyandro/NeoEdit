using System.Collections.Generic;

namespace Build.BuildActions
{
	class BuildAction : BaseAction
	{
		public override string Name => "Build";

		public override void Run(WriteTextDelegate writeText, string configuration, List<string> platforms)
		{
			const string devenv = @"C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE\devenv.com";
			foreach (var platform in platforms)
			{
				writeText($"Building {configuration}.{platform}...");
				var arguments = $@"""{App.Location}\NeoEdit.sln"" /build ""{configuration}|{platform}"" /project Loader";
				RunCommand(writeText, devenv, arguments);
			}
		}
	}
}
