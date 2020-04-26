namespace NeoEdit.Common.Configuration
{
	public class Configuration_Files_Name_MakeAbsolute : IConfiguration
	{
		public enum ResultType
		{
			None,
			File,
			Directory,
		}

		public string Expression { get; set; }
		public ResultType Type { get; set; }
	}
}
