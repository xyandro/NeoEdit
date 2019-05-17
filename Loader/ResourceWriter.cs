using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.Loader
{
	class ResourceWriter : IDisposable
	{
		readonly IntPtr handle;

		public ResourceWriter(string file)
		{
			handle = Native.BeginUpdateResource(file, false);
		}

		public void Dispose() => Native.EndUpdateResource(handle, false);

		public void CopyResources(PEInfo info, IEnumerable<IntPtr> types)
		{
			foreach (var type in types)
			{
				try
				{
					var find = $"\\{type}\\";
					foreach (var res in info.ResourceNames.Where(name => name.StartsWith(find)))
					{
						var id = (IntPtr)int.Parse(res.Substring(find.Length));
						var data = info.GetResource(res);
						Native.UpdateResource(handle, type, id, 0, data, data.Length);
					}
				}
				catch { }
			}
		}

		public void AddBinary(int id, byte[] data) => Native.UpdateResource(handle, Native.RT_RCDATA, (IntPtr)id, 0, data, data.Length);
	}
}
