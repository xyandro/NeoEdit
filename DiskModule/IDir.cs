using System.Collections.Generic;

namespace NeoEdit.DiskModule
{
	public interface IDir
	{
		List<string> Files { get; }
		string Name { get; }
	}
}
