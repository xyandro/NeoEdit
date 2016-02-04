using System;
using System.Drawing;

namespace Loader
{
	class ResourceWriter : IDisposable
	{
		readonly IntPtr handle;

		public ResourceWriter(string file)
		{
			handle = Native.BeginUpdateResource(file, false);
		}

		public void Dispose() => Native.EndUpdateResource(handle, false);

		public void AddIcon(Icon icon)
		{
			var iconInfo = new IconInfo(icon);

			Native.UpdateResource(handle, Native.RT_ICON, (IntPtr)1, 0, iconInfo.IconBytes, iconInfo.IconBytes.Length);

			var groupIcon = new Native.GROUPICON
			{
				ResourceType = 1,
				ImageCount = 1,
				Width = iconInfo.Width,
				Height = iconInfo.Height,
				Planes = iconInfo.Planes,
				BitsPerPixel = iconInfo.BitsPerPixel,
				ImageSize = iconInfo.IconBytes.Length,
				ResourceID = 1,
			};
			Native.UpdateResource(handle, Native.RT_GROUP_ICON, (IntPtr)1, 0, ref groupIcon, Native.GROUPICONSIZE);
		}

		public void AddVersion(byte[] versionData) => Native.UpdateResource(handle, Native.RT_VERSION, (IntPtr)1, 0, versionData, versionData.Length);

		public void AddBinary(int id, byte[] data) => Native.UpdateResource(handle, Native.RT_RCDATA, (IntPtr)id, 0, data, data.Length);
	}
}
