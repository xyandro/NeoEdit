using System.Collections.Generic;

namespace NeoEdit.Common.Configuration
{
	public class OpenFileDialogResult : IConfiguration
	{
		public List<string> FileNames { get; set; }
	}
}
