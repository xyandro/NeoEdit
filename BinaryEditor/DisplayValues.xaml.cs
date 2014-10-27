using System.Collections.Generic;
using System.Windows.Controls;
using NeoEdit.BinaryEditor.Dialogs;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Common;

namespace NeoEdit.BinaryEditor
{
	partial class DisplayValues : StackPanel
	{
		[DepProp]
		public BinaryEditor BinaryEditor { get { return UIHelper<DisplayValues>.GetPropValue<BinaryEditor>(this); } set { UIHelper<DisplayValues>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowLE { get { return UIHelper<DisplayValues>.GetPropValue<bool>(this); } set { UIHelper<DisplayValues>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowBE { get { return UIHelper<DisplayValues>.GetPropValue<bool>(this); } set { UIHelper<DisplayValues>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowInt { get { return UIHelper<DisplayValues>.GetPropValue<bool>(this); } set { UIHelper<DisplayValues>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowFloat { get { return UIHelper<DisplayValues>.GetPropValue<bool>(this); } set { UIHelper<DisplayValues>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowStr { get { return UIHelper<DisplayValues>.GetPropValue<bool>(this); } set { UIHelper<DisplayValues>.SetPropValue(this, value); } }

		static DisplayValues() { UIHelper<DisplayValues>.Register(); }

		HashSet<StrCoder.CodePage> codePages = new HashSet<StrCoder.CodePage> { StrCoder.CodePage.UTF8, StrCoder.CodePage.UTF16LE, StrCoder.CodePage.Default };

		public DisplayValues()
		{
			InitializeComponent();
			ShowLE = ShowInt = true;
			ShowBE = ShowFloat = ShowStr = false;
			SetStrings();
		}

		void ChooseEncodings(object sender, System.Windows.RoutedEventArgs e)
		{
			var result = ChooseEncodingsDialog.Run(codePages);
			if (result == null)
				return;
			ShowStr = true;
			codePages = result;
			SetStrings();
		}

		void SetStrings()
		{
			strings.RowDefinitions.Clear();
			strings.Children.Clear();

			foreach (var codePage in codePages)
			{
				var row = strings.RowDefinitions.Count;
				strings.RowDefinitions.Add(new RowDefinition());

				var label = new Label { Content = StrCoder.GetDescription(codePage, true) };
				Grid.SetRow(label, row);
				Grid.SetColumn(label, 0);
				strings.Children.Add(label);

				var displayString = new DisplayString { CodePage = codePage };
				Grid.SetRow(displayString, row);
				Grid.SetColumn(displayString, 1);
				strings.Children.Add(displayString);
			}
		}
	}
}
