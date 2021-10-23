using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Transform;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Text_Find_Find_Dialog
	{
		class CheckBoxStatus
		{
			public bool IsExpression { get; set; }
			public bool AlignSelections { get; set; }
			public bool IsBoolean { get; set; }
			public bool IsRegex { get; set; }
			public bool RegexGroups { get; set; }
			public bool IsBinary { get; set; }
			public bool WholeWords { get; set; }
			public bool MatchCase { get; set; }
			public bool SkipSpace { get; set; }
			public bool SelectionOnly { get; set; }
			public bool EntireSelection { get; set; }
			public bool KeepMatching { get; set; }
			public bool RemoveMatching { get; set; }
		}

		[DepProp]
		public string Text { get { return UIHelper<Text_Find_Find_Dialog>.GetPropValue<string>(this); } set { UIHelper<Text_Find_Find_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsExpression { get { return UIHelper<Text_Find_Find_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Text_Find_Find_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool AlignSelections { get { return UIHelper<Text_Find_Find_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Text_Find_Find_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsBoolean { get { return UIHelper<Text_Find_Find_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Text_Find_Find_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsRegex { get { return UIHelper<Text_Find_Find_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Text_Find_Find_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool RegexGroups { get { return UIHelper<Text_Find_Find_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Text_Find_Find_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsBinary { get { return UIHelper<Text_Find_Find_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Text_Find_Find_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public HashSet<Coder.CodePage> CodePages { get { return UIHelper<Text_Find_Find_Dialog>.GetPropValue<HashSet<Coder.CodePage>>(this); } set { UIHelper<Text_Find_Find_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool WholeWords { get { return UIHelper<Text_Find_Find_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Text_Find_Find_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<Text_Find_Find_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Text_Find_Find_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool SkipSpace { get { return UIHelper<Text_Find_Find_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Text_Find_Find_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool SelectionOnly { get { return UIHelper<Text_Find_Find_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Text_Find_Find_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool EntireSelection { get { return UIHelper<Text_Find_Find_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Text_Find_Find_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool KeepMatching { get { return UIHelper<Text_Find_Find_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Text_Find_Find_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool RemoveMatching { get { return UIHelper<Text_Find_Find_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Text_Find_Find_Dialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static Text_Find_Find_Dialog()
		{
			UIHelper<Text_Find_Find_Dialog>.Register();
			UIHelper<Text_Find_Find_Dialog>.AddCallback(a => a.IsExpression, (obj, o, n) =>
			{
				if (obj.IsExpression)
					obj.SkipSpace = false;
				else
					obj.AlignSelections = false;
			});
			UIHelper<Text_Find_Find_Dialog>.AddCallback(a => a.AlignSelections, (obj, o, n) =>
			{
				if (obj.AlignSelections)
					obj.IsExpression = obj.SelectionOnly = true;
				else
					obj.IsBoolean = false;
			});
			UIHelper<Text_Find_Find_Dialog>.AddCallback(a => a.IsBoolean, (obj, o, n) =>
			{
				if (obj.IsBoolean)
				{
					obj.AlignSelections = true;
					if (!obj.RemoveMatching)
						obj.KeepMatching = true;
					obj.IsBinary = false;
				}
			});
			UIHelper<Text_Find_Find_Dialog>.AddCallback(a => a.IsRegex, (obj, o, n) =>
			{
				if (obj.IsRegex)
					obj.IsBinary = obj.SkipSpace = false;
				else
					obj.RegexGroups = false;
			});
			UIHelper<Text_Find_Find_Dialog>.AddCallback(a => a.RegexGroups, (obj, o, n) =>
			{
				if (obj.RegexGroups)
				{
					obj.IsRegex = true;
					obj.KeepMatching = obj.RemoveMatching = false;
				}
			});
			UIHelper<Text_Find_Find_Dialog>.AddCallback(a => a.IsBinary, (obj, o, n) =>
			{
				if (obj.IsBinary)
					obj.IsBoolean = obj.IsRegex = obj.SkipSpace = false;
			});
			UIHelper<Text_Find_Find_Dialog>.AddCallback(a => a.SkipSpace, (obj, o, n) =>
			{
				if (obj.SkipSpace)
					obj.IsExpression = obj.IsRegex = obj.IsBinary = false;
			});
			UIHelper<Text_Find_Find_Dialog>.AddCallback(a => a.SelectionOnly, (obj, o, n) =>
			{
				if (!obj.SelectionOnly)
					obj.AlignSelections = obj.EntireSelection = obj.KeepMatching = obj.RemoveMatching = false;
			});
			UIHelper<Text_Find_Find_Dialog>.AddCallback(a => a.EntireSelection, (obj, o, n) =>
			{
				if (obj.EntireSelection)
				{
					obj.SelectionOnly = true;
					if (!obj.RemoveMatching)
						obj.KeepMatching = true;
				}
			});
			UIHelper<Text_Find_Find_Dialog>.AddCallback(a => a.KeepMatching, (obj, o, n) =>
			{
				if (obj.KeepMatching)
				{
					obj.SelectionOnly = true;
					obj.RegexGroups = obj.RemoveMatching = false;
				}
				else if (!obj.RemoveMatching)
					obj.IsBoolean = obj.EntireSelection = false;
			});
			UIHelper<Text_Find_Find_Dialog>.AddCallback(a => a.RemoveMatching, (obj, o, n) =>
			{
				if (obj.RemoveMatching)
				{
					obj.SelectionOnly = true;
					obj.RegexGroups = obj.KeepMatching = false;
				}
				else if (!obj.KeepMatching)
					obj.IsBoolean = obj.EntireSelection = false;
			});
		}

		readonly HashSet<Coder.CodePage> startCodePages;

		Text_Find_Find_Dialog(string text, HashSet<Coder.CodePage> codePages, NEVariables variables)
		{
			Variables = variables;
			startCodePages = codePages;

			InitializeComponent();

			Reset();
			Text = text ?? "";
		}

		CheckBoxStatus GetCheckBoxStatus()
		{
			return new CheckBoxStatus
			{
				IsExpression = IsExpression,
				AlignSelections = AlignSelections,
				IsBoolean = IsBoolean,
				IsRegex = IsRegex,
				RegexGroups = RegexGroups,
				IsBinary = IsBinary,
				WholeWords = WholeWords,
				MatchCase = MatchCase,
				SkipSpace = SkipSpace,
				SelectionOnly = SelectionOnly,
				EntireSelection = EntireSelection,
				KeepMatching = KeepMatching,
				RemoveMatching = RemoveMatching,
			};
		}

		void SetCheckBoxStatus(CheckBoxStatus checkBoxStatus)
		{
			if (checkBoxStatus == null)
				return;

			IsExpression = checkBoxStatus.IsExpression;
			AlignSelections = checkBoxStatus.AlignSelections;
			IsBoolean = checkBoxStatus.IsBoolean;
			IsRegex = checkBoxStatus.IsRegex;
			RegexGroups = checkBoxStatus.RegexGroups;
			IsBinary = checkBoxStatus.IsBinary;
			WholeWords = checkBoxStatus.WholeWords;
			MatchCase = checkBoxStatus.MatchCase;
			SkipSpace = checkBoxStatus.SkipSpace;
			SelectionOnly = checkBoxStatus.SelectionOnly;
			EntireSelection = checkBoxStatus.EntireSelection;
			KeepMatching = checkBoxStatus.KeepMatching;
			RemoveMatching = checkBoxStatus.RemoveMatching;
		}

		void OnAcceptSuggestion(string text, object data) => SetCheckBoxStatus(data as CheckBoxStatus);

		void Escape(object sender, RoutedEventArgs e) => Text = Regex.Escape(Text);
		void Unescape(object sender, RoutedEventArgs e) => Text = Regex.Unescape(Text);

		Configuration_Text_Find_Find result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(Text))
				return;

			result = new Configuration_Text_Find_Find
			{
				Text = Text,
				IsExpression = IsExpression,
				AlignSelections = AlignSelections,
				IsBoolean = IsBoolean,
				IsRegex = IsRegex,
				RegexGroups = RegexGroups,
				IsBinary = IsBinary,
				CodePages = IsBinary ? CodePages : null,
				WholeWords = WholeWords,
				MatchCase = MatchCase,
				SkipSpace = SkipSpace,
				SelectionOnly = SelectionOnly,
				EntireSelection = EntireSelection,
				KeepMatching = KeepMatching,
				RemoveMatching = RemoveMatching,
				Type = (Configuration_Text_Find_Find.ResultType)(sender as FrameworkElement).Tag
			};
			text.AddCurrentSuggestion(GetCheckBoxStatus());

			DialogResult = true;
		}

		void OnCodePagesClick(object sender, RoutedEventArgs e)
		{
			try
			{
				CodePages = Window_BinaryCodePages_Dialog.Run(this, CodePages).CodePages;
				IsBinary = true;
			}
			catch { }
		}

		void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		void RegExHelp(object sender, RoutedEventArgs e) => RegExHelpDialog.Display();

		void Reset(object sender = null, RoutedEventArgs e = null)
		{
			IsExpression = AlignSelections = IsBoolean = IsRegex = RegexGroups = IsBinary = WholeWords = MatchCase = SkipSpace = SelectionOnly = EntireSelection = KeepMatching = RemoveMatching = false;
			CodePages = startCodePages;
		}

		public static Configuration_Text_Find_Find Run(Window parent, string text, HashSet<Coder.CodePage> codePages, NEVariables variables)
		{
			var dialog = new Text_Find_Find_Dialog(text, codePages, variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
