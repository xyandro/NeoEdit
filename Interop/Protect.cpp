#include "stdafx.h"
#include "Protect.h"

namespace NeoEdit
{
	namespace Interop
	{
		Protect::Protect(std::shared_ptr<Win32Lib::Protect> _ptr)
		{
			ptr = new std::shared_ptr<Win32Lib::Protect>(_ptr);
		}

		Protect::~Protect()
		{
			delete ptr;
		}
	}
}
