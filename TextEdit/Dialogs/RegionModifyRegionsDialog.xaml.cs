using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class RegionModifyRegionsDialog
	{
		internal enum Action
		{
			Select,
			Set,
			Clear,
			Remove,
			Replace,
			Unite,
			Intersect,
			Exclude,
		}

		internal class Result
		{
			public List<int> Regions { get; set; }
			public Action Action { get; set; }
		}

		[DepProp]
		public bool Region1 { get { return UIHelper<RegionModifyRegionsDialog>.GetPropValue<bool>(this); } set { UIHelper<RegionModifyRegionsDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Region2 { get { return UIHelper<RegionModifyRegionsDialog>.GetPropValue<bool>(this); } set { UIHelper<RegionModifyRegionsDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Region3 { get { return UIHelper<RegionModifyRegionsDialog>.GetPropValue<bool>(this); } set { UIHelper<RegionModifyRegionsDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Region4 { get { return UIHelper<RegionModifyRegionsDialog>.GetPropValue<bool>(this); } set { UIHelper<RegionModifyRegionsDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Region5 { get { return UIHelper<RegionModifyRegionsDialog>.GetPropValue<bool>(this); } set { UIHelper<RegionModifyRegionsDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Region6 { get { return UIHelper<RegionModifyRegionsDialog>.GetPropValue<bool>(this); } set { UIHelper<RegionModifyRegionsDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Region7 { get { return UIHelper<RegionModifyRegionsDialog>.GetPropValue<bool>(this); } set { UIHelper<RegionModifyRegionsDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Region8 { get { return UIHelper<RegionModifyRegionsDialog>.GetPropValue<bool>(this); } set { UIHelper<RegionModifyRegionsDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Region9 { get { return UIHelper<RegionModifyRegionsDialog>.GetPropValue<bool>(this); } set { UIHelper<RegionModifyRegionsDialog>.SetPropValue(this, value); } }
		[DepProp]

		static RegionModifyRegionsDialog()
		{
			UIHelper<RegionModifyRegionsDialog>.Register();
		}

		RegionModifyRegionsDialog()
		{
			InitializeComponent();
		}

		void SetButtons(object sender, RoutedEventArgs e)
		{
			Region1 = Region2 = Region3 = Region4 = Region5 = Region6 = Region7 = Region8 = Region9 = (bool)(sender as Button).Tag;
		}

		Result result;
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

			result = new Result { Regions = regions, Action = (Action)(sender as Button).Tag };
			DialogResult = true;
		}

		public static Result Run(Window parent)
		{
			var dialog = new RegionModifyRegionsDialog { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
