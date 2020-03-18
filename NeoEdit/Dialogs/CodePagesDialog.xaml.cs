using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program.Dialogs
{
	partial class CodePagesDialog
	{
		public static IReadOnlyCollection<Coder.CodePage> DefaultCodePages { get; } = new HashSet<Coder.CodePage> { Coder.CodePage.UInt8, Coder.CodePage.UInt16LE, Coder.CodePage.UInt32LE, Coder.CodePage.UInt64LE, Coder.CodePage.Int8, Coder.CodePage.Int16LE, Coder.CodePage.Int32LE, Coder.CodePage.Int64LE, Coder.DefaultCodePage, Coder.CodePage.UTF8, Coder.CodePage.UTF16LE };

		readonly HashSet<string> codePages = new HashSet<string>();
		readonly List<FrameworkElement> checkBoxes = new List<FrameworkElement>();
		CodePagesDialog(HashSet<Coder.CodePage> startCodePages)
		{
			InitializeComponent();

			var row = 0;
			foreach (var codePage in Coder.GetStringCodePages())
			{
				stringsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

				var toggleButton = new CodePageToggle { Text = Coder.GetDescription(codePage), Tag = codePage.ToString(), Padding = new Thickness(1) };
				toggleButton.Click += OnClick;
				Grid.SetRow(toggleButton, row);
				Grid.SetColumn(toggleButton, 0);
				stringsGrid.Children.Add(toggleButton);

				checkBoxes.Add(toggleButton);

				++row;
			}

			Strings.Tag = string.Join("|", checkBoxes.Select(x => x.Tag));

			checkBoxes.AddRange(new List<FrameworkElement> { LE, SIntLE, UIntLE, Int8LE, Int16LE, Int32LE, Int64LE, UInt8LE, UInt16LE, UInt32LE, UInt64LE, BE, SIntBE, UIntBE, Int8BE, Int16BE, Int32BE, Int64BE, UInt8BE, UInt16BE, UInt32BE, UInt64BE, Float, Single, Double, Strings });

			SetCodePages(startCodePages);
		}

		void SetCodePages(IReadOnlyCollection<Coder.CodePage> startCodePages)
		{
			codePages.Clear();
			foreach (var startCodePage in startCodePages)
			{
				var codePage = startCodePage.ToString();
				if (!codePage.EndsWith("Int8"))
					codePages.Add(codePage);
				else
				{
					var hasLE = startCodePages.Intersect(Coder.GetNumericCodePages()).Any(x => x.ToString().EndsWith("LE"));
					var hasBE = startCodePages.Intersect(Coder.GetNumericCodePages()).Any(x => x.ToString().EndsWith("BE"));
					if ((hasLE) || (!hasBE))
						codePages.Add(codePage + "LE");
					if (hasBE)
						codePages.Add(codePage + "BE");
				}
			}
			UpdateCheckBoxes();
		}

		void OnClick(object sender, RoutedEventArgs e)
		{
			if (!(e.Source is ToggleButton button))
				return;

			var codePages = ((sender as FrameworkElement).Tag as string).Split('|').ToList();
			if (button.IsChecked == true)
				codePages.ForEach(codePage => this.codePages.Add(codePage));
			else
				codePages.ForEach(codePage => this.codePages.Remove(codePage));

			UpdateCheckBoxes();
		}

		void UpdateCheckBoxes()
		{
			foreach (var checkBox in checkBoxes)
			{
				var tags = (checkBox.Tag as string).Split('|').ToList();
				var found = tags.Select(codePage => codePages.Contains(codePage)).Distinct().Take(2).ToList();
				var state = found.Count == 1 ? found[0] : default(bool?);
				SetCheckbox(checkBox, state);
			}
		}

		void SetCheckbox(FrameworkElement checkBox, bool? state)
		{
			if (checkBox is CheckBox cb)
				cb.IsChecked = state;
			else if (checkBox is CodePageToggle cpt)
				cpt.IsChecked = state.Value;
		}

		void CheckAllNone(object sender, RoutedEventArgs e)
		{
			if (!(sender is FrameworkElement element))
				return;
			if ((element.Tag as string) == "All")
				checkBoxes.OfType<FrameworkElement>().SelectMany(x => (x.Tag as string).Split('|')).Distinct().ForEach(codePage => codePages.Add(codePage));
			else
				codePages.Clear();
			UpdateCheckBoxes();
		}

		void Reset(object sender = null, RoutedEventArgs e = null) => SetCodePages(DefaultCodePages);

		HashSet<Coder.CodePage> result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new HashSet<Coder.CodePage>();
			foreach (var codePage in codePages)
			{
				switch (codePage)
				{
					case "UInt8LE": case "UInt8BE": result.Add(Coder.CodePage.UInt8); break;
					case "Int8LE": case "Int8BE": result.Add(Coder.CodePage.Int8); break;
					default: result.Add(Helpers.ParseEnum<Coder.CodePage>(codePage)); break;
				}
			}

			DialogResult = true;
		}

		public static HashSet<Coder.CodePage> Run(Window parent, HashSet<Coder.CodePage> startCodePages = null)
		{
			var find = new CodePagesDialog(startCodePages) { Owner = parent };
			if (!find.ShowDialog())
				throw new OperationCanceledException();
			return find.result;
		}
	}

	public class NoResizeScrollViewer : ScrollViewer
	{
		protected override Size MeasureOverride(Size constraint) => new Size(base.MeasureOverride(constraint).Width, 0);

		protected override Size ArrangeOverride(Size arrangeBounds)
		{
			Height = arrangeBounds.Height;
			return base.ArrangeOverride(arrangeBounds);
		}
	}
}
