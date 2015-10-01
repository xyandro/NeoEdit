using System.Collections.Generic;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TableEdit.Dialogs
{
	internal partial class JoinDialog
	{
		internal class Result
		{
			public Table.JoinType Type { get; set; }
		}

		[DepProp]
		public Table.JoinType Type { get { return UIHelper<JoinDialog>.GetPropValue<Table.JoinType>(this); } set { UIHelper<JoinDialog>.SetPropValue(this, value); } }

		static JoinDialog() { UIHelper<JoinDialog>.Register(); }

		JoinDialog()
		{
			InitializeComponent();

			type.ItemsSource = new Dictionary<Table.JoinType, string>
			{
				{ Table.JoinType.Inner, "Inner" },
				{ Table.JoinType.LeftOuter, "Left Outer" },
				{ Table.JoinType.RightOuter, "Right Outer" },
				{ Table.JoinType.FullOuter, "Full Outer" },
			};
			type.SelectedValuePath = "Key";
			type.DisplayMemberPath = "Value";

			Type = Table.JoinType.Inner;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result
			{
				Type = Type,
			};
			DialogResult = true;
		}

		static public Result Run(Window parent)
		{
			var dialog = new JoinDialog() { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
