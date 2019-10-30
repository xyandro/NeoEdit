using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Dialogs
{
	partial class WCFInterceptDialog
	{
		[DepProp]
		int CallCount { get { return UIHelper<WCFInterceptDialog>.GetPropValue<int>(this); } set { UIHelper<WCFInterceptDialog>.SetPropValue(this, value); } }
		[DepProp]
		string LastCall { get { return UIHelper<WCFInterceptDialog>.GetPropValue<string>(this); } set { UIHelper<WCFInterceptDialog>.SetPropValue(this, value); } }

		static WCFInterceptDialog() => UIHelper<WCFInterceptDialog>.Register();

		public WCFInterceptDialog()
		{
			InitializeComponent();
			CallCount = 0;
			LastCall = "None";
		}

		public void AddCall(string call)
		{
			Dispatcher.Invoke(() =>
			{
				++CallCount;
				LastCall = call;
			});
		}
	}
}
