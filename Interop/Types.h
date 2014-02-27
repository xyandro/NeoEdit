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
			System::IntPtr Handle;
			System::String ^Type, ^Name, ^Data;

			HandleInfo(int PID, System::IntPtr Handle, System::String ^Type, System::String ^Name, System::String ^Data);
			System::String ^ToString() override;
		};
	}
}
