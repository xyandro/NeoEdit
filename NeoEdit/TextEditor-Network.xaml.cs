using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using NeoEdit.Program.Dialogs;
using NeoEdit.Program.Expressions;
using NeoEdit.Program.Parsing;
using NeoEdit.Program.Transform;
using NeoEdit.Program.WCF;

namespace NeoEdit.Program
{
	partial class TextEditor
	{
		static async Task FetchURL(string url, string fileName)
		{
			using (var client = new WebClient())
			{
				client.Headers["User-Agent"] = "Mozilla/5.0 (Windows; U; MSIE 9.0; Windows NT 9.0; en-US)";
				client.Encoding = Encoding.UTF8;
				await client.DownloadFileTaskAsync(url, fileName);
			}
		}

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

		async Task<List<Tuple<string, string, bool>>> GetURLs(List<string> urls, Coder.CodePage codePage = Coder.CodePage.None)
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
					data = $"<error>{Data.DefaultEnding}";
					data += $"\t<url>{urls[ctr]}</url>{Data.DefaultEnding}";
					data += $"\t<data>{Data.DefaultEnding}";
					for (; ex != null; ex = ex.InnerException)
						data += $"\t\t{ex.Message}{Data.DefaultEnding}";
					data += $"\t</data>{Data.DefaultEnding}";
					data += $"</error>{Data.DefaultEnding}";
				}
				results.Add(Tuple.Create(urls[ctr], data, error));
			}
			return results;
		}

		NetworkAbsoluteURLDialog.Result Command_Network_AbsoluteURL_Dialog() => NetworkAbsoluteURLDialog.Run(TabsParent, GetVariables());

		void Command_Network_AbsoluteURL(NetworkAbsoluteURLDialog.Result result)
		{
			var results = GetFixedExpressionResults<string>(result.Expression);
			var newStrs = Selections.Zip(results, (range, baseUrl) => new { range, baseUrl }).AsParallel().AsOrdered().Select(obj => new Uri(new Uri(obj.baseUrl), GetString(obj.range)).AbsoluteUri).ToList();
			ReplaceSelections(newStrs);
		}

		void Command_Network_Fetch(Coder.CodePage codePage = Coder.CodePage.None)
		{
			var urls = GetSelectionStrings();
			var results = Task.Run(() => GetURLs(urls, codePage).Result).Result;
			if (results.Any(result => result.Item3))
				new Message(TabsParent)
				{
					Title = "Error",
					Text = $"Failed to fetch the URLs:\n{string.Join("\n", results.Where(result => result.Item3).Select(result => result.Item1))}",
					Options = MessageOptions.Ok,
				}.Show();
			ReplaceSelections(results.Select(result => result.Item2).ToList());
		}

		NetworkFetchFileDialog.Result Command_Network_FetchFile_Dialog() => NetworkFetchFileDialog.Run(TabsParent, GetVariables());

		void Command_Network_FetchFile(NetworkFetchFileDialog.Result result)
		{
			var variables = GetVariables();

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
				if (!new Message(TabsParent)
				{
					Title = "Confirm",
					Text = $"Are you sure you want to overwrite these files:\n{string.Join("\n", invalid)}",
					Options = MessageOptions.YesNo,
					DefaultAccept = MessageOptions.Yes,
					DefaultCancel = MessageOptions.No,
				}.Show().HasFlag(MessageOptions.Yes))
					return;
			}

			MultiProgressDialog.RunAsync(TabsParent, "Fetching URLs", urls.Zip(fileNames, (url, fileName) => new { url, fileName }), (obj, progress, cancellationToken) => FetchURL(obj.url, obj.fileName), obj => obj.url);
		}

		NetworkFetchStreamDialog.Result Command_Network_FetchStream_Dialog() => NetworkFetchStreamDialog.Run(TabsParent, GetVariables(), Path.GetDirectoryName(FileName) ?? "");

		void Command_Network_FetchStream(NetworkFetchStreamDialog.Result result)
		{
			var urls = GetVariableExpressionResults<string>(result.Expression);
			if (!urls.Any())
				return;

			var now = DateTime.Now;
			var data = urls.Select((url, index) => Tuple.Create(url, now + TimeSpan.FromSeconds(index))).ToList();
			MultiProgressDialog.RunAsync(TabsParent, "Downloading...", data, async (item, progress, cancelled) => await YouTubeDL.DownloadStream(result.OutputDirectory, item.Item1, item.Item2, progress, cancelled));
		}

		NetworkFetchStreamDialog.Result Command_Network_FetchPlaylist_Dialog() => NetworkFetchStreamDialog.Run(TabsParent, GetVariables(), null);

		void Command_Network_FetchPlaylist(NetworkFetchStreamDialog.Result result)
		{
			var urls = GetVariableExpressionResults<string>(result.Expression);
			if (!urls.Any())
				return;

			var items = MultiProgressDialog.RunAsync(TabsParent, "Getting playlist contents...", urls, async (item, progress, cancelled) => await YouTubeDL.GetPlayListItems(item, progress, cancelled)).ToList();
			ReplaceSelections(items.Select(l => string.Join(Data.DefaultEnding, l)).ToList());
		}

		void Command_Network_Lookup_IP() { ReplaceSelections(Task.Run(async () => await Task.WhenAll(GetSelectionStrings().Select(async name => { try { return string.Join(" / ", (await Dns.GetHostEntryAsync(name)).AddressList.Select(address => address.ToString()).Distinct()); } catch { return "<ERROR>"; } }).ToList())).Result.ToList()); }

		void Command_Network_Lookup_HostName() { ReplaceSelections(Task.Run(async () => await Task.WhenAll(GetSelectionStrings().Select(async name => { try { return (await Dns.GetHostEntryAsync(name)).HostName; } catch { return "<ERROR>"; } }).ToList())).Result.ToList()); }

		void Command_Network_AdaptersInfo()
		{
			if (Selections.Count != 1)
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
			ReplaceOneWithMany(data.Select(row => string.Join("│", row.Select((item, column) => item + new string(' ', columnLens[column] - item.Length)))).ToList(), true);
		}

		NetworkPingDialog.Result Command_Network_Ping_Dialog() => NetworkPingDialog.Run(TabsParent);

		void Command_Network_Ping(NetworkPingDialog.Result result)
		{
			var replies = Task.Run(async () =>
			{
				var strs = GetSelectionStrings().Select(async str =>
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
			ReplaceSelections(replies);
		}

		NetworkScanPortsDialog.Result Command_Network_ScanPorts_Dialog() => NetworkScanPortsDialog.Run(TabsParent);

		void Command_Network_ScanPorts(NetworkScanPortsDialog.Result result)
		{
			var strs = GetSelectionStrings();
			var results = PortScanner.ScanPorts(strs.Select(str => IPAddress.Parse(str)).ToList(), result.Ports, result.Attempts, TimeSpan.FromMilliseconds(result.Timeout), result.Concurrency);
			ReplaceSelections(strs.Zip(results, (str, strResult) => $"{str}: {string.Join(", ", strResult)}").ToList());
		}

		NetworkWCFGetConfig.Result Command_Network_WCF_GetConfig_Dialog() => NetworkWCFGetConfig.Run(TabsParent);

		void Command_Network_WCF_GetConfig(NetworkWCFGetConfig.Result result)
		{
			if (Selections.Count != 1)
				throw new Exception("Must have single selection.");

			ReplaceSelections(WCFOperations.GetWCFConfig(result.URL));
		}

		void Command_Network_WCF_Execute() => ReplaceSelections(Selections.Select(range => WCFOperations.ExecuteWCF(GetString(range))).ToList());

		void Command_Network_WCF_ResetClients() => WCFOperations.ResetClients();
	}
}
