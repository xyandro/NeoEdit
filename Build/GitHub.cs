using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.StaticFiles;
using Newtonsoft.Json;

namespace Build
{
	class GitHub : IDisposable
	{
		const string baseUrl = "https://api.github.com/repos/xyandro/NeoEdit";

		readonly HttpClient client;

		public GitHub()
		{
			client = new HttpClient();
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", App.GitHubToken);
			client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 6.1; WOW64; rv:50.0) Gecko/20100101 Firefox/50.0");
		}

		public void Dispose() => client.Dispose();

		async Task<HttpResponseMessage> SendMessage(HttpRequestMessage message) => await client.SendAsync(message);

		async Task<T> ParseResponse<T>(HttpResponseMessage response) => JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());

		async Task<Dictionary<string, object>> ParseResponseObj(HttpResponseMessage response) => await ParseResponse<Dictionary<string, object>>(response);

		async Task<List<Dictionary<string, object>>> ParseResponseList(HttpResponseMessage response) => await ParseResponse<List<Dictionary<string, object>>>(response);

		public async Task<int?> GetReleaseID(string version)
		{
			using (var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/releases"))
			{
				var response = await SendMessage(request);
				if (response.StatusCode != HttpStatusCode.OK)
					throw new Exception("Failed to find releases.");
				var result = await ParseResponseList(response);
				return result.Where(entry => entry["tag_name"]?.ToString() == version).Select(entry => Convert.ToInt32(entry["id"])).Cast<int?>().FirstOrDefault();
			}
		}

		public async Task DeleteRelease(int id)
		{
			using (var request = new HttpRequestMessage(HttpMethod.Delete, $"{baseUrl}/releases/{id}"))
			{
				var response = await SendMessage(request);
				if (response.StatusCode != HttpStatusCode.NoContent)
					throw new Exception("Failed to delete release.");
			}
		}

		public async Task<string> GetTagID(string version)
		{
			using (var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/tags"))
			{
				var response = await SendMessage(request);
				if (response.StatusCode != HttpStatusCode.OK)
					throw new Exception("Failed to find tags.");
				var result = await ParseResponseList(response);
				return result.Where(entry => entry["name"]?.ToString() == version).Select(entry => (string)entry["name"]).FirstOrDefault();
			}
		}

		public async Task DeleteTag(string tagName)
		{
			using (var request = new HttpRequestMessage(HttpMethod.Delete, $"{baseUrl}/git/refs/tags/{tagName}"))
			{
				var response = await SendMessage(request);
				if (response.StatusCode != HttpStatusCode.NoContent)
					throw new Exception("Failed to delete tag.");
			}
		}

		public async Task<string> CreateRelease(string tagName)
		{
			using (var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/releases"))
			{
				var json = $@"{{ ""tag_name"": ""{tagName}"", ""target_commitish"": ""master"", ""name"": ""NeoEdit {tagName}"", ""draft"": false, ""body"": ""![neoedit](https://cloud.githubusercontent.com/assets/13739632/21296705/19308aea-c52f-11e6-9071-88816d9ceedf.png)\r\n"" }}";
				request.Content = new StringContent(json, Encoding.UTF8, "application/json");

				var response = await SendMessage(request);
				if (response.StatusCode != HttpStatusCode.Created)
					throw new Exception("Failed to create release.");
				var uploadUrl = (await ParseResponseObj(response))["upload_url"].ToString();
				return uploadUrl.Substring(0, uploadUrl.IndexOf('{')) + "?name={0}";
			}
		}

		class ProgressStreamContent : StreamContent
		{
			public delegate void ProgressDelegate(int percentDone);
			public event ProgressDelegate Progress;

			readonly Stream content;
			readonly int bufferSize;
			public ProgressStreamContent(Stream content, int bufferSize) : base(content, bufferSize)
			{
				this.content = content;
				this.bufferSize = bufferSize;
			}

			protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
			{
				long offset = 0;
				var block = new byte[bufferSize];
				var lastPercent = -1;
				while (offset < content.Length)
				{
					var percent = (int)(offset * 100 / content.Length);
					if (lastPercent != percent)
					{
						lastPercent = percent;
						Progress?.Invoke(percent);
					}
					var blockSize = (int)Math.Min(content.Length - offset, block.Length);
					blockSize = await content.ReadAsync(block, 0, blockSize);
					await stream.WriteAsync(block, 0, blockSize);
					offset += blockSize;
				}
			}
		}

		public async Task UploadFile(string uploadUrl, string fileName, Action<int> status)
		{
			using (var request = new HttpRequestMessage(HttpMethod.Post, string.Format(uploadUrl, Path.GetFileName(fileName))))
			using (var file = File.OpenRead(fileName))
			{
				var content = new ProgressStreamContent(file, 1048576);
				content.Progress += percent => status(percent);
				if (!new FileExtensionContentTypeProvider().TryGetContentType(fileName, out var contentType))
					throw new NotImplementedException();
				content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
				request.Content = content;
				var response = await SendMessage(request);
				if (response.StatusCode != HttpStatusCode.Created)
					throw new Exception("Failed to upload.");
			}
		}
	}
}
