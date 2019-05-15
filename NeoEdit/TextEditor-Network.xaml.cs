using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using NeoEdit;
using NeoEdit.Dialogs;
using NeoEdit.Expressions;
using NeoEdit.Transform;

namespace NeoEdit
{
	partial class TextEditor
	{
		static async Task<string> GetURL(string url, Coder.CodePage codePage = Coder.CodePage.None)
		{
			using (var client = new WebClient())
			{
				client.Headers["User-Agent"] = "Mozilla/5.0 (Windows; U; MSIE 9.0; Windows NT 9.0; en-US)";
				client.Encoding = Encoding.UTF8;
				var uri = new Uri(url);
				if (codePage == Coder.CodePage.None)
					return await client.DownloadStringTaskAsync(uri);

				var data = await client.DownloadDataTaskAsync(uri);
				return Coder.BytesToString(data, codePage);
			}
		}

		static async Task<List<Tuple<string, string, bool>>> GetURLs(ITextEditor te, List<string> urls, Coder.CodePage codePage = Coder.CodePage.None)
		{
			var tasks = urls.Select(url => GetURL(url, codePage)).ToList();
			var results = new List<Tuple<string, string, bool>>();
			for (var ctr = 0; ctr < tasks.Count; ++ctr)
			{
				string data;
				bool error = false;
				try { data = await tasks[ctr]; }
				catch (Exception ex)
				{
					error = true;
					data = $"<error>{te.Data.DefaultEnding}";
					data += $"\t<url>{urls[ctr]}</url>{te.Data.DefaultEnding}";
					data += $"\t<data>{te.Data.DefaultEnding}";
					for (; ex != null; ex = ex.InnerException)
						data += $"\t\t{ex.Message}{te.Data.DefaultEnding}";
					data += $"\t</data>{te.Data.DefaultEnding}";
					data += $"</error>{te.Data.DefaultEnding}";
				}
				results.Add(Tuple.Create(urls[ctr], data, error));
			}
			return results;
		}

		static async Task FetchURL(string url, string fileName)
		{
			using (var client = new WebClient())
			{
				client.Headers["User-Agent"] = "Mozilla/5.0 (Windows; U; MSIE 9.0; Windows NT 9.0; en-US)";
				client.Encoding = Encoding.UTF8;
				await client.DownloadFileTaskAsync(url, fileName);
			}
		}

		static NetworkAbsoluteURLDialog.Result Command_Network_AbsoluteURL_Dialog(ITextEditor te) => NetworkAbsoluteURLDialog.Run(te.WindowParent, te.GetVariables());

		static void Command_Network_AbsoluteURL(ITextEditor te, NetworkAbsoluteURLDialog.Result result)
		{
			var results = te.GetFixedExpressionResults<string>(result.Expression);
			var newStrs = te.Selections.Zip(results, (range, baseUrl) => new { range, baseUrl }).AsParallel().AsOrdered().Select(obj => new Uri(new Uri(obj.baseUrl), te.GetString(obj.range)).AbsoluteUri).ToList();
			te.ReplaceSelections(newStrs);
		}

		static void Command_Network_Fetch(ITextEditor te, Coder.CodePage codePage = Coder.CodePage.None)
		{
			var urls = te.GetSelectionStrings();
			var results = Task.Run(() => GetURLs(te, urls, codePage).Result).Result;
			if (results.Any(result => result.Item3))
				new Message(te.WindowParent)
				{
					Title = "Error",
					Text = $"Failed to fetch the URLs:\n{string.Join("\n", results.Where(result => result.Item3).Select(result => result.Item1))}",
					Options = MessageOptions.Ok,
				}.Show();
			te.ReplaceSelections(results.Select(result => result.Item2).ToList());
		}

		static NetworkFetchFileDialog.Result Command_Network_FetchFile_Dialog(ITextEditor te) => NetworkFetchFileDialog.Run(te.WindowParent, te.GetVariables());

		static void Command_Network_FetchFile(ITextEditor te, NetworkFetchFileDialog.Result result)
		{
			var variables = te.GetVariables();

			var urlExpression = new NEExpression(result.URL);
			var fileNameExpression = new NEExpression(result.FileName);
			var resultCount = variables.ResultCount(urlExpression, fileNameExpression);

			var urls = urlExpression.EvaluateList<string>(variables, resultCount);
			var fileNames = fileNameExpression.EvaluateList<string>(variables, resultCount);

			const int InvalidCount = 10;
			var invalid = fileNames.Select(name => Path.GetDirectoryName(name)).Distinct().Where(dir => !Directory.Exists(dir)).Take(InvalidCount).ToList();
			if (invalid.Any())
				throw new Exception($"Directories don't exist:\n{string.Join("\n", invalid)}");

			invalid = fileNames.Where(fileName => File.Exists(fileName)).Distinct().Take(InvalidCount).ToList();
			if (invalid.Any())
			{
				if (new Message(te.WindowParent)
				{
					Title = "Confirm",
					Text = $"Are you sure you want to overwrite these files:\n{string.Join("\n", invalid)}",
					Options = MessageOptions.YesNo,
					DefaultAccept = MessageOptions.Yes,
					DefaultCancel = MessageOptions.No,
				}.Show() != MessageOptions.Yes)
					return;
			}

			MultiProgressDialog.RunAsync(te.WindowParent, "Fetching URLs", urls.Zip(fileNames, (url, fileName) => new { url, fileName }), (obj, progress, cancellationToken) => FetchURL(obj.url, obj.fileName), obj => obj.url);
		}

		static NetworkFetchStreamDialog.Result Command_Network_FetchStream_Dialog(ITextEditor te) => NetworkFetchStreamDialog.Run(te.WindowParent, te.GetVariables(), Path.GetDirectoryName(te.FileName) ?? "");

		static void Command_Network_FetchStream(ITextEditor te, NetworkFetchStreamDialog.Result result)
		{
			var urls = te.GetVariableExpressionResults<string>(result.Expression);
			if (!urls.Any())
				return;

			var now = DateTime.Now;
			var data = urls.Select((url, index) => Tuple.Create(url, now + TimeSpan.FromSeconds(index))).ToList();
			MultiProgressDialog.RunAsync(te.WindowParent, "Downloading...", data, async (item, progress, cancelled) => await YouTubeDL.DownloadStream(result.OutputDirectory, item.Item1, item.Item2, progress, cancelled));
		}

		static NetworkFetchStreamDialog.Result Command_Network_FetchPlaylist_Dialog(ITextEditor te) => NetworkFetchStreamDialog.Run(te.WindowParent, te.GetVariables(), null);

		static void Command_Network_FetchPlaylist(ITextEditor te, NetworkFetchStreamDialog.Result result)
		{
			var urls = te.GetVariableExpressionResults<string>(result.Expression);
			if (!urls.Any())
				return;

			var items = MultiProgressDialog.RunAsync(te.WindowParent, "Getting playlist contents...", urls, async (item, progress, cancelled) => await YouTubeDL.GetPlayListItems(item, progress, cancelled)).ToList();
			te.ReplaceSelections(items.Select(l => string.Join(te.Data.DefaultEnding, l)).ToList());
		}

		static void Command_Network_Lookup_IP(ITextEditor te) { te.ReplaceSelections(Task.Run(async () => await Task.WhenAll(te.GetSelectionStrings().Select(async name => { try { return string.Join(" / ", (await Dns.GetHostEntryAsync(name)).AddressList.Select(address => address.ToString()).Distinct()); } catch { return "<ERROR>"; } }).ToList())).Result.ToList()); }

		static void Command_Network_Lookup_HostName(ITextEditor te) { te.ReplaceSelections(Task.Run(async () => await Task.WhenAll(te.GetSelectionStrings().Select(async name => { try { return (await Dns.GetHostEntryAsync(name)).HostName; } catch { return "<ERROR>"; } }).ToList())).Result.ToList()); }

		static void Command_Network_AdaptersInfo(ITextEditor te)
		{
			if (te.Selections.Count != 1)
				throw new Exception("Must have one selection.");

			var data = new List<List<string>>();
			data.Add(new List<string>
			{
				"Name",
				"Desc",
				"Status",
				"Type",
				"IPs"
			});
			foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces().OrderBy(inter => inter.OperationalStatus).ThenBy(inter => inter.Name))
			{
				var props = networkInterface.GetIPProperties();
				data.Add(new List<string>
				{
					networkInterface.Name,
					networkInterface.Description,
					networkInterface.OperationalStatus.ToString(),
					networkInterface.NetworkInterfaceType.ToString(),
					string.Join(" / ", props.UnicastAddresses.Select(info=>info.Address)),
				});
			}
			var columnLens = data[0].Select((item, column) => data.Max(row => row[column].Length)).ToList();
			te.ReplaceOneWithMany(data.Select(row => string.Join("│", row.Select((item, column) => item + new string(' ', columnLens[column] - item.Length)))).ToList(), true);
		}

		static NetworkPingDialog.Result Command_Network_Ping_Dialog(ITextEditor te) => NetworkPingDialog.Run(te.WindowParent);

		static void Command_Network_Ping(ITextEditor te, NetworkPingDialog.Result result)
		{
			var replies = Task.Run(async () =>
			{
				var strs = te.GetSelectionStrings().Select(async str =>
				{
					try
					{
						using (var ping = new Ping())
						{
							var reply = await ping.SendPingAsync(IPAddress.Parse(str), result.Timeout);
							return $"{str}: {reply.Status}{(reply.Status == IPStatus.Success ? $": {reply.RoundtripTime} ms" : "")}";
						}
					}
					catch (Exception ex)
					{
						return $"{str}: {ex.Message}";
					}
				}).ToList();
				return await Task.WhenAll(strs);
			}).Result.ToList();
			te.ReplaceSelections(replies);
		}

		static NetworkScanPortsDialog.Result Command_Network_ScanPorts_Dialog(ITextEditor te) => NetworkScanPortsDialog.Run(te.WindowParent);

		static void Command_Network_ScanPorts(ITextEditor te, NetworkScanPortsDialog.Result result)
		{
			var strs = te.GetSelectionStrings();
			var results = PortScanner.ScanPorts(strs.Select(str => IPAddress.Parse(str)).ToList(), result.Ports, result.Attempts, TimeSpan.FromMilliseconds(result.Timeout), result.Concurrency);
			te.ReplaceSelections(strs.Zip(results, (str, strResult) => $"{str}: {string.Join(", ", strResult)}").ToList());
		}
	}
}
