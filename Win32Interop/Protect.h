#pragma once

namespace NeoEdit
{
	namespace Win32Interop
	{
		public ref class Protect
		{
		internal:
			Protect(std::shared_ptr<Win32Lib::Protect> ptr);
		private:
			~Protect();
			std::shared_ptr<Win32Lib::Protect> *ptr;
		};
	}
}
