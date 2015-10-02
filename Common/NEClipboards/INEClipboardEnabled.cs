namespace NeoEdit.Common.NEClipboards
{
	public interface IClipboardEnabled
	{
		object LocalClipboardData { get; set; }
		bool UseLocalClipboard { get; set; }
	}
}
