#pragma once

namespace NeoEdit
{
	namespace Win32Interop
	{
		public ref class VirtualQueryInfo
		{
		public:
			property bool Committed { bool get(); }
			property bool Mapped { bool get(); }
			property bool NoAccess { bool get(); }
			property int Protect { int get(); }
			property System::IntPtr StartAddress { System::IntPtr get(); }
			property System::IntPtr EndAddress { System::IntPtr get(); }
			property System::IntPtr RegionSize { System::IntPtr get(); }

			~VirtualQueryInfo();
		internal:
			VirtualQueryInfo(std::shared_ptr<Win32Lib::VirtualQueryInfo> ptr);
			std::shared_ptr<Win32Lib::VirtualQueryInfo> Get();
		private:
			std::shared_ptr<Win32Lib::VirtualQueryInfo> *ptr;
		};
	}
}
