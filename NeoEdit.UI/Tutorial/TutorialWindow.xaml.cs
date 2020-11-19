using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using NeoEdit.Common;
using NeoEdit.Common.Transform;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Tutorial
{
	partial class TutorialWindow
	{
		const string StartPage = "Home";

		readonly bool LoadFromFile = false;
		readonly INEWindow neWindow;
		readonly List<string> pages = new List<string>();
		int currentPage = 0;

		void AuditTutorial()
		{
			try
			{
				var pagesToCheck = new Queue<string>();
				pagesToCheck.Enqueue(StartPage);
				var pagesChecked = new HashSet<string>();
				while (pagesToCheck.Any())
				{
					var pageToCheck = pagesToCheck.Dequeue();
					if (pagesChecked.Contains(pageToCheck))
						continue;
					pagesChecked.Add(pageToCheck);

					try
					{
						var page = GetPage(pageToCheck);
						if (page == null)
							throw new Exception($"{nameof(GetPage)} returned null");

						foreach (var tag in UIHelper.FindLogicalChildren<Hyperlink>(page).Select(hl => hl.Tag).OfType<string>().SelectMany(x => x.Split(';')))
						{
							var parts = tag.Split(':');
							if (parts.Length == 0)
								throw new Exception($"Empty tag in page {pageToCheck}");

							switch (parts[0])
							{
								case "Data":
									if (parts.Length != 3)
										throw new Exception($"Invalid tag: {tag}");
									if (string.IsNullOrEmpty(parts[1]))
										throw new Exception($"No page title: {tag}");
									try { Coder.BytesToString(Compressor.Decompress(Coder.StringToBytes(parts[2], Coder.CodePage.Base64), Compressor.Type.GZip), Coder.CodePage.UTF8); }
									catch (Exception ex) { throw new Exception($"Unable to decode data: {parts[2]}", ex); }
									break;
								case "Page":
									if (parts.Length != 2)
										throw new Exception($"Invalid tag: {tag}");
									pagesToCheck.Enqueue(parts[1]);
									break;
								default: throw new Exception($"Invalid tag: {parts[0]}");
							}
						}
					}
					catch (Exception ex) { throw new Exception($"Failed to load {pageToCheck}: {ex.Message}", ex); }
				}
			}
			catch (Exception ex) { MessageBox.Show(ex.Message); }
		}

		public TutorialWindow(INEWindow neWindow)
		{
			this.neWindow = neWindow;
			InitializeComponent();
			//TODO Owner = neWindow.FilesWindow;

			if (Helpers.IsDebugBuild)
			{
				AuditTutorial();
				LoadFromFile = true;
			}

			pages.Add(StartPage);
			Refresh();
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.Key == Key.F5)
				Refresh();
		}

		TutorialPage GetPage(string page)
		{
			var resources = Resources;

			if (LoadFromFile)
			{
				string data;
				using (var input = new StreamReader($@"{typeof(TutorialWindow).Assembly.Location}\..\..\..\NeoEdit\Tutorial\Pages\{page}.xaml"))
					data = input.ReadToEnd();
				data = data.Replace($@"""clr-namespace:{typeof(TutorialWindow).Namespace}""", $@"""clr-namespace:{typeof(TutorialWindow).Namespace};assembly={typeof(TutorialWindow).Assembly.FullName}""");
				resources = XamlReader.Parse(data) as ResourceDictionary;
			}

			return resources[page] as TutorialPage;
		}

		void Refresh()
		{
			var tutorialPage = GetPage(pages[currentPage]);
			title.Text = tutorialPage.Title;
			content.Document = tutorialPage;
		}

		void BackClick(object sender, RoutedEventArgs e)
		{
			currentPage = Math.Max(0, currentPage - 1);
			Refresh();
		}

		void ForwardClick(object sender, RoutedEventArgs e)
		{
			currentPage = Math.Min(pages.Count - 1, currentPage + 1);
			Refresh();
		}

		void OnLinkClick(object sender, RoutedEventArgs e)
		{
			var tags = (sender as Hyperlink)?.Tag?.ToString();
			if (tags == null)
				throw new Exception("Missing tag");

			foreach (var tag in tags.Split(';'))
			{
				var parts = tag.Split(':');
				switch (parts[0])
				{
					case "Page": GotoPage(parts[1]); break;
					case "Data": LoadData(parts[1], parts[2]); break;
				}
			}
		}

		void GotoPage(string page)
		{
			pages.RemoveRange(currentPage + 1, pages.Count - currentPage - 1);
			pages.Add(page);
			++currentPage;
			Refresh();
		}

		void LoadData(string pageName, string data)
		{
			//TODO
			//var bytes = Compressor.Decompress(Coder.StringToBytes(data, Coder.CodePage.Base64), Compressor.Type.GZip);
			//files.AddFile(new INEFile(displayName: pageName, bytes: bytes, codePage: Coder.CodePage.UTF8, modified: false));
		}
	}
}
