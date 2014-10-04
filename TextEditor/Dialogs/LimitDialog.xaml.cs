using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEditor.Dialogs
{
	public partial class LimitDialog : Window
	{
		public class Response
		{
			public int SelMult { get; private set; }
			public bool IgnoreBlank { get; private set; }
			public int NumSels { get; private set; }
			public int MaxSels { get; private set; }

			public Response(int selMult, bool ignoreBlank, int numSels, int maxSels)
			{
				SelMult = selMult;
				IgnoreBlank = ignoreBlank;
				NumSels = numSels;
				MaxSels = maxSels;
			}
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

		void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		public static Response Run(int numSels)
		{
			var dialog = new LimitDialog(numSels);
			if (dialog.ShowDialog() != true)
				return null;

			return new Response(dialog.SelMult, dialog.IgnoreBlank, dialog.NumSels, dialog.MaxSels);
		}
	}
}
