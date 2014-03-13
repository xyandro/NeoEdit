#pragma once

namespace NeoEdit
{
	namespace Win32
	{
		public ref class Protect
		{
		internal:
			Protect(std::shared_ptr<const Win32Lib::Protect> ptr);
		private:
			~Protect();
			std::shared_ptr<const Win32Lib::Protect> *ptr;
		};
	}
}
