using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Expressions;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program.Dialogs
{
	partial class EditFindFindDialog
	{
		class CheckBoxStatus
		{
			public bool IsExpression { get; set; }
			public bool AlignSelections { get; set; }
			public bool IsBoolean { get; set; }
			public bool IsRegex { get; set; }
			public bool RegexGroups { get; set; }
			public bool IsBinary { get; set; }
			public HashSet<Coder.CodePage> CodePages { get; set; }
			public bool WholeWords { get; set; }
			public bool MatchCase { get; set; }
			public bool EntireSelection { get; set; }
			public bool KeepMatching { get; set; }
			public bool RemoveMatching { get; set; }
		}

		public enum ResultType
		{
			None,
			CopyCount,
			FindNext,
			FindAll,
		}

		public class Result
		{
			public string Text { get; set; }
			public bool IsExpression { get; set; }
			public bool AlignSelections { get; set; }
			public bool IsBoolean { get; set; }
			public bool IsRegex { get; set; }
			public bool RegexGroups { get; set; }
			public bool IsBinary { get; set; }
			public HashSet<Coder.CodePage> CodePages { get; set; }
			public bool WholeWords { get; set; }
			public bool MatchCase { get; set; }
			public bool SelectionOnly { get; set; }
			public bool EntireSelection { get; set; }
			public bool KeepMatching { get; set; }
			public bool RemoveMatching { get; set; }
			public ResultType Type { get; set; }
		}

		[DepProp]
		public string Text { get { return UIHelper<EditFindFindDialog>.GetPropValue<string>(this); } set { UIHelper<EditFindFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsExpression { get { return UIHelper<EditFindFindDialog>.GetPropValue<bool>(this); } set { UIHelper<EditFindFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool AlignSelections { get { return UIHelper<EditFindFindDialog>.GetPropValue<bool>(this); } set { UIHelper<EditFindFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsBoolean { get { return UIHelper<EditFindFindDialog>.GetPropValue<bool>(this); } set { UIHelper<EditFindFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsRegex { get { return UIHelper<EditFindFindDialog>.GetPropValue<bool>(this); } set { UIHelper<EditFindFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool RegexGroups { get { return UIHelper<EditFindFindDialog>.GetPropValue<bool>(this); } set { UIHelper<EditFindFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsBinary { get { return UIHelper<EditFindFindDialog>.GetPropValue<bool>(this); } set { UIHelper<EditFindFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public HashSet<Coder.CodePage> CodePages { get { return UIHelper<EditFindFindDialog>.GetPropValue<HashSet<Coder.CodePage>>(this); } set { UIHelper<EditFindFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool WholeWords { get { return UIHelper<EditFindFindDialog>.GetPropValue<bool>(this); } set { UIHelper<EditFindFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<EditFindFindDialog>.GetPropValue<bool>(this); } set { UIHelper<EditFindFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool SelectionOnly { get { return UIHelper<EditFindFindDialog>.GetPropValue<bool>(this); } set { UIHelper<EditFindFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool EntireSelection { get { return UIHelper<EditFindFindDialog>.GetPropValue<bool>(this); } set { UIHelper<EditFindFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool KeepMatching { get { return UIHelper<EditFindFindDialog>.GetPropValue<bool>(this); } set { UIHelper<EditFindFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool RemoveMatching { get { return UIHelper<EditFindFindDialog>.GetPropValue<bool>(this); } set { UIHelper<EditFindFindDialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static EditFindFindDialog()
		{
			UIHelper<EditFindFindDialog>.Register();
			UIHelper<EditFindFindDialog>.AddCallback(a => a.IsExpression, (obj, o, n) =>
			{
				if (!obj.IsExpression)
					obj.AlignSelections = false;
			});
			UIHelper<EditFindFindDialog>.AddCallback(a => a.AlignSelections, (obj, o, n) =>
			{
				if (obj.AlignSelections)
					obj.IsExpression = obj.SelectionOnly = true;
				else
					obj.IsBoolean = false;
			});
			UIHelper<EditFindFindDialog>.AddCallback(a => a.IsBoolean, (obj, o, n) =>
			{
				if (obj.IsBoolean)
				{
					obj.AlignSelections = true;
					if (!obj.RemoveMatching)
						obj.KeepMatching = true;
					obj.IsBinary = false;
				}
			});
			UIHelper<EditFindFindDialog>.AddCallback(a => a.IsRegex, (obj, o, n) =>
			{
				if (obj.IsRegex)
					obj.IsBinary = false;
				else
					obj.RegexGroups = false;
			});
			UIHelper<EditFindFindDialog>.AddCallback(a => a.RegexGroups, (obj, o, n) =>
			{
				if (obj.RegexGroups)
				{
					obj.IsRegex = true;
					obj.KeepMatching = obj.RemoveMatching = false;
				}
			});
			UIHelper<EditFindFindDialog>.AddCallback(a => a.IsBinary, (obj, o, n) =>
			{
				if (obj.IsBinary)
					obj.IsRegex = obj.IsBoolean = false;
			});
			UIHelper<EditFindFindDialog>.AddCallback(a => a.SelectionOnly, (obj, o, n) =>
			{
				if (!obj.SelectionOnly)
					obj.AlignSelections = obj.EntireSelection = obj.KeepMatching = obj.RemoveMatching = false;
			});
			UIHelper<EditFindFindDialog>.AddCallback(a => a.EntireSelection, (obj, o, n) =>
			{
				if (obj.EntireSelection)
				{
					obj.SelectionOnly = true;
					if (!obj.RemoveMatching)
						obj.KeepMatching = true;
				}
			});
			UIHelper<EditFindFindDialog>.AddCallback(a => a.KeepMatching, (obj, o, n) =>
			{
				if (obj.KeepMatching)
				{
					obj.SelectionOnly = true;
					obj.RemoveMatching = obj.RegexGroups = false;
				}
				else if (!obj.RemoveMatching)
					obj.IsBoolean = obj.EntireSelection = false;
			});
			UIHelper<EditFindFindDialog>.AddCallback(a => a.RemoveMatching, (obj, o, n) =>
			{
				if (obj.RemoveMatching)
				{
					obj.SelectionOnly = true;
					obj.KeepMatching = obj.RegexGroups = false;
				}
				else if (!obj.KeepMatching)
					obj.IsBoolean = obj.EntireSelection = false;
			});
		}


		EditFindFindDialog(string text, bool selectionOnly, NEVariables variables)
		{
			Variables = variables;

			InitializeComponent();

			Reset();
			SelectionOnly = selectionOnly;
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
				CodePages = CodePages,
				WholeWords = WholeWords,
				MatchCase = MatchCase,
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
			CodePages = checkBoxStatus.CodePages;
			WholeWords = checkBoxStatus.WholeWords;
			MatchCase = checkBoxStatus.MatchCase;
			EntireSelection = checkBoxStatus.EntireSelection;
			KeepMatching = checkBoxStatus.KeepMatching;
			RemoveMatching = checkBoxStatus.RemoveMatching;
		}

		void OnAcceptSuggestion(string text, object data) => SetCheckBoxStatus(data as CheckBoxStatus);

		void Escape(object sender, RoutedEventArgs e) => Text = Regex.Escape(Text);
		void Unescape(object sender, RoutedEventArgs e) => Text = Regex.Unescape(Text);

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(Text))
				return;


			result = new Result { Text = Text, IsExpression = IsExpression, AlignSelections = AlignSelections, IsBoolean = IsBoolean, IsRegex = IsRegex, RegexGroups = RegexGroups, IsBinary = IsBinary, CodePages = IsBinary ? CodePages : null, WholeWords = WholeWords, MatchCase = MatchCase, SelectionOnly = SelectionOnly, EntireSelection = EntireSelection, KeepMatching = KeepMatching, RemoveMatching = RemoveMatching, Type = (ResultType)(sender as FrameworkElement).Tag };
			text.AddCurrentSuggestion(GetCheckBoxStatus());

			DialogResult = true;
		}

		void OnCodePagesClick(object sender, RoutedEventArgs e)
		{
			var codePages = CodePagesDialog.Run(this, CodePages);
			if (codePages == null)
				return;

			IsBinary = true;
			CodePages = codePages;
		}

		void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		void RegExHelp(object sender, RoutedEventArgs e) => RegExHelpDialog.Display();

		void Reset(object sender = null, RoutedEventArgs e = null)
		{
			IsExpression = AlignSelections = IsBoolean = IsRegex = RegexGroups = IsBinary = WholeWords = MatchCase = SelectionOnly = EntireSelection = KeepMatching = RemoveMatching = false;
			CodePages = new HashSet<Coder.CodePage>(CodePagesDialog.DefaultCodePages);
		}

		static public Result Run(Window parent, string text, bool selectionOnly, NEVariables variables)
		{
			var dialog = new EditFindFindDialog(text, selectionOnly, variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
