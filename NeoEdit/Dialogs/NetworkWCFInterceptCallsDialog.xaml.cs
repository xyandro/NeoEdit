using System.Windows;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Dialogs
{
	partial class NetworkWCFInterceptCallsDialog
	{
		public class Result
		{
			public string WCFURL { get; set; }
			public string InterceptURL { get; set; }
		}

		[DepProp]
		public string WCFURL { get { return UIHelper<NetworkWCFInterceptCallsDialog>.GetPropValue<string>(this); } set { UIHelper<NetworkWCFInterceptCallsDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string InterceptURL { get { return UIHelper<NetworkWCFInterceptCallsDialog>.GetPropValue<string>(this); } set { UIHelper<NetworkWCFInterceptCallsDialog>.SetPropValue(this, value); } }

		static NetworkWCFInterceptCallsDialog() { UIHelper<NetworkWCFInterceptCallsDialog>.Register(); }

		NetworkWCFInterceptCallsDialog()
		{
			InitializeComponent();
			WCFURL = wcfURL.GetLastSuggestion();
			InterceptURL = interceptURL.GetLastSuggestion();
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { WCFURL = WCFURL, InterceptURL = InterceptURL };
			DialogResult = true;
			wcfURL.AddCurrentSuggestion();
			interceptURL.AddCurrentSuggestion();
		}

		static public Result Run(Window parent)
		{
			var dialog = new NetworkWCFInterceptCallsDialog() { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
