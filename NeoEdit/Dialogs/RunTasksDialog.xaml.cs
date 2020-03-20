using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace NeoEdit.Program.Dialogs
{
	partial class RunTasksDialog
	{
		const int Columns = 3;
		const int Rows = 3;
		const int Concurrency = Columns * Rows;

		public class UIData
		{
			public UIElement UIElement { get; set; }
			public TextBlock Text { get; set; }
			public ProgressBar ProgressBar { get; set; }
		}

		class Task
		{
			public object item;
			public Func<object, ProgressData, object> func;
			public Result result;
			public int resultIndex;
		}

		class Result
		{
			public object[] results;
			public Action<IEnumerable<object>> action;
		}

		List<Thread> threads;
		DispatcherTimer drawTimer;
		Exception exception;

		readonly ProgressData[] progress = Enumerable.Range(0, Concurrency).Select(x => new ProgressData()).ToArray();
		readonly UIData[] uiData = new UIData[Concurrency + 1];
		readonly List<Result> result = new List<Result>();

		List<Task> tasks = new List<Task>();
		int nextTask;

		public RunTasksDialog()
		{
			InitializeComponent();

			for (var column = 0; column < Columns; ++column)
				tasksGrid.ColumnDefinitions.Add(new ColumnDefinition());

			for (var row = 0; row <= Rows; ++row)
				tasksGrid.RowDefinitions.Add(new RowDefinition());

			for (var row = 0; row < Rows; ++row)
				for (var column = 0; column < Columns; ++column)
					uiData[row * Columns + column] = CreateUIData(row + 1, column);

			uiData[Concurrency] = CreateUIData(0, 0, Columns);
			uiData[Concurrency].Text.Text = "Overall";
			uiData[Concurrency].UIElement.Visibility = Visibility.Visible;
		}

		UIData CreateUIData(int row, int column, int columnSpan = 1)
		{
			var textBlock = new TextBlock { HorizontalAlignment = HorizontalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
			var progressBar = new ProgressBar { Height = 10, Minimum = 0, Maximum = 100, Margin = new Thickness(10, 0, 10, 0) };

			var stackPanel = new StackPanel { Visibility = Visibility.Hidden };
			stackPanel.Children.Add(textBlock);
			stackPanel.Children.Add(progressBar);
			Grid.SetRow(stackPanel, row);
			Grid.SetColumn(stackPanel, column);
			Grid.SetColumnSpan(stackPanel, columnSpan);
			tasksGrid.Children.Add(stackPanel);

			return new UIData { UIElement = stackPanel, ProgressBar = progressBar, Text = textBlock };
		}

		public void AddTasks<TSource>(IEnumerable<TSource> items, Action<TSource> taskFunc) => AddTasks(items, (item, index, progress) => { taskFunc(item); return 0; }, list => { });

		public void AddTasks<TSource>(IEnumerable<TSource> items, Action<TSource, int> taskFunc) => AddTasks(items, (item, index, progress) => { taskFunc(item, index); return 0; }, list => { });

		public void AddTasks<TSource>(IEnumerable<TSource> items, Action<TSource, ProgressData> taskFunc) => AddTasks(items, (item, index, progress) => { taskFunc(item, progress); return 0; }, list => { });

		public void AddTasks<TSource>(IEnumerable<TSource> items, Action<TSource, int, ProgressData> taskFunc) => AddTasks(items, (item, index, progress) => { taskFunc(item, index, progress); return 0; }, list => { });

		public void AddTasks<TSource, TResult>(IEnumerable<TSource> items, Func<TSource, TResult> taskFunc, Action<List<TResult>> finished) => AddTasks(items, (item, index, progress) => taskFunc(item), finished);

		public void AddTasks<TSource, TResult>(IEnumerable<TSource> items, Func<TSource, int, TResult> taskFunc, Action<List<TResult>> finished) => AddTasks(items, (item, index, progress) => taskFunc(item, index), finished);

		public void AddTasks<TSource, TResult>(IEnumerable<TSource> items, Func<TSource, ProgressData, TResult> taskFunc, Action<List<TResult>> finished) => AddTasks(items, (item, index, progress) => taskFunc(item, progress), finished);

		public void AddTasks<TSource, TResult>(IEnumerable<TSource> items, Func<TSource, int, ProgressData, TResult> taskFunc, Action<List<TResult>> finished)
		{
			lock (tasks)
			{
				var result = new Result { action = objs => finished(objs.Cast<TResult>().ToList()) };
				this.result.Add(result);
				var index = 0;
				foreach (var item in items)
				{
					tasks.Add(new Task
					{
						item = item,
						func = (obj, progress) => taskFunc((TSource)obj, index, progress),
						result = result,
						resultIndex = index++,
					});
				}
				result.results = new object[index];
			}
		}

		void RunThread(ProgressData progress)
		{
			while (!progress.Cancel)
			{
				int next;
				lock (tasks)
				{
					if (nextTask >= tasks.Count)
						break;
					next = nextTask++;
				}

				var task = tasks[next];
				progress.Name = "Task";
				progress.Percent = 0;
				progress.Done = false;
				try { task.result.results[task.resultIndex] = task.func(task.item, progress); }
				catch (Exception ex) { Cancel(ex); }
				progress.Done = true;
			}
		}

		void OnTimerTick(object sender, EventArgs e)
		{
			var running = 0;
			for (var ctr = 0; ctr < Concurrency; ++ctr)
			{
				var progress = this.progress[ctr];
				var uiData = this.uiData[ctr];
				if (progress.Done)
					uiData.UIElement.Visibility = Visibility.Hidden;
				else
				{
					++running;
					uiData.UIElement.Visibility = Visibility.Visible;
					uiData.Text.Text = progress.Name;
					uiData.ProgressBar.Value = progress.Percent;
				}
			}
			uiData[Concurrency].ProgressBar.Value = nextTask - running;
			if ((progress[0].Cancel) || ((nextTask >= tasks.Count) && (running == 0)))
			{
				drawTimer.Stop();
				threads.ForEach(thread => thread.Join());
				DialogResult = true;
			}
		}

		void Cancel(Exception ex)
		{
			exception = exception ?? ex;
			progress.ForEach(x => x.Cancel = true);
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			if (DialogResult != true)
			{
				Cancel(new OperationCanceledException());
				e.Cancel = true;
			}
			base.OnClosing(e);
		}

		public void Run()
		{
			uiData[Concurrency].ProgressBar.Maximum = tasks.Count;

			threads = Enumerable.Range(0, Concurrency).Select(x => new Thread(() => RunThread(progress[x]))).ToList();
			threads.ForEach(thread => thread.Start());

			drawTimer = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromMilliseconds(25) };
			drawTimer.Tick += OnTimerTick;
			drawTimer.IsEnabled = true;
			drawTimer.Start();

			ShowDialog();

			if (exception != null)
				throw exception;

			result.ForEach(result => result.action(result.results));
		}
	}
}
