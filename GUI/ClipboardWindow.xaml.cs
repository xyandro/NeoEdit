using System;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Input;
using NeoEdit.Common;
using NeoEdit.GUI.Common;

namespace NeoEdit.GUI
{
	partial class ClipboardWindow
	{
		static ClipboardWindow current;
		public static new void Show()
		{
			if (current == null)
				current = new ClipboardWindow();
			current.Focus();
		}

		static ClipboardWindow()
		{
			UIHelper<ClipboardWindow>.Register();
			UIHelper<ClipboardWindow>.AddObservableCallback(a => a.Records, (obj, s, e) => obj.items.SelectedItem = NEClipboard.Current);
		}

		[DepProp]
		ObservableCollection<NEClipboard.ClipboardData> Records { get { return UIHelper<ClipboardWindow>.GetPropValue<ObservableCollection<NEClipboard.ClipboardData>>(this); } set { UIHelper<ClipboardWindow>.SetPropValue(this, value); } }

		ClipboardWindow()
		{
			InitializeComponent();

			Records = NEClipboard.History;

			Loaded += (s, e) =>
			{
				var item = items.ItemContainerGenerator.ContainerFromItem(items.SelectedItem) as ListBoxItem;
				if (item != null)
					item.Focus();
			};
			KeyDown += (s, e) =>
			{
				if (e.Key == Key.Escape)
					Close();
			};
			items.MouseDoubleClick += (s, e) => ItemClicked();
			items.PreviewKeyDown += (s, e) =>
			{
				if (e.Key == Key.Enter)
				{
					e.Handled = true;
					ItemClicked();
				}
			};
		}

		void ItemClicked()
		{
			NEClipboard.Current = items.SelectedItem as NEClipboard.ClipboardData;
			Close();
		}

		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			if (current == this)
				current = null;
		}
	}
}
