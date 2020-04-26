using System.Collections.Generic;
using System.IO;

namespace NeoEdit.Common.Configuration
{
	public class FilesSetAttributesDialogResult : IConfiguration
	{
		public Dictionary<FileAttributes, bool?> Attributes { get; set; }
	}
}
