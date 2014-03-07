#pragma once

namespace NeoEdit
{
	namespace Win32Interop
	{
		public ref struct HandleInfo
		{
		public:
			property int PID { int get(); }
			property System::IntPtr Handle { System::IntPtr get(); }
			property System::String ^Type { System::String ^get(); }
			property System::String ^Name { System::String ^get(); }
			property System::String ^Data { System::String ^get(); }

			~HandleInfo();
			System::String ^ToString() override;
		internal:
			HandleInfo(std::shared_ptr<Win32Lib::HandleInfo> ptr);
		private:
			std::shared_ptr<Win32Lib::HandleInfo> *ptr;
		};
	}
}
