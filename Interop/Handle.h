#pragma once

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
			std::shared_ptr<void> Get();
		private:
			std::shared_ptr<void> *ptr;
		};
	}
}
