using System.Collections.Generic;
using NeoEdit.Common.Transform;

namespace NeoEdit.Common.Configuration
{
	public class Configuration_Window_ViewBinaryCodePages : IConfiguration
	{
		public HashSet<Coder.CodePage> CodePages { get; set; }
	}
}
