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

		public ref struct HandleInfo
		{
			int PID;
			System::String ^Type, ^Name;

			HandleInfo(int PID, System::String ^Type, System::String ^Name);
			System::String ^ToString() override;
		};
	}
}
