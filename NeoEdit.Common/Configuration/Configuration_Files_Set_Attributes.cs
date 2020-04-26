using System.Collections.Generic;
using System.IO;

namespace NeoEdit.Common.Configuration
{
	public class Configuration_Files_Set_Attributes : IConfiguration
	{
		public Dictionary<FileAttributes, bool?> Attributes { get; set; }
	}
}
