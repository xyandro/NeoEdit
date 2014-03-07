#include "stdafx.h"
#include "Handle.h"

using namespace std;

namespace NeoEdit
{
	namespace Win32Interop
	{
		Handle::Handle()
		{
			this->ptr = new shared_ptr<void>();
		}

		Handle::Handle(shared_ptr<void> ptr)
		{
			this->ptr = new shared_ptr<void>(ptr);
		}

		Handle::~Handle()
		{
			delete ptr;
		}

		shared_ptr<void> Handle::Get()
		{
			return *ptr;
		}
	}
}
