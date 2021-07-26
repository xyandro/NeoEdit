namespace NeoEdit.Editor.Models
{
	public class NetworkRequest
	{
		public class NetworkRequestRequest
		{
			public string Method { get; set; }
			public string URL { get; set; }

			public class NetworkRequestRequestAuthentication
			{
				public string Scheme { get; set; }
				public string Parameter { get; set; }
			}
			public NetworkRequestRequestAuthentication Authentication { get; set; }

			public class NetworkRequestRequestBody
			{
				public string Content { get; set; }
				public string MediaType { get; set; }
				public string CharSet { get; set; }
			}
			public NetworkRequestRequestBody Body { get; set; }
		}
		public NetworkRequestRequest Request { get; set; }

		public class NetworkRequestResponse
		{
			public bool Success { get; set; }
			public int StatusCode { get; set; }
			public string StatusCodeText { get; set; }
			public string Response { get; set; }
		}
		public NetworkRequestResponse Response { get; set; }
	}
}
