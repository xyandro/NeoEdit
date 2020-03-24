using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NeoEdit.Common;
using NeoEdit.Common.Parsing;
using NeoEdit.Program;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Dialogs
{
	partial class MultiProgressDialog
	{
		public const int DefaultConcurrency = 9;

		class RunTask
		{
			public object Item { get; set; }
			public string Name { get; set; }
			public Task<object> Task { get; set; }
			public double Value { get; set; }
			public double Maximum { get; set; }
		}

		class ProgressData
		{
			public UIElement UIElement { get; }
			public TextBlock TextBlock { get; }
			public ProgressBar ProgressBar { get; }
			public RunTask RunTask { get; set; }

			public ProgressData(UIElement uiElement, TextBlock textblock, ProgressBar progressBar)
			{
				UIElement = uiElement;
				TextBlock = textblock;
				ProgressBar = progressBar;
			}
		}
		RunTask OverallTask;
		List<ProgressData> ProgressDatas = new List<ProgressData>();

		readonly int Concurrency;
		readonly List<object> Items;
		readonly Func<object, IProgress<ProgressReport>, CancellationToken, Task<object>> GetTask;
		readonly Func<object, string> GetName;
		readonly List<RunTask> Running = new List<RunTask>();
		readonly Dictionary<object, object> Result = new Dictionary<object, object>();
		readonly BackgroundWorker Worker;
		readonly CancellationTokenSource token = new CancellationTokenSource();
		readonly AutoResetEvent Event = new AutoResetEvent(false);
		int Current = 0, Finished = 0;
		Exception Exception = null;
		bool IsFinished = false;

		static MultiProgressDialog() { UIHelper<MultiProgressDialog>.Register(); }

		MultiProgressDialog(List<object> items, Func<object, IProgress<ProgressReport>, CancellationToken, Task<object>> getTask, Func<object, string> getName, int concurrency)
		{
			InitializeComponent();
			Items = items;
			GetTask = getTask;
			GetName = getName;
			Concurrency = concurrency;

			SetupGrid();

			Worker = new BackgroundWorker();
			Worker.DoWork += WorkerDoWork;
			Worker.RunWorkerCompleted += WorkerRunWorkerCompleted;
			Worker.RunWorkerAsync();

			new Thread(UpdateUIThread).Start();
		}

		void WorkerDoWork(object sender, DoWorkEventArgs e)
		{
			while (true)
			{
				lock (Running)
				{
					while ((Running.Count < Concurrency) && (Current < Items.Count) && (!token.IsCancellationRequested))
					{
						var item = Items[Current++];
						var runTask = new RunTask { Item = item, Name = GetName(item) };
						var progress = new Progress<ProgressReport>(tuple =>
						{
							runTask.Value = tuple.Value;
							runTask.Maximum = tuple.Maximum;
							Event.Set();
						});
						runTask.Task = GetTask(item, progress, token.Token);
						Running.Add(runTask);
						var progressData = ProgressDatas.First(x => x.RunTask == null);
						progressData.RunTask = runTask;
						Event.Set();
					}
				}
				if (Running.Count == 0)
					break;

				var finished = Task.WhenAny(Running.Select(task => task.Task)).Result;
				lock (Running)
				{
					var runTask = Running.First(tuple => tuple.Task == finished);
					ProgressDatas.Single(x => x.RunTask == runTask).RunTask = null;
					Running.Remove(runTask);
					if ((finished.IsCanceled) || (finished.Exception != null))
					{
						OnCancel();
						if (Exception == null)
							Exception = finished.Exception;
						Event.Set();
					}
					else if (finished.Status == TaskStatus.RanToCompletion)
						Result[runTask.Item] = finished.Result;
					++Finished;
					Event.Set();
				}
			}
		}

		void WorkerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			DialogResult = IsFinished = true;
			Event.Set();
		}

		void UpdateUIThread()
		{
			while (!IsFinished)
			{
				Event.WaitOne();
				lock (Running)
					Dispatcher.Invoke(UpdateGrid);
			}
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			if (!DialogResult.HasValue)
				OnCancel();
			if (!DialogResult.HasValue)
			{
				e.Cancel = true;
				return;
			}
			base.OnClosing(e);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
				OnCancel();
			base.OnKeyDown(e);
		}

		void SetupGrid()
		{
			var columns = Math.Max(1, Math.Min((int)Math.Ceiling(Math.Sqrt(Concurrency - 1)), 8));
			var rows = (Concurrency + columns - 1) / columns + 1;

			for (var column = 0; column < columns; ++column)
				progressGrid.ColumnDefinitions.Add(new ColumnDefinition());

			for (var row = 0; row < rows; ++row)
				progressGrid.RowDefinitions.Add(new RowDefinition());

			OverallTask = new RunTask { Name = "Overall", Maximum = Items.Count };
			for (var ctr = 0; ctr < Concurrency; ++ctr)
				AddProgress(ctr / columns + 1, ctr % columns);
			AddProgress(0, 0, columns).RunTask = OverallTask;
		}

		void UpdateGrid()
		{
			OverallTask.Value = Finished;
			foreach (var progressData in ProgressDatas)
			{
				var runTask = progressData.RunTask;
				if (runTask == null)
					progressData.UIElement.Visibility = Visibility.Hidden;
				else
				{
					progressData.TextBlock.Text = runTask.Name;
					progressData.UIElement.Visibility = Visibility.Visible;
					if (runTask.Maximum == 0)
						progressData.ProgressBar.Visibility = Visibility.Hidden;
					else
					{
						progressData.ProgressBar.Visibility = Visibility.Visible;
						progressData.ProgressBar.Value = runTask.Value;
						progressData.ProgressBar.Maximum = runTask.Maximum;
						OverallTask.Value += runTask.Value / runTask.Maximum;
					}
				}
			}
		}

		ProgressData AddProgress(int row, int column, int columnSpan = 1)
		{
			var textBlock = new TextBlock { HorizontalAlignment = HorizontalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
			var progressBar = new ProgressBar { Height = 10, Minimum = 0, Margin = new Thickness(10, 0, 10, 0) };

			var sp = new StackPanel();
			sp.Children.Add(textBlock);
			sp.Children.Add(progressBar);
			Grid.SetRow(sp, row);
			Grid.SetColumn(sp, column);
			Grid.SetColumnSpan(sp, columnSpan);
			progressGrid.Children.Add(sp);

			var progressData = new ProgressData(sp, textBlock, progressBar);
			ProgressDatas.Add(progressData);
			return progressData;
		}

		void OnCancel(object sender = null, RoutedEventArgs e = null)
		{
			token.Cancel();
			if (Exception == null)
				Exception = new OperationCanceledException();
		}

		static public List<TResult> RunAsync<TSource, TResult>(Window parent, string title, IEnumerable<TSource> items, Func<TSource, IProgress<ProgressReport>, CancellationToken, Task<TResult>> getTask, Func<TSource, string> getName = null, int concurrency = DefaultConcurrency)
		{
			var objItems = items.Cast<object>().ToList();
			if (!objItems.Any())
				return new List<TResult>();
			Func<object, IProgress<ProgressReport>, CancellationToken, Task<object>> objGetTask = async (item, progress, cancellationToken) => await getTask((TSource)item, progress, cancellationToken);
			Func<object, string> objGetName = getName == null ? (Func<object, string>)(item => item?.ToString() ?? "<NULL>") : item => getName((TSource)item);

			var dialog = new MultiProgressDialog(objItems, objGetTask, objGetName, concurrency) { Owner = parent, Title = title };
			dialog.ShowDialog();
			if (dialog.Exception != null)
				throw dialog.Exception;
			return dialog.Items.Select(item => dialog.Result.ContainsKey(item) ? (TResult)dialog.Result[item] : default(TResult)).ToList();
		}

		static public void RunAsync<TSource>(Window parent, string title, IEnumerable<TSource> items, Func<TSource, IProgress<ProgressReport>, CancellationToken, Task> getTask, Func<TSource, string> getName = null, int concurrency = DefaultConcurrency) => RunAsync(parent, title, items, async (item, progress, cancellationToken) => { await getTask(item, progress, cancellationToken); return default(object); }, getName, concurrency);

		static public List<TResult> Run<TSource, TResult>(Window parent, string title, IEnumerable<TSource> items, Func<TSource, IProgress<ProgressReport>, CancellationToken, TResult> getTask, Func<TSource, string> getName = null, int concurrency = DefaultConcurrency) => RunAsync(parent, title, items, (item, progress, cancellationToken) => Task.Run(() => getTask(item, progress, cancellationToken)), getName, concurrency);

		static public void Run<TSource>(Window parent, string title, IEnumerable<TSource> items, Action<TSource, IProgress<ProgressReport>, CancellationToken> getTask, Func<TSource, string> getName = null, int concurrency = DefaultConcurrency) => RunAsync(parent, title, items, (item, progress, isCanceled) => Task.Run(() => { getTask(item, progress, isCanceled); return default(object); }), getName, concurrency);
	}
}
