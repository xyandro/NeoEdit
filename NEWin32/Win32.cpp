#include "stdafx.h"

using namespace std;
using namespace NeoEdit;

void wmain(int argc, wchar_t *argv[])
{
	try
	{
		vector<wstring> params;
		for (auto ctr = 1; ctr < argc; ++ctr)
			params.push_back(argv[ctr]);

		if (params.size() == 0)
			params.push_back(L"-handleinfo");

		if (params[0] == L"-handleinfo")
		{
			auto handles = Win32Lib::GetAllHandles();
			params.erase(params.begin());
			while (params.size() != 0)
			{
				if ((params[0] == L"-pid") && (params.size() >= 2))
				{
					handles = Win32Lib::GetProcessHandles(handles, stoul(params[1]));
					params.erase(params.begin(), params.begin() + 2);
				}
				else if ((params[0] == L"-type") && (params.size() >= 2))
				{
					handles = Win32Lib::GetTypeHandles(handles, params[1]);
					params.erase(params.begin(), params.begin() + 2);
				}
				else 
					throw L"Invalid arguments";
			}

			auto handleInfo = Win32Lib::GetHandleInfo(handles);
			for each (auto handle in *handleInfo)
				printf("PID: %i, Handle: %i, Type: %S, Name: %S, Data %S\n", handle->PID, handle->Handle, handle->Type.c_str(), handle->Name.c_str(), handle->Data.c_str());
		}
		else if (params[0] == L"-types")
		{
			auto types = Win32Lib::GetHandleTypes();
			for (decltype(types->size()) ctr = 0; ctr < types->size(); ++ctr)
				printf("%i: %S\n", ctr, (*types)[ctr].c_str());
		}
		else
			throw L"Invalid arguments";
	}
	catch (Win32Lib::Win32Exception &e)
	{
		printf("Error: %S.\n", e.Message());
	}
	catch (...)
	{
		printf("Error occured.\n");
	}
}
