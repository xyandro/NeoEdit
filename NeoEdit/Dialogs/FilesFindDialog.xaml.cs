using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Expressions;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program.Dialogs
{
	partial class FilesFindDialog
	{
		class CheckBoxStatus
		{
			public bool IsExpression { get; set; }
			public bool AlignSelections { get; set; }
			public bool IsRegex { get; set; }
			public bool IsBinary { get; set; }
			public HashSet<Coder.CodePage> CodePages { get; set; }
			public bool MatchCase { get; set; }
		}

		public class Result
		{
			public string Text { get; set; }
			public bool IsExpression { get; set; }
			public bool AlignSelections { get; set; }
			public bool IsRegex { get; set; }
			public bool IsBinary { get; set; }
			public HashSet<Coder.CodePage> CodePages { get; set; }
			public bool MatchCase { get; set; }
		}

		[DepProp]
		public string Text { get { return UIHelper<FilesFindDialog>.GetPropValue<string>(this); } set { UIHelper<FilesFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsExpression { get { return UIHelper<FilesFindDialog>.GetPropValue<bool>(this); } set { UIHelper<FilesFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool AlignSelections { get { return UIHelper<FilesFindDialog>.GetPropValue<bool>(this); } set { UIHelper<FilesFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsRegex { get { return UIHelper<FilesFindDialog>.GetPropValue<bool>(this); } set { UIHelper<FilesFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsBinary { get { return UIHelper<FilesFindDialog>.GetPropValue<bool>(this); } set { UIHelper<FilesFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public HashSet<Coder.CodePage> CodePages { get { return UIHelper<FilesFindDialog>.GetPropValue<HashSet<Coder.CodePage>>(this); } set { UIHelper<FilesFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<FilesFindDialog>.GetPropValue<bool>(this); } set { UIHelper<FilesFindDialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static FilesFindDialog()
		{
			UIHelper<FilesFindDialog>.Register();
			UIHelper<FilesFindDialog>.AddCallback(a => a.IsExpression, (obj, o, n) =>
			{
				if (!obj.IsExpression)
					obj.AlignSelections = false;
			});
			UIHelper<FilesFindDialog>.AddCallback(a => a.AlignSelections, (obj, o, n) =>
			{
				if (obj.AlignSelections)
					obj.IsExpression = true;
			});
			UIHelper<FilesFindDialog>.AddCallback(a => a.IsRegex, (obj, o, n) =>
			{
				if (obj.IsRegex)
					obj.IsBinary = false;
			});
			UIHelper<FilesFindDialog>.AddCallback(a => a.IsBinary, (obj, o, n) =>
			{
				if (obj.IsBinary)
					obj.IsRegex = false;
			});
		}


		FilesFindDialog(NEVariables variables)
		{
			Variables = variables;

			InitializeComponent();

			Reset();
			Text = text.GetLastSuggestion() ?? "";
			SetCheckBoxStatus(this.text.GetLastSuggestionData() as CheckBoxStatus);
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
		}

		void OnAcceptSuggestion(string text, object data) => SetCheckBoxStatus(data as CheckBoxStatus);

		void Escape(object sender, RoutedEventArgs e) => Text = Regex.Escape(Text);
		void Unescape(object sender, RoutedEventArgs e) => Text = Regex.Unescape(Text);

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(Text))
				return;

			result = new Result { Text = Text, IsExpression = IsExpression, AlignSelections = AlignSelections, IsRegex = IsRegex, IsBinary = IsBinary, CodePages = IsBinary ? CodePages : null, MatchCase = MatchCase };
			text.AddCurrentSuggestion(GetCheckBoxStatus());

			DialogResult = true;
		}

		void OnCodePagesClick(object sender, RoutedEventArgs e)
		{
			try
			{
				CodePages = CodePagesDialog.Run(this, CodePages);
				IsBinary = true;
			}
			catch { }
		}

		void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		void RegExHelp(object sender, RoutedEventArgs e) => RegExHelpDialog.Display();

		void Reset(object sender = null, RoutedEventArgs e = null)
		{
			IsExpression = AlignSelections = IsRegex = MatchCase = false;
			IsBinary = true;
			CodePages = new HashSet<Coder.CodePage>(CodePagesDialog.DefaultCodePages);
		}

		static public Result Run(Window parent, NEVariables variables)
		{
			var dialog = new FilesFindDialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
