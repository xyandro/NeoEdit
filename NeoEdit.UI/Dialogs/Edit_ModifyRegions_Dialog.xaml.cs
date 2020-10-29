using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.Common.Configuration;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Edit_ModifyRegions_Dialog
	{
		[DepProp]
		public bool Region1 { get { return UIHelper<Edit_ModifyRegions_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Edit_ModifyRegions_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Region2 { get { return UIHelper<Edit_ModifyRegions_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Edit_ModifyRegions_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Region3 { get { return UIHelper<Edit_ModifyRegions_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Edit_ModifyRegions_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Region4 { get { return UIHelper<Edit_ModifyRegions_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Edit_ModifyRegions_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Region5 { get { return UIHelper<Edit_ModifyRegions_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Edit_ModifyRegions_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Region6 { get { return UIHelper<Edit_ModifyRegions_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Edit_ModifyRegions_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Region7 { get { return UIHelper<Edit_ModifyRegions_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Edit_ModifyRegions_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Region8 { get { return UIHelper<Edit_ModifyRegions_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Edit_ModifyRegions_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Region9 { get { return UIHelper<Edit_ModifyRegions_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Edit_ModifyRegions_Dialog>.SetPropValue(this, value); } }
		[DepProp]

		static Edit_ModifyRegions_Dialog()
		{
			UIHelper<Edit_ModifyRegions_Dialog>.Register();
		}

		Edit_ModifyRegions_Dialog()
		{
			InitializeComponent();
		}

		void SetButtons(object sender, RoutedEventArgs e) => Region1 = Region2 = Region3 = Region4 = Region5 = Region6 = Region7 = Region8 = Region9 = true;

		void Reset(object sender, RoutedEventArgs e) => Region1 = Region2 = Region3 = Region4 = Region5 = Region6 = Region7 = Region8 = Region9 = false;

		Configuration_Edit_ModifyRegions result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			var regions = new List<int>();
			if (Region1)
				regions.Add(1);
			if (Region2)
				regions.Add(2);
			if (Region3)
				regions.Add(3);
			if (Region4)
				regions.Add(4);
			if (Region5)
				regions.Add(5);
			if (Region6)
				regions.Add(6);
			if (Region7)
				regions.Add(7);
			if (Region8)
				regions.Add(8);
			if (Region9)
				regions.Add(9);
			if (!regions.Any())
				return;

			result = new Configuration_Edit_ModifyRegions { Regions = regions, Action = (Configuration_Edit_ModifyRegions.Actions)(sender as Button).Tag };
			DialogResult = true;
		}

		public static Configuration_Edit_ModifyRegions Run(Window parent)
		{
			var dialog = new Edit_ModifyRegions_Dialog { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
