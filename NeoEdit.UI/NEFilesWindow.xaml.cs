using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.UI.Controls;
using NeoEdit.UI.Dialogs;

namespace NeoEdit.UI
{
	partial class NEFilesWindow : INEFilesWindow
	{
		static readonly ActionRunner actionRunner = new ActionRunner();
		static readonly Brush OutlineBrush = new SolidColorBrush(Color.FromRgb(192, 192, 192));
		static readonly Brush BackgroundBrush = new SolidColorBrush(Color.FromRgb(64, 64, 64));

		static NEFilesWindow()
		{
			UIHelper<NEFilesWindow>.Register();

			OutlineBrush.Freeze();
			BackgroundBrush.Freeze();

			Clipboarder.Initialize();
		}

		readonly INEFiles NEFiles;

		public RenderParameters renderParameters;

		public NEFilesWindow(INEFiles neFiles)
		{
			NEFiles = neFiles;

			NEMenuItem.RegisterCommands(this, (command, multiStatus) => HandleCommand(new ExecuteState(command) { MultiStatus = multiStatus }));
			InitializeComponent();
			UIHelper.AuditMenu(menu);
			menu.StopTasks += StopTasks;
			menu.KillTasks += KillTasks;

			//NEClipboard.ClipboardChanged += () => statusBar.InvalidateVisual();
			Font.FontSizeChanged += (s, e) => HandleCommand(new ExecuteState(NECommand.Internal_Redraw));

			scrollBar.MouseWheel += (s, e) => scrollBar.Value -= e.Delta * scrollBar.ViewportSize / 1200;
			Closing += OnClosing;
		}

		public void HandleCommand(ExecuteState state)
		{
			state.Modifiers = Keyboard.Modifiers;
			actionRunner.Add(moreQueued =>
			{
				Clipboarder.GetSystem(Dispatcher);
				NEFiles.HandleCommand(state, moreQueued);
				Clipboarder.SetSystem(Dispatcher);
			});
		}

		public void QueueActivateNEFiles()
		{
			RunOnUIThread(() =>
			{
				if (!IsActive)
					return;

				HandleCommand(new ExecuteState(NECommand.Internal_Activate));
			});
		}

		void OnActivated(object sender, EventArgs e)
		{
			if (!Helpers.IsDebugBuild)
				QueueActivateNEFiles();
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
			//fileList.ForEach(file => Files.AddFile(new INEFile(file)));
			//e.Handled = true;
		}

		bool StopTasks()
		{
			var result = false;
			if (actionRunner.CancelActive())
				result = true;
			if (NEFiles.StopTasks())
				result = true;
			return result;
		}

		bool KillTasks()
		{
			actionRunner.CancelActive();
			NEFiles.KillTasks();
			return true;
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);

			var key = e.Key;
			if (key == Key.System)
				key = e.SystemKey;

			if (key == Key.Escape)
				e.Handled = StopTasks();

			if (key == Key.Cancel)
				e.Handled = KillTasks();

			if ((!e.Handled) && (INEFilesStatic.HandlesKey(Keyboard.Modifiers, key)))
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
		//		if (source is FileData file)
		//		{
		//			if (HandleClick(file))
		//			{
		//				e.Handled = true;
		//				return;
		//			}
		//			break;
		//		}
		//	base.OnPreviewMouseLeftButtonDown(e);
		//}

		void OnDrop(DragEventArgs e, INEFile toFile)
		{
			//TODO
			//var fromFiles = e.Data.GetData(typeof(List<NEFile>)) as List<NEFile>;
			//if (fromFiles == null)
			//	return;

			//var toIndex = GetFileIndex(toFile);
			//fromFiles.ForEach(fromFile => fromFile.FilesParent.RemoveFile(fromFile));

			//if (toIndex == -1)
			//	toIndex = Files.Count;
			//else
			//	toIndex = Math.Min(toIndex, Files.Count);

			//foreach (var fromFile in fromFiles)
			//{
			//	AddFile(fromFile, toIndex);
			//	++toIndex;
			//	e.Handled = true;
			//}
		}

		private void OnClosing(object sender, CancelEventArgs args)
		{
			if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
				Environment.Exit(0);

			HandleCommand(new ExecuteState(NECommand.File_Exit) { Configuration = new Configuration_File_Exit { WindowClosed = true } });
			args.Cancel = true;
		}

		protected override void OnClosed(EventArgs e)
		{
			try { Settings.WindowPosition = GetPosition(); } catch { }
			base.OnClosed(e);
		}

		public void CloseWindow()
		{
			RunOnUIThread(() =>
			{
				Closing -= OnClosing;
				Close();
			});
		}

		NEFileLabel CreateFileLabel(INEFile neFile)
		{
			var fileLabel = new NEFileLabel(neFile);
			fileLabel.MouseLeftButtonDown += (s, e) => HandleCommand(new ExecuteState(NECommand.Internal_MouseActivate) { Configuration = new Configuration_Internal_MouseActivate { NEFile = neFile } });
			fileLabel.MouseMove += (s, e) =>
			{
				if (e.LeftButton == MouseButtonState.Pressed)
					DragDrop.DoDragDrop(s as DependencyObject, new DataObject(typeof(List<INEFile>), renderParameters.ActiveFiles), DragDropEffects.Move);
			};
			fileLabel.CloseClicked += (s, e) => HandleCommand(new ExecuteState(NECommand.Internal_CloseFile) { Configuration = new Configuration_Internal_CloseFile { NEFile = neFile } });

			fileLabel.Refresh(renderParameters);
			return fileLabel;
		}

		readonly List<NEFileWindow> fileWindows = new List<NEFileWindow>();
		void SetFileWindowCount(int desiredCount)
		{
			while (fileWindows.Count < desiredCount)
				fileWindows.Add(new NEFileWindow(this));
			fileWindows.RemoveRange(desiredCount, fileWindows.Count - desiredCount);
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

			progressBars.Width = Math.Max(100, statusBar.ActualWidth - text.Width - 6);
		}

		void SetMenuCheckboxes()
		{
			menu.menu_File_AutoRefresh.MultiStatus = renderParameters.MenuStatus[nameof(NECommand.File_AutoRefresh)];
			menu.menu_File_Advanced_Compress.MultiStatus = renderParameters.MenuStatus[nameof(NECommand.File_Advanced_Compress)];
			menu.menu_File_Advanced_Encrypt.MultiStatus = renderParameters.MenuStatus[nameof(NECommand.File_Advanced_Encrypt)];
			menu.menu_File_Advanced_DontExitOnClose.MultiStatus = renderParameters.MenuStatus[nameof(NECommand.File_Advanced_DontExitOnClose)];
			menu.menu_Edit_Navigate_JumpBy_Words.MultiStatus = renderParameters.MenuStatus[nameof(NECommand.Edit_Navigate_JumpBy_Words)];
			menu.menu_Edit_Navigate_JumpBy_Numbers.MultiStatus = renderParameters.MenuStatus[nameof(NECommand.Edit_Navigate_JumpBy_Numbers)];
			menu.menu_Edit_Navigate_JumpBy_Paths.MultiStatus = renderParameters.MenuStatus[nameof(NECommand.Edit_Navigate_JumpBy_Paths)];
			menu.menu_Edit_Advanced_EscapeClearsSelections.MultiStatus = renderParameters.MenuStatus[nameof(NECommand.Edit_Advanced_EscapeClearsSelections)];
			menu.menu_Content_Type_None.MultiStatus = renderParameters.MenuStatus[nameof(NECommand.Content_Type_None)];
			menu.menu_Content_Type_Balanced.MultiStatus = renderParameters.MenuStatus[nameof(NECommand.Content_Type_Balanced)];
			menu.menu_Content_Type_Columns.MultiStatus = renderParameters.MenuStatus[nameof(NECommand.Content_Type_Columns)];
			menu.menu_Content_Type_CPlusPlus.MultiStatus = renderParameters.MenuStatus[nameof(NECommand.Content_Type_CPlusPlus)];
			menu.menu_Content_Type_CSharp.MultiStatus = renderParameters.MenuStatus[nameof(NECommand.Content_Type_CSharp)];
			menu.menu_Content_Type_CSV.MultiStatus = renderParameters.MenuStatus[nameof(NECommand.Content_Type_CSV)];
			menu.menu_Content_Type_ExactColumns.MultiStatus = renderParameters.MenuStatus[nameof(NECommand.Content_Type_ExactColumns)];
			menu.menu_Content_Type_HTML.MultiStatus = renderParameters.MenuStatus[nameof(NECommand.Content_Type_HTML)];
			menu.menu_Content_Type_JSON.MultiStatus = renderParameters.MenuStatus[nameof(NECommand.Content_Type_JSON)];
			menu.menu_Content_Type_SQL.MultiStatus = renderParameters.MenuStatus[nameof(NECommand.Content_Type_SQL)];
			menu.menu_Content_Type_TSV.MultiStatus = renderParameters.MenuStatus[nameof(NECommand.Content_Type_TSV)];
			menu.menu_Content_Type_XML.MultiStatus = renderParameters.MenuStatus[nameof(NECommand.Content_Type_XML)];
			menu.menu_Content_HighlightSyntax.MultiStatus = renderParameters.MenuStatus[nameof(NECommand.Content_HighlightSyntax)];
			menu.menu_Content_StrictParsing.MultiStatus = renderParameters.MenuStatus[nameof(NECommand.Content_StrictParsing)];
			menu.menu_Content_KeepSelections.MultiStatus = renderParameters.MenuStatus[nameof(NECommand.Content_KeepSelections)];
			menu.menu_Diff_IgnoreWhitespace.MultiStatus = renderParameters.MenuStatus[nameof(NECommand.Diff_IgnoreWhitespace)];
			menu.menu_Diff_IgnoreCase.MultiStatus = renderParameters.MenuStatus[nameof(NECommand.Diff_IgnoreCase)];
			menu.menu_Diff_IgnoreNumbers.MultiStatus = renderParameters.MenuStatus[nameof(NECommand.Diff_IgnoreNumbers)];
			menu.menu_Diff_IgnoreLineEndings.MultiStatus = renderParameters.MenuStatus[nameof(NECommand.Diff_IgnoreLineEndings)];
			menu.menu_Macro_Visualize.MultiStatus = renderParameters.MenuStatus[nameof(NECommand.Macro_Visualize)];
			menu.menu_Window_ActiveOnly.MultiStatus = renderParameters.MenuStatus[nameof(NECommand.Window_ActiveOnly)];
			menu.menu_Window_Font_ShowSpecial.MultiStatus = renderParameters.MenuStatus[nameof(NECommand.Window_Font_ShowSpecial)];
			menu.menu_Window_Binary.MultiStatus = renderParameters.MenuStatus[nameof(NECommand.Window_Binary)];
		}

		public void Render(RenderParameters renderParameters)
		{
			this.renderParameters = renderParameters;
			RunOnUIThread(() => DrawAll());
		}

		void DrawAll()
		{
			statusBar.InvalidateVisual();
			SetMenuCheckboxes();
			Title = $"{(renderParameters.FocusedFile == null ? "" : $"{renderParameters.FocusedFile.DisplayName ?? renderParameters.FocusedFile.FileName ?? "Untitled"} - ")}NeoEdit{(Helpers.IsAdministrator() ? " (Administrator)" : "")}";

			if ((renderParameters.WindowLayout.Columns == 1) && (renderParameters.WindowLayout.Rows == 1))
				DoFullLayout();
			else
				DoGridLayout();
			UpdateLayout();
		}

		//public bool GotoFile(string fileName, int? line, int? column, int? index)
		//{
		//	var neFile = NEFiles.AllIFiles.FirstOrDefault(x => x.FileName == fileName);
		//	if (neFile == null)
		//		return false;
		//	//TODO
		//	//Activate();
		//	//Files.ClearAllActive();
		//	//Files.SetActive(neFile);
		//	//Files.FocusedIFile = neFile;
		//	//neFile.Execute_File_Refresh();
		//	//neFile.Goto(line, column, index);
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

		void SetProgress(ProgressBar progressBar, double? percent)
		{
			RunOnUIThread(() =>
			{
				if (percent.HasValue)
				{
					if (progressBar.Visibility != Visibility.Visible)
						progressBar.Visibility = Visibility.Visible;
					progressBar.Value = percent.Value;
				}
				else
				{
					if (progressBar.Visibility != Visibility.Hidden)
						progressBar.Visibility = Visibility.Hidden;
				}
			});
		}

		public void SetMacroProgress(double? percent) => SetProgress(macroProgressBar, percent);
		public void SetTaskRunnerProgress(double? percent) => SetProgress(taskRunnerProgressBar, percent);

		void RunOnUIThread(Action action)
		{
			if (!Dispatcher.CheckAccess())
				Dispatcher.Invoke(action);
			else
				action();
		}

		T RunOnUIThread<T>(Func<T> func)
		{
			if (!Dispatcher.CheckAccess())
				return Dispatcher.Invoke(func);
			else
				return func();
		}

		public void ShowExceptionMessage(Exception ex)
		{
			var window = this;
			if (!window.IsVisible)
				window = null;
			RunOnUIThread(() => ShowExceptionMessage(ex, window));
		}

		public static void ShowExceptionMessage(Exception ex, Window window = null)
		{
			var message = "";
			for (var ex2 = ex; ex2 != null; ex2 = ex2.InnerException)
				message += $"{ex2.Message}\n";

			Message.Run(window, "Error", message);

			if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
			{
				var exceptions = new List<Exception> { ex };
				while (exceptions[0].InnerException != null)
					exceptions.Insert(0, exceptions[0].InnerException);

				if ((Helpers.IsDebugBuild) && (Debugger.IsAttached))
				{
					var inner = exceptions.First();
					var er = inner?.StackTrace?.Split('\r', '\n').FirstOrDefault(a => a.Contains(":line"));
					if (er != null)
					{
						var idx = er.LastIndexOf(" in ");
						if (idx != -1)
							er = er.Substring(idx + 4);
						idx = er.IndexOf(":line ");
						er = $"{er.Substring(0, idx)} {er.Substring(idx + 6)}";
						NoDelayClipboard.SetText(er);
					}
					Debugger.Break();
				}
				else
				{
					var sb = new StringBuilder();
					var first = true;
					foreach (var exception in exceptions)
					{
						if (first)
							first = false;
						else
							sb.AppendLine();
						sb.AppendLine($"Message: {exception?.Message ?? ""}");
						sb.AppendLine($"StackTrace:\r\n{exception?.StackTrace ?? ""}");
					}
					Clipboard.SetText(sb.ToString());
				}
			}
		}
	}
}
