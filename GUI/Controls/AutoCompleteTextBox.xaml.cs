using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using NeoEdit.Common;

namespace NeoEdit.GUI.Controls
{
	partial class AutoCompleteTextBox
	{
		[DepProp]
		public string CompletionTag { get { return UIHelper<AutoCompleteTextBox>.GetPropValue<string>(this); } set { UIHelper<AutoCompleteTextBox>.SetPropValue(this, value); } }

		[DepProp]
		public bool IsDropDownOpen { get { return UIHelper<AutoCompleteTextBox>.GetPropValue<bool>(this); } set { UIHelper<AutoCompleteTextBox>.SetPropValue(this, value); } }
		[DepProp]
		public bool SuppressNavigation { get { return UIHelper<AutoCompleteTextBox>.GetPropValue<bool>(this); } set { UIHelper<AutoCompleteTextBox>.SetPropValue(this, value); } }
		[DepProp]
		public bool UpcaseTracking { get { return UIHelper<AutoCompleteTextBox>.GetPropValue<bool>(this); } set { UIHelper<AutoCompleteTextBox>.SetPropValue(this, value); } }
		[DepProp]
		ObservableCollection<string> Suggestions { get { return UIHelper<AutoCompleteTextBox>.GetPropValue<ObservableCollection<string>>(this); } set { UIHelper<AutoCompleteTextBox>.SetPropValue(this, value); } }
		[DepProp]
		int SuggestedIndex { get { return UIHelper<AutoCompleteTextBox>.GetPropValue<int>(this); } set { UIHelper<AutoCompleteTextBox>.SetPropValue(this, value); } }
		[DepProp]
		string SuggestedValue { get { return UIHelper<AutoCompleteTextBox>.GetPropValue<string>(this); } set { UIHelper<AutoCompleteTextBox>.SetPropValue(this, value); } }

		static AutoCompleteTextBox()
		{
			UIHelper<AutoCompleteTextBox>.Register();
			UIHelper<AutoCompleteTextBox>.AddCallback(a => a.SuggestedValue, (obj, o, n) => obj.listbox.ScrollIntoView(n));
		}

		public AutoCompleteTextBox()
		{
			InitializeComponent();
			SuppressNavigation = true;
			Suggestions = new ObservableCollection<string>();
			TextChanged += (s, e) => SetSuggestions();
			SelectionChanged += (s, e) => SetSuggestions();
			LostKeyboardFocus += (s, e) => IsDropDownOpen = false;
			PreviewKeyDown += HandleKey;
		}

		ListBox listbox => Template.FindName("PART_ListBox", this) as ListBox;

		static Dictionary<string, List<string>> SuggestionLists = new Dictionary<string, List<string>>();

		List<string> localSuggestionList = new List<string>();
		List<string> SuggestionList
		{
			get
			{
				if (string.IsNullOrEmpty(CompletionTag))
					return localSuggestionList;
				if (!SuggestionLists.ContainsKey(CompletionTag))
					SuggestionLists[CompletionTag] = new List<string>();
				return SuggestionLists[CompletionTag];
			}
		}

		public void AddSuggestions(params string[] suggestions)
		{
			var list = SuggestionList;
			list?.RemoveAll(str => suggestions.Any(suggestion => str.Equals(suggestion, StringComparison.OrdinalIgnoreCase)));
			list?.InsertRange(0, suggestions.Where(val => !string.IsNullOrEmpty(val)));
		}
		public void AddCurrentSuggestion() => AddSuggestions(Text);

		public string GetLastSuggestion() => SuggestionList.FirstOrDefault();

		bool controlDown => Keyboard.Modifiers.HasFlag(ModifierKeys.Control);

		void HandleKey(object sender, KeyEventArgs e)
		{
			if ((e.Key == Key.Down) && (!IsDropDownOpen))
			{
				IsDropDownOpen = true;
				SetSuggestions();
				e.Handled = true;
				return;
			}

			if (IsDropDownOpen)
			{
				var suggestedIndex = SuggestedIndex;
				e.Handled = true;
				switch (e.Key)
				{
					case Key.PageUp:
						if (controlDown)
							suggestedIndex = 0;
						else
							suggestedIndex -= (int)(listbox.ActualHeight / (listbox.ItemContainerGenerator.ContainerFromIndex(SuggestedIndex) as ListBoxItem).ActualHeight) - 2;
						break;
					case Key.PageDown:
						if (controlDown)
							suggestedIndex = int.MaxValue;
						else
							suggestedIndex += (int)(listbox.ActualHeight / (listbox.ItemContainerGenerator.ContainerFromIndex(SuggestedIndex) as ListBoxItem).ActualHeight) - 2;
						break;
					case Key.Down: ++suggestedIndex; break;
					case Key.Up: --suggestedIndex; break;
					case Key.Escape:
						IsDropDownOpen = false;
						if (!SuppressNavigation)
							e.Handled = false;
						break;
					case Key.Enter:
						AcceptSuggestion(SuggestedValue);
						if (!SuppressNavigation)
							e.Handled = false;
						break;
					default: e.Handled = false; break;
				}

				suggestedIndex = Math.Max(0, Math.Min(suggestedIndex, Suggestions.Count - 1));
				if ((e.Handled) && (SuggestedIndex != suggestedIndex))
					SuggestedIndex = suggestedIndex;
			}
		}

		void AcceptSuggestion(string suggestion)
		{
			if (suggestion != null)
			{
				Text = suggestion;
				SelectAll();
			}
			IsDropDownOpen = false;
		}

		bool HasStr(string str, string find)
		{
			if (str.IndexOf(find, StringComparison.InvariantCultureIgnoreCase) != -1)
				return true;
			if ((UpcaseTracking) && (Regex.Replace(str, "[^0-9A-Z_.]", "").IndexOf(find, StringComparison.InvariantCultureIgnoreCase) != -1))
				return true;
			return false;
		}

		void SetSuggestions()
		{
			if (!IsDropDownOpen)
				return;

			Suggestions.Clear();

			var find = SelectedText == Text ? "" : SelectedText.CoalesceNullOrEmpty(Text) ?? "";
			var ucFind = Regex.Replace(find, "[^A-Z]", "");
			foreach (string suggestion in SuggestionList.Where(str => HasStr(str, find)))
				Suggestions.Add(suggestion);

			if (!Suggestions.Any())
				Suggestions.Add(null);

			SuggestedIndex = Suggestions.Indexes(str => str?.StartsWith(Text, StringComparison.InvariantCultureIgnoreCase) == true).FirstOrDefault();
		}

		void OnSuggestionClick(object sender, MouseButtonEventArgs e) => AcceptSuggestion((sender as ListBoxItem).Content as string);
	}

	class NullItemConverter : MarkupExtension, IValueConverter
	{
		static NullItemConverter converter;
		public override object ProvideValue(IServiceProvider serviceProvider) => converter = converter ?? new NullItemConverter();
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value ?? "<No suggestions>";
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }

	}
}
