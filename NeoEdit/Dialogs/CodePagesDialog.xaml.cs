using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NeoEdit.Program;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Parsing;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program.Dialogs
{
	public class CodePagesDialogCheckBox : CheckBox
	{
		Coder.CodePage codePage = Coder.CodePage.None;
		public Coder.CodePage CodePage
		{
			get => codePage;
			set
			{
				codePage = value;
				Content = Coder.IsStr(codePage) ? Coder.GetDescription(codePage) : codePage.ToString();
			}
		}
	}

	partial class CodePagesDialog
	{
		public static IReadOnlyCollection<Coder.CodePage> DefaultCodePages { get; } = new HashSet<Coder.CodePage> { Coder.CodePage.UInt8, Coder.CodePage.UInt16LE, Coder.CodePage.UInt32LE, Coder.CodePage.UInt64LE, Coder.CodePage.Int8, Coder.CodePage.Int16LE, Coder.CodePage.Int32LE, Coder.CodePage.Int64LE, Coder.DefaultCodePage, Coder.CodePage.UTF8, Coder.CodePage.UTF16LE };

		HashSet<Coder.CodePage> result { get; set; }

		[DepProp]
		public bool ShowLE { get { return UIHelper<CodePagesDialog>.GetPropValue<bool>(this); } set { UIHelper<CodePagesDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowBE { get { return UIHelper<CodePagesDialog>.GetPropValue<bool>(this); } set { UIHelper<CodePagesDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowInt { get { return UIHelper<CodePagesDialog>.GetPropValue<bool>(this); } set { UIHelper<CodePagesDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowFloat { get { return UIHelper<CodePagesDialog>.GetPropValue<bool>(this); } set { UIHelper<CodePagesDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowStr { get { return UIHelper<CodePagesDialog>.GetPropValue<bool>(this); } set { UIHelper<CodePagesDialog>.SetPropValue(this, value); } }
		[DepProp]
		ObservableCollection<CodePagesDialogCheckBox> EncodingCheckBoxes { get { return UIHelper<CodePagesDialog>.GetPropValue<ObservableCollection<CodePagesDialogCheckBox>>(this); } set { UIHelper<CodePagesDialog>.SetPropValue(this, value); } }

		static bool savedShowLE = true;
		static bool savedShowBE = false;
		static bool savedShowInt = true;
		static bool savedShowFloat = false;
		static bool savedShowStr = true;
		readonly List<CodePagesDialogCheckBox> checkBoxes;
		readonly static HashSet<Coder.CodePage> savedCodePages = new HashSet<Coder.CodePage>(DefaultCodePages);

		static CodePagesDialog() => UIHelper<CodePagesDialog>.Register();

		CodePagesDialog(HashSet<Coder.CodePage> startCodePages = null)
		{
			InitializeComponent();

			EncodingCheckBoxes = new ObservableCollection<CodePagesDialogCheckBox>(Coder.GetStringCodePages().Select(codePage => new CodePagesDialogCheckBox { CodePage = codePage }));
			checkBoxes = this.FindLogicalChildren<CodePagesDialogCheckBox>().Concat(EncodingCheckBoxes).ToList();

			ShowLE = savedShowLE;
			ShowBE = savedShowBE;
			ShowInt = savedShowInt;
			ShowFloat = savedShowFloat;
			ShowStr = savedShowStr;
			startCodePages = startCodePages ?? savedCodePages;
			foreach (var checkBox in checkBoxes)
				checkBox.IsChecked = startCodePages.Contains(checkBox.CodePage);
		}

		void CodePagesPreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Space)
			{
				var selected = codePages.SelectedItems.OfType<CodePagesDialogCheckBox>().ToList();
				var status = !selected.All(item => item.IsChecked == true);
				selected.ForEach(item => item.IsChecked = status);
				e.Handled = true;
			}
		}

		void SelectAllNoneNumerics(object sender, RoutedEventArgs e)
		{
			var all = (e.Source as FrameworkElement).Tag as string == "all";
			var children = (e.Source as FrameworkElement).FindParent<GroupBox>().FindLogicalChildren<CodePagesDialogCheckBox>().ToList();
			foreach (var child in children)
				if (child.IsVisible)
					child.IsChecked = all;
		}

		void SelectAllNoneStrings(object sender, RoutedEventArgs e)
		{
			var all = (e.Source as FrameworkElement).Tag as string == "all";
			foreach (var checkBox in EncodingCheckBoxes)
				checkBox.IsChecked = all;
		}

		void SelectAllNoneGlobal(object sender, RoutedEventArgs e)
		{
			var all = (e.Source as FrameworkElement).Tag as string == "all";
			if (all)
				ShowLE = ShowBE = ShowInt = ShowFloat = ShowStr = true;
			foreach (var checkBox in checkBoxes)
				checkBox.IsChecked = all;
		}

		void Reset(object sender = null, RoutedEventArgs e = null)
		{
			ShowLE = ShowInt = ShowStr = true;
			ShowBE = ShowFloat = false;
			foreach (var checkBox in checkBoxes)
				checkBox.IsChecked = DefaultCodePages.Contains(checkBox.CodePage);
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			var codePages = new HashSet<Coder.CodePage>();
			foreach (var checkBox in checkBoxes)
			{
				if (checkBox.IsChecked != true)
					continue;
				var str = Coder.IsStr(checkBox.CodePage);
				if ((str) && (!ShowStr))
					continue;
				if ((!str) && (!checkBox.IsVisible))
					continue;
				codePages.Add(checkBox.CodePage);
			}

			if (codePages.Count == 0)
				return;

			result = codePages;

			savedShowLE = ShowLE;
			savedShowBE = ShowBE;
			savedShowInt = ShowInt;
			savedShowFloat = ShowFloat;
			savedShowStr = ShowStr;
			savedCodePages.Clear();
			foreach (var checkBox in checkBoxes)
				if (checkBox.IsChecked == true)
					savedCodePages.Add(checkBox.CodePage);

			DialogResult = true;
		}

		public static HashSet<Coder.CodePage> Run(Window parent, HashSet<Coder.CodePage> startCodePages = null)
		{
			var find = new CodePagesDialog(startCodePages) { Owner = parent };
			if (!find.ShowDialog())
				return null;
			return find.result;
		}
	}
}
