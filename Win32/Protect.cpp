#include "stdafx.h"
#include "Protect.h"

namespace NeoEdit
{
	namespace Win32
	{
		Protect::Protect(std::shared_ptr<const Win32Lib::Protect> _ptr)
		{
			ptr = new std::shared_ptr<const Win32Lib::Protect>(_ptr);
		}

		Protect::~Protect()
		{
			delete ptr;
		}
	}
}
