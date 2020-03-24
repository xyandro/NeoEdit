using System.Windows;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Controls
{
	partial class CodePageToggle
	{
		[DepProp]
		public string OnText { get { return UIHelper<CodePageToggle>.GetPropValue<string>(this); } set { UIHelper<CodePageToggle>.SetPropValue(this, value); } }
		[DepProp]
		public string OffText { get { return UIHelper<CodePageToggle>.GetPropValue<string>(this); } set { UIHelper<CodePageToggle>.SetPropValue(this, value); } }

		static CodePageToggle() => UIHelper<CodePageToggle>.Register();

		public bool IsChecked { get => toggleButton.IsChecked == true; set => toggleButton.IsChecked = value; }
		public string Text { set { OnText = OffText = value; } }

		public event RoutedEventHandler Click;

		public CodePageToggle()
		{
			InitializeComponent();
			OnText = "Enabled";
			OffText = "Disabled";
		}

		void OnClick(object sender, RoutedEventArgs e) => Click?.Invoke(this, e);
	}
}
