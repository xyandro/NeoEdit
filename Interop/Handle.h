#pragma once

#ifdef __cplusplus_cli

namespace NeoEdit
{
	namespace Interop
	{
		public ref class Handle
		{
		public:
			Handle();
			Handle(std::shared_ptr<void> ptr);
			~Handle();
		internal:
			HANDLE Get();
		private:
			std::shared_ptr<void> *ptr;
		};
	}
}

#endif
