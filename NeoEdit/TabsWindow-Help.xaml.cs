using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using NeoEdit.Program.Dialogs;
using NeoEdit.Program.Tutorial;

namespace NeoEdit.Program
{
	partial class TabsWindow
	{
		static void Execute_Help_About() => HelpAboutDialog.Run();

		void Execute_Help_Tutorial() => new TutorialWindow(this);

		static void Execute_Help_Update()
		{
			const string location = "https://github.com/xyandro/NeoEdit/releases";
			const string url = location + "/latest";
			const string check = location + "/tag/";
			const string exe = location + "/download/{0}/NeoEdit.exe";

			var oldVersion = ((AssemblyFileVersionAttribute)typeof(TabsWindow).Assembly.GetCustomAttribute(typeof(AssemblyFileVersionAttribute))).Version;
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
			if (!new Message
			{
				Title = "Download new version?",
				Text = newer ? $"A newer version ({newVersion}) is available. Download it?" : $"Already up to date ({newVersion}). Update anyway?",
				Options = MessageOptions.YesNo,
				DefaultAccept = newer ? MessageOptions.Yes : MessageOptions.No,
				DefaultCancel = MessageOptions.No,
			}.Show().HasFlag(MessageOptions.Yes))
				return;

			var oldLocation = Assembly.GetEntryAssembly().Location;
			var newLocation = Path.Combine(Path.GetDirectoryName(oldLocation), $"{Path.GetFileNameWithoutExtension(oldLocation)}-Update{Path.GetExtension(oldLocation)}");

			byte[] result = null;
			ProgressDialog.Run(null, "Downloading new version...", (canceled, progress) =>
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
			Message.Show("The program will be updated after exiting.");
		}

		static void Execute_Help_Extract()
		{
			var location = Assembly.GetEntryAssembly().Location;

			if (!new Message
			{
				Title = "Extract files",
				Text = $"Files will be extracted from {location} after program exits.",
				Options = MessageOptions.OkCancel,
				DefaultAccept = MessageOptions.Ok,
				DefaultCancel = MessageOptions.Cancel,
			}.Show().HasFlag(MessageOptions.Ok))
				return;

			Process.Start(location, $@"-extract {Process.GetCurrentProcess().Id}");
		}

		static void Execute_Help_RunGC() => GC.Collect();
	}
}
