#pragma once

#ifdef __cplusplus_cli

#include "Types.h"
#include "Handle.h"

namespace NeoEdit
{
	namespace Interop
	{
		public ref class Protect
		{
		public:
			Protect(Handle ^handle, VirtualQueryInfo ^info, int protect);
			~Protect();
		private:
			Handle ^handle;
			VirtualQueryInfo ^info;
			int protect;
		};
	}
}

#endif
