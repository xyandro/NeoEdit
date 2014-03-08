#pragma once

namespace NeoEdit
{
	namespace Win32
	{
		public ref class Handle
		{
		internal:
			Handle();
			Handle(std::shared_ptr<void> ptr);
			std::shared_ptr<void> Get();
		private:
			~Handle();
			std::shared_ptr<void> *ptr;
		};
	}
}
