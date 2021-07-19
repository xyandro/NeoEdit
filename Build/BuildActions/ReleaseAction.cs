using System;
using System.Diagnostics;
using System.IO;

namespace Build.BuildActions
{
	class ReleaseAction : BaseAction
	{
		public override string Name => "Release";

		public override bool Prepare() => App.EnsureGitHubTokenExists();

		public override void Run(WriteTextDelegate writeText)
		{
			var version = FileVersionInfo.GetVersionInfo($@"{App.Location}\NeoEdit\bin\Release\net5.0-windows\NeoEdit.exe").FileVersion;

			var msiName = $@"{App.Location}\NeoEdit.Setup\Release\NeoEdit.msi";
			if (!File.Exists(msiName))
				throw new Exception($"Build not found: {msiName}.");

			using (var client = new GitHub())
			{
				writeText($"Version is {version}");

				writeText($"Creating release {version}.");
				var uploadUrl = client.CreateRelease(version).Result;

				writeText($"Uploading {msiName}...");
				writeText($"0%");
				client.UploadFile(uploadUrl, msiName, percent => writeText($"\udead{percent}%")).Wait();
				writeText("\udead100%");
			}
		}
	}
}
