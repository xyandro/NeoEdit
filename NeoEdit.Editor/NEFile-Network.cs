using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;
using NeoEdit.Editor.Models;
using NeoEdit.TaskRunning;
using Newtonsoft.Json;

namespace NeoEdit.Editor
{
	partial class NEFile
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

		async Task<List<Tuple<string, string, bool>>> GetURLs(IReadOnlyList<string> urls, Coder.CodePage codePage = Coder.CodePage.None)
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
					data = $"<error>{Text.DefaultEnding}";
					data += $"\t<url>{urls[ctr]}</url>{Text.DefaultEnding}";
					data += $"\t<data>{Text.DefaultEnding}";
					for (; ex != null; ex = ex.InnerException)
						data += $"\t\t{ex.Message}{Text.DefaultEnding}";
					data += $"\t</data>{Text.DefaultEnding}";
					data += $"</error>{Text.DefaultEnding}";
				}
				results.Add(Tuple.Create(urls[ctr], data, error));
			}
			return results;
		}

		static NetworkRequest ParseNetworkRequest(string data)
		{
			if (string.IsNullOrWhiteSpace(data))
				throw new Exception($"Invalid request: {data}");

			var d = data.Trim();
			if (d.StartsWith("{"))
				return JsonConvert.DeserializeObject<NetworkRequest>(data);

			return new NetworkRequest { Request = new NetworkRequest.NetworkRequestRequest { Method = "GET", URL = data } };
		}

		static async Task<string> MakeNetworkRequest(HttpClient client, string data)
		{
			var networkRequest = ParseNetworkRequest(data);

			var request = new HttpRequestMessage(new HttpMethod(networkRequest.Request.Method), networkRequest.Request.URL);
			if ((!string.IsNullOrWhiteSpace(networkRequest.Request.Authentication?.Scheme)) && (!string.IsNullOrWhiteSpace(networkRequest.Request.Authentication?.Parameter)))
				request.Headers.Authorization = new AuthenticationHeaderValue(networkRequest.Request.Authentication.Scheme, networkRequest.Request.Authentication.Parameter);
			if (!string.IsNullOrWhiteSpace(networkRequest.Request.Body?.Content))
			{
				var content = new ByteArrayContent(Convert.FromBase64String(networkRequest.Request.Body?.Content));
				content.Headers.ContentType = new MediaTypeHeaderValue(networkRequest.Request.Body.MediaType) { CharSet = networkRequest.Request.Body.CharSet };
				request.Content = content;
			}

			var result = await client.SendAsync(request);
			networkRequest.Response = new NetworkRequest.NetworkRequestResponse();
			networkRequest.Response.Success = result.IsSuccessStatusCode;
			networkRequest.Response.StatusCode = (int)result.StatusCode;
			networkRequest.Response.StatusCodeText = result.StatusCode.ToString();
			networkRequest.Response.Response = Convert.ToBase64String(await result.Content.ReadAsByteArrayAsync());
			return JsonConvert.SerializeObject(networkRequest, Formatting.Indented);
		}

		static async Task<IReadOnlyList<string>> MakeNetworkRequests(IReadOnlyList<string> sels)
		{
			using var httpClient = new HttpClient();
			return await Helpers.RunTasks(sels, sel => MakeNetworkRequest(httpClient, sel));
		}

		static void Configure_Network_AbsoluteURL() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Network_AbsoluteURL(state.NEWindow.Focused.GetVariables());

		void Execute_Network_AbsoluteURL()
		{
			var result = state.Configuration as Configuration_Network_AbsoluteURL;
			var results = GetExpressionResults<string>(result.Expression, Selections.Count());
			ReplaceSelections(Selections.Select((range, index) => new Uri(new Uri(results[index]), Text.GetString(range)).AbsoluteUri).ToList());
		}

		void Execute_Network_Fetch_FetchHex(Coder.CodePage codePage = Coder.CodePage.None)
		{
			var urls = GetSelectionStrings();
			var results = Task.Run(() => GetURLs(urls, codePage).Result).Result;
			if (results.Any(result => result.Item3))
				throw new Exception($"Failed to fetch the URLs:\n{string.Join("\n", results.Where(result => result.Item3).Select(result => result.Item1))}");
			ReplaceSelections(results.Select(result => result.Item2).ToList());
		}

		static void Configure_Network_Fetch_File() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Network_Fetch_File(state.NEWindow.Focused.GetVariables());

		void Execute_Network_Fetch_File()
		{
			var result = state.Configuration as Configuration_Network_Fetch_File;
			var variables = GetVariables();

			var urlExpression = state.GetExpression(result.URL);
			var fileNameExpression = state.GetExpression(result.FileName);
			var rowCount = variables.RowCount(urlExpression, fileNameExpression);

			var urls = urlExpression.Evaluate<string>(variables, rowCount);
			var fileNames = fileNameExpression.Evaluate<string>(variables, rowCount);

			const int InvalidCount = 10;
			var invalid = fileNames.Select(name => Path.GetDirectoryName(name)).Distinct().Where(dir => !Directory.Exists(dir)).Take(InvalidCount).ToList();
			if (invalid.Any())
				throw new Exception($"Directories don't exist:\n{string.Join("\n", invalid)}");

			invalid = fileNames.Where(fileName => File.Exists(fileName)).Distinct().Take(InvalidCount).ToList();
			if (invalid.Any())
			{
				if (!QueryUser(nameof(Execute_Network_Fetch_File), $"Are you sure you want to overwrite these files:\n{string.Join("\n", invalid)}", MessageOptions.Yes))
					return;
			}

			TaskRunner.Range(0, urls.Count).ForAll(index => FetchURL(urls[index], fileNames[index]));
		}

		void Execute_Network_Fetch_Custom() => ReplaceSelections(Task.Run(() => MakeNetworkRequests(GetSelectionStrings()).Result).Result);

		static void Configure_Network_Fetch_Stream() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Network_Fetch_StreamPlaylist(state.NEWindow.Focused.GetVariables(), Path.GetDirectoryName(state.NEWindow.Focused.FileName) ?? "");

		void Execute_Network_Fetch_Stream()
		{
			var result = state.Configuration as Configuration_Network_Fetch_StreamPlaylist;
			var urls = GetExpressionResults<string>(result.Expression);
			if (!urls.Any())
				return;

			var now = DateTime.Now;
			var data = urls.Select((url, index) => Tuple.Create(url, now + TimeSpan.FromSeconds(index))).ToList();
			data.AsTaskRunner().ForAll((item, index, progress) => YouTubeDL.DownloadStream(result.OutputDirectory, item.Item1, item.Item2, progress));
		}

		static void Configure_Network_Fetch_Playlist() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Network_Fetch_StreamPlaylist(state.NEWindow.Focused.GetVariables(), null);

		void Execute_Network_Fetch_Playlist()
		{
			var result = state.Configuration as Configuration_Network_Fetch_StreamPlaylist;
			var urls = GetExpressionResults<string>(result.Expression);
			if (!urls.Any())
				return;

			ReplaceSelections(urls.AsTaskRunner().Select(url => string.Join(Text.DefaultEnding, YouTubeDL.GetPlayListItems(url))).ToList());
		}

		void Execute_Network_Lookup_IP() { ReplaceSelections(Task.Run(async () => await Task.WhenAll(GetSelectionStrings().Select(async name => { try { return string.Join(" / ", (await Dns.GetHostEntryAsync(name)).AddressList.Select(address => address.ToString()).Distinct()); } catch { return "<ERROR>"; } }).ToList())).Result.ToList()); }

		void Execute_Network_Lookup_Hostname() { ReplaceSelections(Task.Run(async () => await Task.WhenAll(GetSelectionStrings().Select(async name => { try { return (await Dns.GetHostEntryAsync(name)).HostName; } catch { return "<ERROR>"; } }).ToList())).Result.ToList()); }

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

		static void Configure_Network_Ping() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Network_Ping();

		void Execute_Network_Ping()
		{
			var result = state.Configuration as Configuration_Network_Ping;
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

		static void Configure_Network_ScanPorts() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Network_ScanPorts();

		void Execute_Network_ScanPorts()
		{
			var result = state.Configuration as Configuration_Network_ScanPorts;
			var strs = GetSelectionStrings();
			var results = PortScanner.ScanPorts(strs.Select(str => IPAddress.Parse(str)).ToList(), result.Ports, result.Attempts, TimeSpan.FromMilliseconds(result.Timeout), result.Concurrency);
			ReplaceSelections(strs.Zip(results, (str, strResult) => $"{str}: {string.Join(", ", strResult)}").ToList());
		}

		static void Configure_Network_WCF_GetConfig() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Network_WCF_GetConfig();

		void Execute_Network_WCF_GetConfig()
		{
			var result = state.Configuration as Configuration_Network_WCF_GetConfig;
			if (Selections.Count != 1)
				throw new Exception("Must have single selection.");

			ReplaceSelections(WCFClient.GetWCFConfig(result.URL));
			Settings.AddWCFUrl(result.URL);
		}

		void Execute_Network_WCF_Execute() => ReplaceSelections(Selections.Select(range => WCFClient.ExecuteWCF(Text.GetString(range))).ToList());

		static void Configure_Network_WCF_InterceptCalls() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Network_WCF_InterceptCalls();

		void Execute_Network_WCF_InterceptCalls()
		{
			var result = state.Configuration as Configuration_Network_WCF_InterceptCalls;
			if (Selections.Count != 1)
				throw new Exception("Must have single selection.");

			WCFClient.StartInterceptCalls(result.WCFURL, result.InterceptURL);
			NEWindow.neWindowUI.RunDialog_Execute_Network_WCF_InterceptCalls();
			var values = WCFClient.EndInterceptCalls(result.WCFURL);
			if (!values.Any())
				return;

			var startSel = Selections[0].Start;
			ReplaceSelections(string.Join("", values));
			var sels = new List<NERange>();
			foreach (var value in values)
			{
				var endSel = startSel + value.Length;
				sels.Add(new NERange(startSel, endSel));
				startSel = endSel;
			}
			Selections = sels;
		}

		void Execute_Network_WCF_ResetClients() => WCFClient.ResetClients();
	}
}
