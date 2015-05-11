using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.HexEdit
{
	partial class DisplayValues
	{
		[DepProp]
		public HexEditor HexEditor { get { return UIHelper<DisplayValues>.GetPropValue<HexEditor>(this); } set { UIHelper<DisplayValues>.SetPropValue(this, value); } }
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

		HashSet<Coder.CodePage> codePages = new HashSet<Coder.CodePage> { Coder.CodePage.Default, Coder.CodePage.UTF8, Coder.CodePage.UTF16LE };

		public DisplayValues()
		{
			InitializeComponent();
			ShowLE = ShowInt = true;
			ShowBE = ShowFloat = ShowStr = false;
			SetStrings();
		}

		void ChooseEncodings(object sender, System.Windows.RoutedEventArgs e)
		{
			var result = EncodingsDialog.Run(UIHelper.FindParent<Window>(this), codePages);
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

				var label = new Label { Content = Coder.GetDescription(codePage, true) };
				Grid.SetRow(label, row);
				Grid.SetColumn(label, 0);
				strings.Children.Add(label);

				var displayString = new DisplayValue { CodePage = codePage, HorizontalAlignment = HorizontalAlignment.Left, IsReadOnly = true };
				Grid.SetRow(displayString, row);
				Grid.SetColumn(displayString, 1);
				strings.Children.Add(displayString);
			}
		}
	}
}
