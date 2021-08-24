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
				throw new Exception($"MSI not found: {msiName}.");
			var zipName = $@"{App.Location}\NeoEdit.Setup\Release\NeoEdit.zip";
			if (!File.Exists(zipName))
				throw new Exception($"Zip not found: {zipName}.");

			using var git = new GitHub();

			writeText($"Version is {version}");

			var releaseId = git.GetReleaseID(version).Result;
			if (releaseId != null)
			{
				writeText($"Delete duplicate release {releaseId}...");
				git.DeleteRelease(releaseId.Value).Wait();
			}

			var tagId = git.GetTagID(version).Result;
			if (tagId != null)
			{
				writeText($"Delete duplicate tag {tagId}...");
				git.DeleteTag(tagId).Wait();
			}

			writeText($"Creating release {version}.");
			var uploadUrl = git.CreateRelease(version).Result;

			writeText($"Uploading {msiName}...");
			writeText($"0%");
			git.UploadFile(uploadUrl, msiName, percent => writeText($"\udead{percent}%")).Wait();
			writeText("\udead100%");

			writeText($"Uploading {zipName}...");
			writeText($"0%");
			git.UploadFile(uploadUrl, zipName, percent => writeText($"\udead{percent}%")).Wait();
			writeText("\udead100%");
		}
	}
}
