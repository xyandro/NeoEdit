using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using NeoEdit.Common;

namespace NeoEdit.Rip
{
	class YouTube : IDisposable
	{
		HttpClient client = new HttpClient();

		public void Dispose()
		{
			client?.Dispose();
			client = null;
		}

		public static bool IsPlaylist(string uri) => uri.Contains("playlist");

		public static string GetPlaylistID(string uri)
		{
			if (uri.Contains("?"))
				uri = new Uri(uri).Query;
			return HttpUtility.ParseQueryString(uri)["list"] ?? uri;
		}

		public static string GetVideoID(string uri)
		{
			uri = uri
				.Replace("youtu.be/", "youtube.com/watch?v=")
				.Replace("youtube.com/embed/", "youtube.com/watch?v=")
				.Replace("/v/", "/watch?v=")
				.Replace("/watch#", "/watch?")
				.ToString();

			if (uri.Contains("?"))
				uri = new Uri(uri).Query;
			return HttpUtility.ParseQueryString(uri)["v"] ?? uri;
		}

		public async Task<List<string>> GetPlaylistVideoIDs(string playlistID, IProgress<ProgressReport> progress, CancellationToken token)
		{
			playlistID = GetPlaylistID(playlistID);
			var html = await GetString($@"https://www.youtube.com/playlist?list={playlistID}", progress, token);
			return Regex.Matches(html, @"data-video-id=""([-_0-9a-zA-Z]+)""").Cast<Match>().Select(result => result.Groups[1].Value).ToList();
		}

		public async Task<List<YouTubeVideo>> GetVideos(string videoID, IProgress<ProgressReport> progress, CancellationToken token)
		{
			videoID = GetVideoID(videoID);
			var html = await GetString($"https://youtube.com/watch?v={videoID}", progress, token);

			var title = GetTitle(html);
			var jsPlayer = "http:" + GetKey("js", html).Replace(@"\/", "/");

			var result = new List<YouTubeVideo>();
			foreach (var key in new string[] { "url_encoded_fmt_stream_map", "adaptive_fmts" })
			{
				var map = GetKey(key, html);
				if (!string.IsNullOrEmpty(map))
					result.AddRange(map.Split(',').Select(query => GetVideo(title, jsPlayer, query)));
			}

			var dashKey = GetKey("dashmpd", html);
			if (dashKey != null)
			{
				dashKey = WebUtility.UrlDecode(dashKey).Replace(@"\/", "/");
				var manifest = (await client.GetStringAsync(dashKey)).Replace(@"\/", "/").Replace("%2F", "/");
				var uris = GetUrisFromManifest(manifest);
				foreach (var uri in uris)
				{
					var match = Regex.Match(uri, @"itag/(\d+)/");
					var formatCode = match.Success ? int.Parse(match.Groups[1].Value) : -1;
					result.Add(new YouTubeVideo(title, uri, jsPlayer, false, formatCode));
				}
			}

			return result;
		}

		public async Task<YouTubeVideo> GetBestVideo(string videoID, IProgress<ProgressReport> progress, CancellationToken token, HashSet<string> extensions, HashSet<string> resolutions, HashSet<string> audios, HashSet<string> videos, HashSet<bool> is3Ds, HashSet<YouTubeVideo.AdaptiveKindEnum> adaptiveKinds)
		{
			return (await GetVideos(videoID, progress, token))
				.Where(video => extensions.Contains(video.Extension))
				.Where(video => resolutions.Contains(video.Resolution))
				.Where(video => audios.Contains(video.Audio))
				.Where(video => videos.Contains(video.Video))
				.Where(video => is3Ds.Contains(video.Is3D))
				.Where(video => adaptiveKinds.Contains(video.AdaptiveKind))
				.OrderByDescending(video => video.Height)
				.ThenByDescending(video => video.Width)
				.First();
		}

		public async Task Save(YouTubeVideo video, string fileName, IProgress<ProgressReport> progress, CancellationToken token)
		{
			await DecryptURI(video);
			using (var output = File.Create(fileName))
				await SaveURL(video.URI, output, progress, token);
		}

		async Task<string> GetString(string url, IProgress<ProgressReport> progress, CancellationToken token)
		{
			using (var ms = new MemoryStream())
			{
				await SaveURL(url, ms, progress, token);
				ms.Position = 0;
				var sr = new StreamReader(ms);
				return sr.ReadToEnd();
			}
		}

		static string GetTitle(string html)
		{
			var match = Regex.Match(html, "<title>(.*?)</title>");
			return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value) : null;
		}

		static string GetKey(string key, string html)
		{
			var match = Regex.Match(html, $@"""{key}""\s*:\s*""([^""]*?)""");
			return match.Success ? match.Groups[1].Value : null;
		}

		static YouTubeVideo GetVideo(string title, string jsPlayer, string query)
		{
			var queryValues = HttpUtility.ParseQueryString(query.Replace(@"\u0026", "&"));
			var uri = WebUtility.UrlDecode(WebUtility.UrlDecode(queryValues["url"]));
			var uriValues = HttpUtility.ParseQueryString(new Uri(uri).Query);
			var signature = queryValues["s"] ?? queryValues["sig"];
			var encrypted = queryValues["s"] != null;
			if (signature != null)
			{
				uriValues["signature"] = signature;
				if (queryValues["fallback_host"] != null)
					uriValues["fallback_host"] = queryValues["fallback_host"];
			}

			if (uriValues["ratebypass"] == null)
				uriValues["ratebypass"] = "yes";

			uri = new UriBuilder(uri) { Query = uriValues.ToString() }.ToString();

			var formatCode = int.Parse(uriValues["itag"]);
			return new YouTubeVideo(title, jsPlayer, uri, encrypted, formatCode);
		}

		static List<string> GetUrisFromManifest(string source)
		{
			var opening = "<BaseURL>";
			var closing = "</BaseURL>";
			var start = source.IndexOf(opening);
			if (start == -1)
				throw new NotSupportedException();

			var temp = source.Substring(start);
			return temp.Split(new string[] { opening }, StringSplitOptions.RemoveEmptyEntries).Select(v => v.Substring(0, v.IndexOf(closing))).ToList();
		}

		async Task DecryptURI(YouTubeVideo video)
		{
			if (!video.Encrypted)
				return;

			var uri = new Uri(video.URI);
			var uriValues = HttpUtility.ParseQueryString(uri.Query);
			var signature = uriValues["signature"].ToArray();
			var decryptSteps = await GetDecryptSteps(video.JSPlayer);
			foreach (var decryptStep in decryptSteps)
			{
				switch (decryptStep.Item1)
				{
					case DecryptAction.Reverse: signature = signature.Reverse().ToArray(); break;
					case DecryptAction.Splice: signature = signature.Skip(decryptStep.Item2).ToArray(); break;
					case DecryptAction.Swap:
						{
							var tmp = signature[0];
							signature[0] = signature[decryptStep.Item2];
							signature[decryptStep.Item2] = tmp;
						}
						break;
				}
			}
			uriValues["signature"] = new string(signature);
			video.SetDecryptedURI(new UriBuilder(uri) { Query = uriValues.ToString() }.ToString());
		}

		async Task SaveURL(string url, Stream output, IProgress<ProgressReport> progress, CancellationToken token)
		{
			var result = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
			var count = result.Content.Headers.ContentLength;
			var buffer = new byte[16384];
			var offset = 0L;
			using (var input = await result.Content.ReadAsStreamAsync())
			{
				while (true)
				{
					var block = (int)Math.Min(buffer.Length, (count - offset) ?? int.MaxValue);
					if (block != 0)
						block = await input.ReadAsync(buffer, 0, block, token);
					if (block == 0)
						break;
					await output.WriteAsync(buffer, 0, block, token);
					offset += block;
					if (count.HasValue)
						progress.Report(new ProgressReport(offset, count.Value));
				}
			}
		}

		enum DecryptAction
		{
			Reverse,
			Splice,
			Swap,
		}

		Dictionary<string, List<Tuple<DecryptAction, int>>> decryptCache = new Dictionary<string, List<Tuple<DecryptAction, int>>>();
		async Task<List<Tuple<DecryptAction, int>>> GetDecryptSteps(string jSPlayer)
		{
			if (!decryptCache.ContainsKey(jSPlayer))
			{
				var js = await client.GetStringAsync(jSPlayer);
				var match = Regex.Match(js, @".sig\s*\|\|\s*([$_0-9a-zA-Z]+)\(");
				if (!match.Success)
					throw new Exception("Failed to find decryption function");
				var decryptFunc = match.Groups[1].Value;

				var decryptBody = GetJSFunction(js, decryptFunc);
				var start = js.IndexOf('{');
				decryptBody = decryptBody.Substring(start, decryptBody.Length - start - 1);

				var lines = decryptBody.Split(';').ToList();
				lines = lines.Skip(1).Take(lines.Count - 2).ToList();
				var result = new List<Tuple<DecryptAction, int>>();
				var funcActionMap = new Dictionary<string, DecryptAction>();
				foreach (var line in lines)
				{
					match = Regex.Match(line, @"^[$_0-9a-zA-Z]+\.([$_0-9a-zA-Z]+)\([$_0-9a-zA-Z]+(?:,([$_0-9a-zA-Z]+))\)");
					if (!match.Success)
						throw new Exception("Failed to decrypt URL");
					var func = match.Groups[1].Value;
					var param = match.Groups[2].Value;

					if (!funcActionMap.ContainsKey(func))
					{
						var funcBody = GetJSFunction(js, func, true);
						if (funcBody.Contains("reverse"))
							funcActionMap[func] = DecryptAction.Reverse;
						else if (funcBody.Contains("splice"))
							funcActionMap[func] = DecryptAction.Splice;
						else if ((funcBody.Contains("var")) && (funcBody.Contains("c=a")))
							funcActionMap[func] = DecryptAction.Swap;
						else
							throw new Exception("Unable to find action");
					}

					result.Add(Tuple.Create(funcActionMap[func], funcActionMap[func] == DecryptAction.Reverse ? 0 : int.Parse(param)));
				}

				decryptCache[jSPlayer] = result;
			}

			return decryptCache[jSPlayer];
		}

		string GetJSFunction(string js, string func, bool literal = false)
		{
			var regex = literal ? $@"\b{func}:function\(" : $@"\b(?:{func}=function|function {func})\(";
			var match = Regex.Match(js, regex);
			if (!match.Success)
				throw new Exception("Function not found");
			var start = match.Index;
			var depth = 0;
			for (var end = start; end < js.Length; ++end)
				if (js[end] == '{')
					++depth;
				else if (js[end] == '}')
					if (--depth == 0)
						return js.Substring(start, end - start + 1);

			throw new Exception("Function not found");
		}
	}
}
