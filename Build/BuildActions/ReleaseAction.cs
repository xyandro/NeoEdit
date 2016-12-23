using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Build.BuildActions
{
	class ReleaseAction : BaseAction
	{
		public override string Name => "Release";

		public override bool Prepare() => App.EnsureGitHubTokenExists();

		public override void Run(WriteTextDelegate writeText, string configuration, List<string> platforms)
		{
			var exeName = $@"{App.Location}\Release\NeoEdit.exe";
			if (!File.Exists(exeName))
				throw new Exception($"Build not found: {exeName}.");
			var version = FileVersionInfo.GetVersionInfo(exeName).FileVersion;

			using (var client = new GitHub())
			{
				writeText($"Version is {version}");

				writeText($"Creating release {version}.");
				var uploadUrl = client.CreateRelease(version).Result;

				writeText($"Uploading {exeName}...");
				writeText($"0%");
				client.UploadFile(uploadUrl, exeName, percent => writeText($"\udead{percent}%")).Wait();
				writeText("\udead100%");
			}
		}
	}
}
