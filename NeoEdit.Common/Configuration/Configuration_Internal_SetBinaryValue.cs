namespace NeoEdit.Common.Configuration
{
	public class Configuration_Internal_SetBinaryValue : IConfiguration
	{
		public byte[] Value { get; set; }
		public int? OldSize { get; set; }
	}
}
