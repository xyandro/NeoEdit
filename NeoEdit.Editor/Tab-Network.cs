using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;
using NeoEdit.Editor.TaskRunning;
using NeoEdit.WCF;

namespace NeoEdit.Editor
{
	partial class Tab
	{
		static void FetchURL(string url, string fileName)
		{
			using (var client = new WebClient())
			{
				client.Headers["User-Agent"] = "Mozilla/5.0 (Windows; U; MSIE 9.0; Windows NT 9.0; en-US)";
				client.Encoding = Encoding.UTF8;
				client.DownloadFile(url, fileName);
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
					data = $"<error>{TextView.DefaultEnding}";
					data += $"\t<url>{urls[ctr]}</url>{TextView.DefaultEnding}";
					data += $"\t<data>{TextView.DefaultEnding}";
					for (; ex != null; ex = ex.InnerException)
						data += $"\t\t{ex.Message}{TextView.DefaultEnding}";
					data += $"\t</data>{TextView.DefaultEnding}";
					data += $"</error>{TextView.DefaultEnding}";
				}
				results.Add(Tuple.Create(urls[ctr], data, error));
			}
			return results;
		}

		NetworkAbsoluteURLDialogResult Configure_Network_AbsoluteURL() => Tabs.TabsWindow.RunNetworkAbsoluteURLDialog(GetVariables());

		void Execute_Network_AbsoluteURL()
		{
			var result = state.Configuration as NetworkAbsoluteURLDialogResult;
			var results = GetExpressionResults<string>(result.Expression, Selections.Count());
			ReplaceSelections(Selections.Select((range, index) => new Uri(new Uri(results[index]), Text.GetString(range)).AbsoluteUri).ToList());
		}

		void Execute_Network_Fetch(Coder.CodePage codePage = Coder.CodePage.None)
		{
			var urls = GetSelectionStrings();
			var results = Task.Run(() => GetURLs(urls, codePage).Result).Result;
			if (results.Any(result => result.Item3))
				throw new Exception($"Failed to fetch the URLs:\n{string.Join("\n", results.Where(result => result.Item3).Select(result => result.Item1))}");
			ReplaceSelections(results.Select(result => result.Item2).ToList());
		}

		NetworkFetchFileDialogResult Configure_Network_FetchFile() => Tabs.TabsWindow.RunNetworkFetchFileDialog(GetVariables());

		void Execute_Network_FetchFile()
		{
			var result = state.Configuration as NetworkFetchFileDialogResult;
			var variables = GetVariables();

			var urlExpression = state.GetExpression(result.URL);
			var fileNameExpression = state.GetExpression(result.FileName);
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
				if (!QueryUser(nameof(Execute_Network_FetchFile), $"Are you sure you want to overwrite these files:\n{string.Join("\n", invalid)}", MessageOptions.Yes))
					return;
			}

			TaskRunner.Range(0, urls.Count).ParallelForEach(index => FetchURL(urls[index], fileNames[index]));
		}

		NetworkFetchStreamDialogResult Configure_Network_FetchStream() => Tabs.TabsWindow.RunNetworkFetchStreamDialog(GetVariables(), Path.GetDirectoryName(FileName) ?? "");

		void Execute_Network_FetchStream()
		{
			var result = state.Configuration as NetworkFetchStreamDialogResult;
			var urls = GetExpressionResults<string>(result.Expression);
			if (!urls.Any())
				return;

			var now = DateTime.Now;
			var data = urls.Select((url, index) => Tuple.Create(url, now + TimeSpan.FromSeconds(index))).ToList();
			data.AsTaskRunner().ParallelForEach((item, progress) => YouTubeDL.DownloadStream(result.OutputDirectory, item.Item1, item.Item2, progress));
		}

		NetworkFetchStreamDialogResult Configure_Network_FetchPlaylist() => Tabs.TabsWindow.RunNetworkFetchStreamDialog(GetVariables(), null);

		void Execute_Network_FetchPlaylist()
		{
			var result = state.Configuration as NetworkFetchStreamDialogResult;
			var urls = GetExpressionResults<string>(result.Expression);
			if (!urls.Any())
				return;

			urls.AsTaskRunner()
				.Select(url => string.Join(TextView.DefaultEnding, YouTubeDL.GetPlayListItems(url)))
				.ToList(taskResults => ReplaceSelections(taskResults));
		}

		void Execute_Network_Lookup_IP() { ReplaceSelections(Task.Run(async () => await Task.WhenAll(GetSelectionStrings().Select(async name => { try { return string.Join(" / ", (await Dns.GetHostEntryAsync(name)).AddressList.Select(address => address.ToString()).Distinct()); } catch { return "<ERROR>"; } }).ToList())).Result.ToList()); }

		void Execute_Network_Lookup_HostName() { ReplaceSelections(Task.Run(async () => await Task.WhenAll(GetSelectionStrings().Select(async name => { try { return (await Dns.GetHostEntryAsync(name)).HostName; } catch { return "<ERROR>"; } }).ToList())).Result.ToList()); }

		void Execute_Network_AdaptersInfo()
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

		NetworkPingDialogResult Configure_Network_Ping() => Tabs.TabsWindow.RunNetworkPingDialog();

		void Execute_Network_Ping()
		{
			var result = state.Configuration as NetworkPingDialogResult;
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

		NetworkScanPortsDialogResult Configure_Network_ScanPorts() => Tabs.TabsWindow.RunNetworkScanPortsDialog();

		void Execute_Network_ScanPorts()
		{
			var result = state.Configuration as NetworkScanPortsDialogResult;
			var strs = GetSelectionStrings();
			var results = PortScanner.ScanPorts(strs.Select(str => IPAddress.Parse(str)).ToList(), result.Ports, result.Attempts, TimeSpan.FromMilliseconds(result.Timeout), result.Concurrency);
			ReplaceSelections(strs.Zip(results, (str, strResult) => $"{str}: {string.Join(", ", strResult)}").ToList());
		}

		NetworkWCFGetConfigDialogResult Configure_Network_WCF_GetConfig() => Tabs.TabsWindow.RunNetworkWCFGetConfigDialog();

		void Execute_Network_WCF_GetConfig()
		{
			var result = state.Configuration as NetworkWCFGetConfigDialogResult;
			if (Selections.Count != 1)
				throw new Exception("Must have single selection.");

			ReplaceSelections(WCFClient.GetWCFConfig(result.URL));
			Settings.AddWCFUrl(result.URL);
		}

		void Execute_Network_WCF_Execute() => ReplaceSelections(Selections.Select(range => WCFClient.ExecuteWCF(Text.GetString(range))).ToList());

		NetworkWCFInterceptCallsDialogResult Configure_Network_WCF_InterceptCalls() => Tabs.TabsWindow.RunNetworkWCFInterceptCallsDialog();

		void Execute_Network_WCF_InterceptCalls()
		{
			var result = state.Configuration as NetworkWCFInterceptCallsDialogResult;
			if (Selections.Count != 1)
				throw new Exception("Must have single selection.");

			WCFClient.StartInterceptCalls(result.WCFURL, result.InterceptURL);
			Tabs.TabsWindow.RunWCFInterceptDialog();
			var values = WCFClient.EndInterceptCalls(result.WCFURL);
			if (!values.Any())
				return;

			var startSel = Selections[0].Start;
			ReplaceSelections(string.Join("", values));
			var sels = new List<Range>();
			foreach (var value in values)
			{
				var endSel = startSel + value.Length;
				sels.Add(new Range(endSel, startSel));
				startSel = endSel;
			}
			Selections = sels;
		}

		void Execute_Network_WCF_ResetClients() => WCFClient.ResetClients();
	}
}
