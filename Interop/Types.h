#pragma once

namespace NeoEdit
{
	namespace Interop
	{
		public ref struct VirtualQueryInfo
		{
			bool Committed, Mapped, NoAccess;
			int Protect;
			System::IntPtr StartAddress, EndAddress, RegionSize;
		};
	}
}
