using System.Collections.Generic;
using NeoEdit.Common.Transform;

namespace NeoEdit.Common.Configuration
{
	public class Configuration_Window_BinaryCodePages : IConfiguration
	{
		public HashSet<Coder.CodePage> CodePages { get; set; }
	}
}
