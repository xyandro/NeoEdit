using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using NeoEdit.Common;
using NeoEdit.Common.Enums;

namespace NeoEdit.Editor
{
	partial class Tabs
	{
		void Execute_Help_About() => TabsWindow.RunHelpAboutDialog();

		void Execute_Help_Tutorial() { }//TODO => new TutorialWindow(this);

		void Execute_Help_Update()
		{
			const string location = "https://github.com/xyandro/NeoEdit/releases";
			const string url = location + "/latest";
			const string check = location + "/tag/";
			const string exe = location + "/download/{0}/NeoEdit.exe";

			var oldVersion = ((AssemblyFileVersionAttribute)typeof(Tabs).Assembly.GetCustomAttribute(typeof(AssemblyFileVersionAttribute))).Version;
			string newVersion;

			var request = WebRequest.Create(url) as HttpWebRequest;
			request.AllowAutoRedirect = false;
			using (var response = request.GetResponse() as HttpWebResponse)
			{
				var redirUrl = response.Headers["Location"];
				if (!redirUrl.StartsWith(check))
					throw new Exception("Version check failed to find latest version");

				newVersion = redirUrl.Substring(check.Length);
			}

			var oldNums = oldVersion.Split('.').Select(str => int.Parse(str)).ToList();
			var newNums = newVersion.Split('.').Select(str => int.Parse(str)).ToList();
			if (oldNums.Count != newNums.Count)
				throw new Exception("Version length mismatch");

			var newer = oldNums.Zip(newNums, (oldNum, newNum) => newNum.IsGreater(oldNum)).NonNull().FirstOrDefault();
			if (!TabsWindow.RunMessageDialog("Download new version?", newer ? $"A newer version ({newVersion}) is available. Download it?" : $"Already up to date ({newVersion}). Update anyway?", MessageOptions.YesNo, newer ? MessageOptions.Yes : MessageOptions.No, MessageOptions.No).HasFlag(MessageOptions.Yes))
				return;

			var oldLocation = Assembly.GetEntryAssembly().Location;
			var newLocation = Path.Combine(Path.GetDirectoryName(oldLocation), $"{Path.GetFileNameWithoutExtension(oldLocation)}-Update{Path.GetExtension(oldLocation)}");

			byte[] result = null;

			TabsWindow.RunProgressDialog("Downloading new version...", (canceled, progress) =>
			{
				var finished = new ManualResetEvent(false);
				using (var client = new WebClient())
				{
					client.DownloadProgressChanged += (s, e) => progress(e.ProgressPercentage);
					client.DownloadDataCompleted += (s, e) =>
					{
						if (!e.Cancelled)
							result = e.Result;
						finished.Set();
					};
					client.DownloadDataAsync(new Uri(string.Format(exe, newVersion)));
					while (!finished.WaitOne(500))
						if (canceled())
							client.CancelAsync();
				}
			});

			if (result == null)
				return;

			File.WriteAllBytes(newLocation, result);

			Process.Start(newLocation, $@"-update ""{oldLocation}"" {Process.GetCurrentProcess().Id}");
			TabsWindow.RunMessageDialog("Info", "The program will be updated after exiting.");
		}

		void Execute_Help_Extract()
		{
			var location = Assembly.GetEntryAssembly().Location;

			if (!TabsWindow.RunMessageDialog("Extract files", $"Files will be extracted from {location} after program exits.", MessageOptions.OkCancel, MessageOptions.Ok, MessageOptions.Cancel).HasFlag(MessageOptions.Ok))
				return;

			Process.Start(location, $@"-extract {Process.GetCurrentProcess().Id}");
		}

		static void Execute_Help_RunGC() => GC.Collect();
	}
}
