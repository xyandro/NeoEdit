using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using NeoEdit.Common;
using NeoEdit.Common.Enums;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI
{
	partial class TabsWindow : ITabsWindow
	{
		static readonly ActionRunner commandRunner = new ActionRunner();
		static readonly Brush OutlineBrush = new SolidColorBrush(Color.FromRgb(192, 192, 192));
		static readonly Brush BackgroundBrush = new SolidColorBrush(Color.FromRgb(64, 64, 64));

		static TabsWindow()
		{
			UIHelper<TabsWindow>.Register();

			OutlineBrush.Freeze();
			BackgroundBrush.Freeze();
		}

		readonly ITabs Tabs;

		public RenderParameters renderParameters;

		public TabsWindow(ITabs tabs)
		{
			Tabs = tabs;

			NEMenuItem.RegisterCommands(this, (command, multiStatus) => HandleCommand(new ExecuteState(command) { MultiStatus = multiStatus }));
			InitializeComponent();
			UIHelper.AuditMenu(menu);

			//NEClipboard.ClipboardChanged += () => statusBar.InvalidateVisual();
			Font.FontSizeChanged += (s, e) => HandleCommand(new ExecuteState(NECommand.Internal_Redraw));

			scrollBar.MouseWheel += (s, e) => scrollBar.Value -= e.Delta * scrollBar.ViewportSize / 1200;
			Closing += OnClosing;
		}

		public void HandleCommand(ExecuteState state)
		{
			state.Modifiers = Keyboard.Modifiers;
			commandRunner.Add(moreQueued =>
			{
				if (NEClipboard.System == null)
					Dispatcher.Invoke(() => Clipboarder.GetSystem());
				Tabs.HandleCommand(state, moreQueued);
				if (NEClipboard.Current != NEClipboard.System)
					Dispatcher.Invoke(() => Clipboarder.SetSystem());
			});
		}

		public void QueueActivateTabs()
		{
			if ((Helpers.IsDebugBuild) || (!IsActive))
				return;

			HandleCommand(new ExecuteState(NECommand.Internal_Activate));
		}

		void OnActivated(object sender, EventArgs e)
		{
			QueueActivateTabs();
		}

		void OnScrollBarValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => HandleCommand(new ExecuteState(NECommand.Internal_Redraw));

		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			base.OnRenderSizeChanged(sizeInfo);
			HandleCommand(new ExecuteState(NECommand.Internal_Redraw));
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			try { SetPosition(Settings.WindowPosition); } catch { }
		}

		void OnDrop(object sender, DragEventArgs e)
		{
			//TODO
			//var fileList = e.Data.GetData("FileDrop") as string[];
			//if (fileList == null)
			//	return;
			//fileList.ForEach(file => Tabs.AddTab(new ITab(file)));
			//e.Handled = true;
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);

			var key = e.Key;
			if (key == Key.System)
				key = e.SystemKey;

			if (ITabsStatic.HandlesKey(Keyboard.Modifiers, key))
			{
				HandleCommand(new ExecuteState(NECommand.Internal_Key) { Key = key });
				e.Handled = true;
			}
		}

		protected override void OnTextInput(TextCompositionEventArgs e)
		{
			base.OnTextInput(e);

			if (e.Source is MenuItem)
				return;

			HandleCommand(new ExecuteState(NECommand.Internal_Text) { Text = e.Text });
			e.Handled = true;
		}

		//TODO
		//protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
		//{
		//	for (var source = e.OriginalSource as FrameworkElement; source != null; source = source.Parent as FrameworkElement)
		//		if (source is TabData tab)
		//		{
		//			if (HandleClick(tab))
		//			{
		//				e.Handled = true;
		//				return;
		//			}
		//			break;
		//		}
		//	base.OnPreviewMouseLeftButtonDown(e);
		//}

		void OnDrop(DragEventArgs e, ITab toTab)
		{
			//TODO
			//var fromTabs = e.Data.GetData(typeof(List<Tab>)) as List<Tab>;
			//if (fromTabs == null)
			//	return;

			//var toIndex = GetTabIndex(toTab);
			//fromTabs.ForEach(fromTab => fromTab.TabsParent.RemoveTab(fromTab));

			//if (toIndex == -1)
			//	toIndex = Tabs.Count;
			//else
			//	toIndex = Math.Min(toIndex, Tabs.Count);

			//foreach (var fromTab in fromTabs)
			//{
			//	AddTab(fromTab, toIndex);
			//	++toIndex;
			//	e.Handled = true;
			//}
		}

		private void OnClosing(object sender, CancelEventArgs args)
		{
			HandleCommand(new ExecuteState(NECommand.File_Exit) { Configuration = false });
			args.Cancel = true;
		}

		protected override void OnClosed(EventArgs e)
		{
			try { Settings.WindowPosition = GetPosition(); } catch { }
			base.OnClosed(e);
		}

		public void CloseWindow()
		{
			Dispatcher.Invoke(() =>
			{
				Closing -= OnClosing;
				Close();
			});
		}

		TabLabel CreateTabLabel(ITab tab)
		{
			var tabLabel = new TabLabel(tab);
			tabLabel.MouseLeftButtonDown += (s, e) => HandleCommand(new ExecuteState(NECommand.Internal_MouseActivate) { Configuration = tab });
			tabLabel.MouseMove += (s, e) =>
			{
				if (e.LeftButton == MouseButtonState.Pressed)
					DragDrop.DoDragDrop(s as DependencyObject, new DataObject(typeof(List<ITab>), renderParameters.ActiveTabs), DragDropEffects.Move);
			};
			tabLabel.CloseClicked += (s, e) => HandleCommand(new ExecuteState(NECommand.Internal_CloseTab) { Configuration = tab });

			tabLabel.Refresh(renderParameters);
			return tabLabel;
		}

		readonly List<TabWindow> tabWindows = new List<TabWindow>();
		void SetTabWindowCount(int desiredCount)
		{
			while (tabWindows.Count < desiredCount)
				tabWindows.Add(new TabWindow(this));
			tabWindows.RemoveRange(desiredCount, tabWindows.Count - desiredCount);
		}

		void OnStatusBarRender(object s, DrawingContext dc)
		{
			if (renderParameters == null)
				return;

			const string Separator = "  |  ";

			var status = renderParameters.StatusBar;
			var text = new FormattedText(string.Join(Separator, status), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Segoe UI"), 12, Brushes.White, 1);
			var pos = 0;
			for (var ctr = 0; ctr < status.Count - 1; ++ctr)
			{
				pos += status[ctr].Length;
				text.SetForegroundBrush(Brushes.Gray, pos, Separator.Length);
				pos += Separator.Length;
			}
			dc.DrawText(text, new Point(6, 1));
		}

		void SetMenuCheckboxes()
		{
			bool? GetMultiStatus(Func<ITab, bool> func)
			{
				var results = renderParameters.ActiveTabs.Select(func).Distinct().Take(2).ToList();
				if (results.Count != 1)
					return default;
				return results[0];
			}

			menu.menu_File_DontExitOnClose.MultiStatus = Settings.DontExitOnClose;
			menu.menu_Edit_EscapeClearsSelections.MultiStatus = Settings.EscapeClearsSelections;
			menu.menu_Macro_Visualize.MultiStatus = renderParameters.MacroVisualize;
			menu.menu_Window_Font_ShowSpecial.MultiStatus = Font.ShowSpecialChars;

			menu.menu_File_AutoRefresh.MultiStatus = GetMultiStatus(tab => tab.AutoRefresh);
			menu.menu_File_Encrypt.MultiStatus = GetMultiStatus(tab => !string.IsNullOrWhiteSpace(tab.AESKey));
			menu.menu_File_Compress.MultiStatus = GetMultiStatus(tab => tab.Compressed);
			menu.menu_Edit_Navigate_JumpBy_Words.MultiStatus = GetMultiStatus(tab => tab.JumpBy == JumpByType.Words);
			menu.menu_Edit_Navigate_JumpBy_Numbers.MultiStatus = GetMultiStatus(tab => tab.JumpBy == JumpByType.Numbers);
			menu.menu_Edit_Navigate_JumpBy_Paths.MultiStatus = GetMultiStatus(tab => tab.JumpBy == JumpByType.Paths);
			menu.menu_Diff_IgnoreWhitespace.MultiStatus = GetMultiStatus(tab => tab.DiffIgnoreWhitespace);
			menu.menu_Diff_IgnoreCase.MultiStatus = GetMultiStatus(tab => tab.DiffIgnoreCase);
			menu.menu_Diff_IgnoreNumbers.MultiStatus = GetMultiStatus(tab => tab.DiffIgnoreNumbers);
			menu.menu_Diff_IgnoreLineEndings.MultiStatus = GetMultiStatus(tab => tab.DiffIgnoreLineEndings);
			menu.menu_Content_Type_None.MultiStatus = GetMultiStatus(tab => tab.ContentType == ParserType.None);
			menu.menu_Content_Type_Balanced.MultiStatus = GetMultiStatus(tab => tab.ContentType == ParserType.Balanced);
			menu.menu_Content_Type_Columns.MultiStatus = GetMultiStatus(tab => tab.ContentType == ParserType.Columns);
			menu.menu_Content_Type_CPlusPlus.MultiStatus = GetMultiStatus(tab => tab.ContentType == ParserType.CPlusPlus);
			menu.menu_Content_Type_CSharp.MultiStatus = GetMultiStatus(tab => tab.ContentType == ParserType.CSharp);
			menu.menu_Content_Type_CSV.MultiStatus = GetMultiStatus(tab => tab.ContentType == ParserType.CSV);
			menu.menu_Content_Type_ExactColumns.MultiStatus = GetMultiStatus(tab => tab.ContentType == ParserType.ExactColumns);
			menu.menu_Content_Type_HTML.MultiStatus = GetMultiStatus(tab => tab.ContentType == ParserType.HTML);
			menu.menu_Content_Type_JSON.MultiStatus = GetMultiStatus(tab => tab.ContentType == ParserType.JSON);
			menu.menu_Content_Type_SQL.MultiStatus = GetMultiStatus(tab => tab.ContentType == ParserType.SQL);
			menu.menu_Content_Type_TSV.MultiStatus = GetMultiStatus(tab => tab.ContentType == ParserType.TSV);
			menu.menu_Content_Type_XML.MultiStatus = GetMultiStatus(tab => tab.ContentType == ParserType.XML);
			menu.menu_Content_HighlightSyntax.MultiStatus = GetMultiStatus(tab => tab.HighlightSyntax);
			menu.menu_Content_StrictParsing.MultiStatus = GetMultiStatus(tab => tab.StrictParsing);
			menu.menu_Content_KeepSelections.MultiStatus = GetMultiStatus(tab => tab.KeepSelections);
			menu.menu_Window_ViewBinary.MultiStatus = GetMultiStatus(tab => tab.ViewBinary);
		}

		public void Render(RenderParameters renderParameters)
		{
			this.renderParameters = renderParameters;
			Dispatcher.Invoke(() => DrawAll());
		}

		void DrawAll()
		{
			statusBar.InvalidateVisual();
			SetMenuCheckboxes();
			Title = $"{(renderParameters.FocusedTab == null ? "" : $"{renderParameters.FocusedTab.DisplayName ?? renderParameters.FocusedTab.FileName ?? "Untitled"} - ")}NeoEdit{(Helpers.IsAdministrator() ? " (Administrator)" : "")}";

			if ((renderParameters.WindowLayout.Columns == 1) && (renderParameters.WindowLayout.Rows == 1))
				DoFullLayout();
			else
				DoGridLayout();
			UpdateLayout();
		}

		//public bool GotoTab(string fileName, int? line, int? column, int? index)
		//{
		//	var tab = Tabs.AllITabs.FirstOrDefault(x => x.FileName == fileName);
		//	if (tab == null)
		//		return false;
		//	//TODO
		//	//Activate();
		//	//Tabs.ClearAllActive();
		//	//Tabs.SetActive(tab);
		//	//Tabs.FocusedITab = tab;
		//	//tab.Execute_File_Refresh();
		//	//tab.Goto(line, column, index);
		//	return true;
		//}

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool SetForegroundWindow(IntPtr hWnd);

		public void SetForeground()
		{
			Activate();
			Show();
			SetForegroundWindow(new WindowInteropHelper(this).Handle);
		}

		public void ShowExceptionMessage(Exception ex) => Dispatcher.Invoke(() => App.ShowExceptionMessage(ex));

		double? macroProgress, taskRunnerProgress;
		public void SetMacroProgress(double? percent) => macroProgress = percent;
		public void SetTaskRunnerProgress(double? percent) => taskRunnerProgress = percent;
	}
}
