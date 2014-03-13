#include "stdafx.h"
#include "HandleList.h"

using namespace std;

namespace NeoEdit
{
	namespace Win32
	{
		HandleList::HandleList()
		{
			this->ptr = new shared_ptr<const Win32Lib::HandleList>();
		}

		HandleList::HandleList(shared_ptr<const Win32Lib::HandleList> ptr)
		{
			this->ptr = new shared_ptr<const Win32Lib::HandleList>(ptr);
		}

		HandleList::~HandleList()
		{
			delete ptr;
		}

		shared_ptr<const Win32Lib::HandleList> HandleList::Get()
		{
			return *ptr;
		}

		void HandleList::Set(shared_ptr<const Win32Lib::HandleList> value)
		{
			*ptr = value;
		}
	}
}
