using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEditor.Dialogs
{
	internal partial class LimitDialog
	{
		internal class Response
		{
			public int SelMult { get; set; }
			public bool IgnoreBlank { get; set; }
			public int NumSels { get; set; }
		}

		[DepProp]
		public int SelMult { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool IgnoreBlank { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int NumSels { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int MaxSels { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }

		static LimitDialog() { UIHelper<LimitDialog>.Register(); }

		readonly UIHelper<LimitDialog> uiHelper;
		LimitDialog(int maxSels)
		{
			uiHelper = new UIHelper<LimitDialog>(this);
			InitializeComponent();

			NumSels = MaxSels = maxSels;
			SelMult = 1;
			IgnoreBlank = false;
		}

		Response response = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			response = new Response { SelMult = SelMult, IgnoreBlank = IgnoreBlank, NumSels = NumSels };
			DialogResult = true;
		}

		public static Response Run(int numSels)
		{
			var dialog = new LimitDialog(numSels);
			if (dialog.ShowDialog() != true)
				return null;

			return dialog.response;
		}
	}
}
