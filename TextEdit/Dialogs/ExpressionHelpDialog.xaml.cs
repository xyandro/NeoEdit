using System.Collections.Generic;
using System.Windows;

namespace NeoEdit.TextEdit.Dialogs
{
	class HelpItems
	{
		public string Name { get; set; }
		public List<HelpItem> Items { get; set; }
		public int Columns { get; set; }
		public int NameWidth { get; set; }
		public int DescWidth { get; set; }
		public HelpItems() { Items = new List<HelpItem>(); }
	}

	class HelpItem
	{
		public string Name { get; set; }
		public string Description { get; set; }
	}

	partial class ExpressionHelpDialog
	{
		static ExpressionHelpDialog singleton = null;

		ExpressionHelpDialog()
		{
			InitializeComponent();
		}

		protected override void OnClosed(System.EventArgs e)
		{
			singleton = null;
			base.OnClosed(e);
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			Close();
		}

		public static void Display()
		{
			if (singleton == null)
				singleton = new ExpressionHelpDialog();
			singleton.Show();
			singleton.Focus();
		}
	}
}
