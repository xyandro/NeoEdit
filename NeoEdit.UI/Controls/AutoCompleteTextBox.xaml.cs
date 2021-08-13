using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using NeoEdit.Common;

namespace NeoEdit.UI.Controls
{
	partial class AutoCompleteTextBox
	{
		public delegate void OnAcceptSuggestionDelegate(string text, object data);
		public event OnAcceptSuggestionDelegate OnAcceptSuggestion;

		class Suggestion
		{
			public string Text { get; set; }
			public object Data { get; set; }

			public override string ToString() => Text;
		}

		[DepProp(BindsTwoWayByDefault = true)]
		public string Text { get => UIHelper<AutoCompleteTextBox>.GetPropValue<string>(this); set => UIHelper<AutoCompleteTextBox>.SetPropValue(this, value); }
		[DepProp]
		public string CompletionTag { get { return UIHelper<AutoCompleteTextBox>.GetPropValue<string>(this); } set { UIHelper<AutoCompleteTextBox>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsDropDownOpen { get { return UIHelper<AutoCompleteTextBox>.GetPropValue<bool>(this); } set { UIHelper<AutoCompleteTextBox>.SetPropValue(this, value); } }
		[DepProp]
		public bool SuppressNavigation { get { return UIHelper<AutoCompleteTextBox>.GetPropValue<bool>(this); } set { UIHelper<AutoCompleteTextBox>.SetPropValue(this, value); } }
		[DepProp]
		public bool UpcaseTracking { get { return UIHelper<AutoCompleteTextBox>.GetPropValue<bool>(this); } set { UIHelper<AutoCompleteTextBox>.SetPropValue(this, value); } }
		[DepProp]
		public bool HighlightExpressions { get { return UIHelper<AutoCompleteTextBox>.GetPropValue<bool>(this); } set { UIHelper<AutoCompleteTextBox>.SetPropValue(this, value); } }
		[DepProp]
		ObservableCollection<Suggestion> Suggestions { get { return UIHelper<AutoCompleteTextBox>.GetPropValue<ObservableCollection<Suggestion>>(this); } set { UIHelper<AutoCompleteTextBox>.SetPropValue(this, value); } }
		[DepProp]
		int SuggestedIndex { get { return UIHelper<AutoCompleteTextBox>.GetPropValue<int>(this); } set { UIHelper<AutoCompleteTextBox>.SetPropValue(this, value); } }
		[DepProp]
		Suggestion SuggestedValue { get { return UIHelper<AutoCompleteTextBox>.GetPropValue<Suggestion>(this); } set { UIHelper<AutoCompleteTextBox>.SetPropValue(this, value); } }

		static AutoCompleteTextBox()
		{
			UIHelper<AutoCompleteTextBox>.Register();
			UIHelper<AutoCompleteTextBox>.AddCallback(a => a.SuggestedValue, (obj, o, n) => obj.listbox.ScrollIntoView(n));
			UIHelper<AutoCompleteTextBox>.AddCallback(x => x.Text, (obj, o, n) => obj.TextPropertyChangedAction?.Invoke(n));
			UIHelper<AutoCompleteTextBox>.AddCallback(x => x.HighlightExpressions, (obj, o, n) => obj.SetHighlighting());
		}

		Action<string> TextPropertyChangedAction;

		public AutoCompleteTextBox()
		{
			InitializeComponent();
			SuppressNavigation = true;
			Suggestions = new ObservableCollection<Suggestion>();
			TextChanged += (s, e) => SetSuggestions();
			SelectionChanged += (s, e) => SetSuggestions();
			LostKeyboardFocus += (s, e) => IsDropDownOpen = false;
			PreviewKeyDown += HandleKey;
			TextChanged += OnTextChanged;
			TextPropertyChangedAction += SetText;
			AcceptsReturn = false;
		}

		void SetText(string text)
		{
			TextChanged -= OnTextChanged;
			Document.Blocks.Clear();
			Document.Blocks.Add(new Paragraph(new Run(text)));
			SelectAll();
			TextChanged += OnTextChanged;
			SetHighlighting();
		}

		protected override void OnSelectionChanged(RoutedEventArgs e)
		{
			base.OnSelectionChanged(e);
			var block = Document.Blocks.FirstBlock;
			if (block == null)
				return;

			var start = Selection.Start.CompareTo(block.ContentEnd) <= 0 ? Selection.Start : block.ContentEnd;
			var end = Selection.End.CompareTo(block.ContentEnd) <= 0 ? Selection.End : block.ContentEnd;

			if ((start != Selection.Start) || (end != Selection.End))
				Selection.Select(start, end);
		}

		void OnTextChanged(object sender, TextChangedEventArgs e)
		{
			TextPropertyChangedAction -= SetText;
			var block = Document.Blocks.FirstBlock;
			Text = block == null ? "" : new TextRange(block.ContentStart, block.ContentEnd).Text;
			TextPropertyChangedAction += SetText;

			SetHighlighting();
		}

		void SetHighlighting()
		{
			TextChanged -= OnTextChanged;
			var oldSelection = new TextRange(Selection.Start, Selection.End);

			SelectAll();
			Selection.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.White);

			if (HighlightExpressions)
			{
				var ranges = new List<(TextRange, int)>();
				var regex = new Regex(@"\br([1-9])\b");
				foreach (var paragraph in Document.Blocks.OfType<Paragraph>())
				{
					foreach (var inline in paragraph.Inlines)
					{
						var text = new TextRange(inline.ContentStart, inline.ContentEnd).Text;
						foreach (Match match in regex.Matches(text))
						{
							var region = int.Parse(match.Groups[1].Value);
							var start = inline.ContentStart.GetPositionAtOffset(match.Groups[0].Index);
							var end = inline.ContentStart.GetPositionAtOffset(match.Groups[0].Index + match.Groups[0].Length);
							var range = new TextRange(start, end);
							ranges.Add((range, region));
						}
					}
				}

				foreach ((var range, var region) in ranges)
				{
					Selection.Select(range.Start, range.End);
					Selection.ApplyPropertyValue(TextElement.ForegroundProperty, NEFileUI.regionPen[region].Brush);
				}
			}

			Selection.Select(oldSelection.Start, oldSelection.End);
			TextChanged += OnTextChanged;
		}

		ListBox listbox => Template.FindName("PART_ListBox", this) as ListBox;

		static Dictionary<string, List<Suggestion>> SuggestionLists = new Dictionary<string, List<Suggestion>>();
		List<Suggestion> localSuggestionList = new List<Suggestion>();

		static List<Suggestion> GetSuggestionList(string completionTag)
		{
			if (!SuggestionLists.ContainsKey(completionTag))
				SuggestionLists[completionTag] = new List<Suggestion>();
			return SuggestionLists[completionTag];
		}

		List<Suggestion> SuggestionList => string.IsNullOrEmpty(CompletionTag) ? localSuggestionList : GetSuggestionList(CompletionTag);

		public static void AddTagSuggestions(string completionTag, params string[] suggestions) => AddTagSuggestions(GetSuggestionList(completionTag), suggestions.Select(text => new Suggestion { Text = text }).ToArray());

		public static void AddTagSuggestions(string completionTag, params Tuple<string, object>[] suggestions) => AddTagSuggestions(GetSuggestionList(completionTag), suggestions.Select(tuple => new Suggestion { Text = tuple.Item1, Data = tuple.Item2 }).ToArray());

		static void AddTagSuggestions(string completionTag, params Suggestion[] suggestions) => AddTagSuggestions(GetSuggestionList(completionTag), suggestions);

		static void AddTagSuggestions(List<Suggestion> list, params Suggestion[] suggestions)
		{
			suggestions = suggestions.Where(x => !string.IsNullOrEmpty(x.Text)).ToArray();
			var toRemove = new HashSet<string>(suggestions.Select(suggestion => suggestion.Text), StringComparer.OrdinalIgnoreCase);
			list.RemoveAll(suggestion => toRemove.Contains(suggestion.Text));
			list.InsertRange(0, suggestions);
		}

		public void AddSuggestions(params string[] suggestions) => AddTagSuggestions(SuggestionList, suggestions.Select(text => new Suggestion { Text = text }).ToArray());

		public void AddSuggestions(params Tuple<string, object>[] suggestions) => AddTagSuggestions(SuggestionList, suggestions.Select(tuple => new Suggestion { Text = tuple.Item1, Data = tuple.Item2 }).ToArray());

		void AddSuggestions(params Suggestion[] suggestions) => AddTagSuggestions(SuggestionList, suggestions);

		public void AddCurrentSuggestion(object data = null) => AddTagSuggestions(SuggestionList, new Suggestion { Text = Text, Data = data });

		public string GetLastSuggestion() => SuggestionList.FirstOrDefault()?.Text;
		public object GetLastSuggestionData() => SuggestionList.FirstOrDefault()?.Data;

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
				if (SuggestedIndex != suggestedIndex)
					SuggestedIndex = suggestedIndex;
			}
		}

		void AcceptSuggestion(Suggestion suggestion)
		{
			if (suggestion != null)
			{
				Text = suggestion.Text;
				SelectAll();
				OnAcceptSuggestion?.Invoke(suggestion.Text, suggestion.Data);
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

			var find = Selection.Text == Text ? "" : Selection.Text.CoalesceNullOrEmpty(Text) ?? "";
			var ucFind = Regex.Replace(find, "[^A-Z]", "");
			foreach (var suggestion in SuggestionList.Where(x => HasStr(x.Text, find)))
				Suggestions.Add(suggestion);

			if (!Suggestions.Any())
				Suggestions.Add(null);

			SuggestedIndex = find == "" ? 0 : Suggestions.Indexes(str => str?.Text.StartsWith(Text, StringComparison.InvariantCultureIgnoreCase) ?? false).FirstOrDefault();
		}

		void OnSuggestionClick(object sender, MouseButtonEventArgs e) => AcceptSuggestion((sender as ListBoxItem).Content as Suggestion);
	}

	class NullItemConverter : MarkupExtension, IValueConverter
	{
		public override object ProvideValue(IServiceProvider serviceProvider) => this;
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value ?? "<No suggestions>";
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
	}
}
