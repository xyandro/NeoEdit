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
	partial class Files_Select_ByContent_Dialog
	{
		class CheckBoxStatus
		{
			public bool IsExpression { get; set; }
			public bool AlignSelections { get; set; }
			public bool IsRegex { get; set; }
			public bool IsBinary { get; set; }
			public HashSet<Coder.CodePage> CodePages { get; set; }
			public bool MatchCase { get; set; }
			public bool SkipSpace { get; set; }
		}

		[DepProp]
		public string Text { get { return UIHelper<Files_Select_ByContent_Dialog>.GetPropValue<string>(this); } set { UIHelper<Files_Select_ByContent_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsExpression { get { return UIHelper<Files_Select_ByContent_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Files_Select_ByContent_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool AlignSelections { get { return UIHelper<Files_Select_ByContent_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Files_Select_ByContent_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsRegex { get { return UIHelper<Files_Select_ByContent_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Files_Select_ByContent_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsBinary { get { return UIHelper<Files_Select_ByContent_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Files_Select_ByContent_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public HashSet<Coder.CodePage> CodePages { get { return UIHelper<Files_Select_ByContent_Dialog>.GetPropValue<HashSet<Coder.CodePage>>(this); } set { UIHelper<Files_Select_ByContent_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<Files_Select_ByContent_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Files_Select_ByContent_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool SkipSpace { get { return UIHelper<Files_Select_ByContent_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Files_Select_ByContent_Dialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static Files_Select_ByContent_Dialog()
		{
			UIHelper<Files_Select_ByContent_Dialog>.Register();
			UIHelper<Files_Select_ByContent_Dialog>.AddCallback(a => a.IsExpression, (obj, o, n) =>
			{
				if (obj.IsExpression)
					obj.SkipSpace = false;
				else
					obj.AlignSelections = false;
			});
			UIHelper<Files_Select_ByContent_Dialog>.AddCallback(a => a.AlignSelections, (obj, o, n) =>
			{
				if (obj.AlignSelections)
					obj.IsExpression = true;
			});
			UIHelper<Files_Select_ByContent_Dialog>.AddCallback(a => a.IsRegex, (obj, o, n) =>
			{
				if (obj.IsRegex)
					obj.IsBinary = obj.SkipSpace = false;
			});
			UIHelper<Files_Select_ByContent_Dialog>.AddCallback(a => a.IsBinary, (obj, o, n) =>
			{
				if (obj.IsBinary)
					obj.IsRegex = obj.SkipSpace = false;
			});
			UIHelper<Files_Select_ByContent_Dialog>.AddCallback(a => a.SkipSpace, (obj, o, n) =>
			{
				if (obj.SkipSpace)
					obj.IsExpression = obj.IsRegex = obj.IsBinary = false;
			});
		}

		Files_Select_ByContent_Dialog(NEVariables variables)
		{
			Variables = variables;

			InitializeComponent();

			Reset();
			Text = text.GetLastSuggestion() ?? "";
			SetCheckBoxStatus(text.GetLastSuggestionData() as CheckBoxStatus);
		}

		CheckBoxStatus GetCheckBoxStatus()
		{
			return new CheckBoxStatus
			{
				IsExpression = IsExpression,
				AlignSelections = AlignSelections,
				IsRegex = IsRegex,
				IsBinary = IsBinary,
				CodePages = CodePages,
				MatchCase = MatchCase,
				SkipSpace = SkipSpace,
			};
		}

		void SetCheckBoxStatus(CheckBoxStatus checkBoxStatus)
		{
			if (checkBoxStatus == null)
				return;

			IsExpression = checkBoxStatus.IsExpression;
			AlignSelections = checkBoxStatus.AlignSelections;
			IsRegex = checkBoxStatus.IsRegex;
			IsBinary = checkBoxStatus.IsBinary;
			CodePages = checkBoxStatus.CodePages;
			MatchCase = checkBoxStatus.MatchCase;
			SkipSpace = checkBoxStatus.SkipSpace;
		}

		void OnAcceptSuggestion(string text, object data) => SetCheckBoxStatus(data as CheckBoxStatus);

		void Escape(object sender, RoutedEventArgs e) => Text = Regex.Escape(Text);
		void Unescape(object sender, RoutedEventArgs e) => Text = Regex.Unescape(Text);

		Configuration_Files_Select_ByContent result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(Text))
				return;

			result = new Configuration_Files_Select_ByContent
			{
				Text = Text,
				IsExpression = IsExpression,
				AlignSelections = AlignSelections,
				IsRegex = IsRegex,
				IsBinary = IsBinary,
				CodePages = IsBinary ? CodePages : null,
				MatchCase = MatchCase,
				SkipSpace = SkipSpace,
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
			IsExpression = AlignSelections = IsRegex = MatchCase = SkipSpace = false;
			IsBinary = true;
			CodePages = new HashSet<Coder.CodePage>(Coder.DefaultCodePages);
		}

		public static Configuration_Files_Select_ByContent Run(Window parent, NEVariables variables)
		{
			var dialog = new Files_Select_ByContent_Dialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
