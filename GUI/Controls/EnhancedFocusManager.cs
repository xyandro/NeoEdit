using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace NeoEdit.GUI.Controls
{
	public static class EnhancedFocusManager
	{
		private static bool SettingKeyboardFocus { get; set; }

		public static bool GetIsEnhancedFocusScope(DependencyObject element)
		{
			return (bool)element.GetValue(IsEnhancedFocusScopeProperty);
		}

		public static void SetIsEnhancedFocusScope(DependencyObject element, bool value)
		{
			element.SetValue(IsEnhancedFocusScopeProperty, value);
		}

		public static readonly DependencyProperty IsEnhancedFocusScopeProperty = DependencyProperty.RegisterAttached("IsEnhancedFocusScope", typeof(bool), typeof(EnhancedFocusManager), new UIPropertyMetadata(false, OnIsEnhancedFocusScopeChanged));

		private static void OnIsEnhancedFocusScopeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			var item = sender as UIElement;
			if (item == null)
				return;

			if ((bool)e.NewValue)
			{
				FocusManager.SetIsFocusScope(item, true);
				item.GotKeyboardFocus += OnGotKeyboardFocus;
			}
			else
			{
				FocusManager.SetIsFocusScope(item, false);
				item.GotKeyboardFocus -= OnGotKeyboardFocus;
			}
		}

		private static void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			if (SettingKeyboardFocus)
				return;

			var focusedElement = e.NewFocus as Visual;
			for (var d = focusedElement; d != null; d = VisualTreeHelper.GetParent(d) as Visual)
			{
				if (FocusManager.GetIsFocusScope(d))
				{
					SettingKeyboardFocus = true;
					try
					{
						d.SetValue(FocusManager.FocusedElementProperty, focusedElement);
					}
					finally
					{
						SettingKeyboardFocus = false;
					}
					if (!(bool)d.GetValue(IsEnhancedFocusScopeProperty))
						break;
				}
			}
		}
	}
}
