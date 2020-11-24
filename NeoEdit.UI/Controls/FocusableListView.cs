using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NeoEdit.Common;

namespace NeoEdit.UI.Controls
{
	class FocusableListView : ListView
	{
		public event EventHandler FocusedChanged;

		public FocusableListView()
		{
			Style = new Style { TargetType = typeof(ListView), BasedOn = (Style)FindResource(typeof(ListView)) };
		}

		public object Focused { get; private set; }

		public void SetFocus(object focused)
		{
			Focused = focused;
			ScrollIntoView(Focused);
			if (ItemContainerGenerator.ContainerFromItem(Focused) is ListBoxItem listBoxItem)
			{
				var prevSelectedItems = SelectedItems.Cast<object>().ToList();
				listBoxItem.Focus();
				SelectedItems.Clear();
				prevSelectedItems.ForEach(SelectedItems.Add);
			}
			FocusedChanged?.Invoke(this, new EventArgs());
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			CheckFocusUpdated();
		}

		object FindFocused()
		{
			foreach (var item in Items)
				if ((ItemContainerGenerator.ContainerFromItem(item) is UIElement uiElement) && (uiElement.IsFocused))
					return item;
			return null;
		}

		void CheckFocusUpdated()
		{
			var nextFocused = FindFocused();
			if (nextFocused == Focused)
				return;

			Focused = nextFocused;
			FocusedChanged?.Invoke(this, new EventArgs());
		}
	}
}
