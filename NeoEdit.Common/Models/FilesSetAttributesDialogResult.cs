using System.Collections.Generic;
using System.IO;

namespace NeoEdit.Common.Models
{
	public class FilesSetAttributesDialogResult
	{
		public Dictionary<FileAttributes, bool?> Attributes { get; set; }
	}
}
