#include "stdafx.h"
#include "Handle.h"

namespace NeoEdit
{
	namespace Interop
	{
		Handle::Handle()
		{
			this->ptr = new std::shared_ptr<void>();
		}

		Handle::Handle(std::shared_ptr<void> ptr)
		{
			this->ptr = new std::shared_ptr<void>(ptr);
		}

		Handle::~Handle()
		{
			delete ptr;
		}

		HANDLE Handle::Get()
		{
			return ptr->get();
		}
	}
}
