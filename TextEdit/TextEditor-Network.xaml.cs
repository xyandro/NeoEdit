using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using NeoEdit.Common;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Dialogs;
using NeoEdit.TextEdit.Dialogs;

namespace NeoEdit.TextEdit
{
	partial class TextEditor
	{
		async Task<string> GetURL(string url, Coder.CodePage codePage = Coder.CodePage.None)
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

		void Command_Network_Fetch(Coder.CodePage codePage = Coder.CodePage.None)
		{
			var urls = GetSelectionStrings();
			var results = Task.Run(() => GetURLs(urls, codePage).Result).Result;
			if (results.Any(result => result.Item3))
				new Message(WindowParent)
				{
					Title = "Error",
					Text = $"Failed to fetch the URLs:\n{string.Join("\n", results.Where(result => result.Item3).Select(result => result.Item1))}",
					Options = Message.OptionsEnum.Ok,
				}.Show();
			ReplaceSelections(results.Select(result => result.Item2).ToList());
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

		PingDialog.Result Command_Network_Ping_Dialog() => PingDialog.Run(WindowParent);

		void Command_Network_Ping(PingDialog.Result result)
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

		ScanPortsDialog.Result Command_Network_ScanPorts_Dialog() => ScanPortsDialog.Run(WindowParent);

		void Command_Network_ScanPorts(ScanPortsDialog.Result result)
		{
			var strs = GetSelectionStrings();
			var results = PortScanner.ScanPorts(strs.Select(str => IPAddress.Parse(str)).ToList(), result.Ports, result.Attempts, TimeSpan.FromMilliseconds(result.Timeout), result.Concurrency);
			ReplaceSelections(strs.Zip(results, (str, strResult) => $"{str}: {string.Join(", ", strResult)}").ToList());
		}
	}
}
