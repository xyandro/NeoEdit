#pragma once

#include "../Win32Lib/Win32Lib.h"

namespace NeoEdit
{
	namespace Win32
	{
		public ref class HandleList
		{
		internal:
			HandleList();
			HandleList(std::shared_ptr<const Win32Lib::HandleList> ptr);
			std::shared_ptr<const Win32Lib::HandleList> Get();
			void Set(std::shared_ptr<const Win32Lib::HandleList>);
		private:
			~HandleList();
			std::shared_ptr<const Win32Lib::HandleList> *ptr;
		};
	}
}
