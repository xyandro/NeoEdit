using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using NeoEdit.Common;

namespace NeoEdit.UI.Dialogs
{
	partial class TaskRunnerDialog
	{
		public class UIData
		{
			public UIElement UIElement { get; set; }
			public TextBlock Text { get; set; }
			public ProgressBar ProgressBar { get; set; }
		}
		List<UIData> uiDatas = null;

		readonly IReadOnlyList<TaskProgress> progresses;
		readonly EventWaitHandle waitHandle;

		TaskRunnerDialog(IReadOnlyList<TaskProgress> progresses, EventWaitHandle waitHandle)
		{
			this.progresses = progresses;
			this.waitHandle = waitHandle;

			InitializeComponent();

			CreateUI();
			StartTimer();
		}

		void CreateUI()
		{
			var columns = (int)(Math.Sqrt(progresses.Count - 1) + 0.5);
			var rows = (progresses.Count - 1 + columns - 1) / columns + 1;

			for (var column = 0; column < columns; ++column)
				tasksGrid.ColumnDefinitions.Add(new ColumnDefinition());

			for (var row = 0; row < rows; ++row)
				tasksGrid.RowDefinitions.Add(new RowDefinition());

			uiDatas = new List<UIData>();
			uiDatas.Add(CreateUIData(0, 0, columns));

			for (var ctr = 1; ctr < progresses.Count; ++ctr)
				uiDatas.Add(CreateUIData((ctr - 1) / columns + 1, (ctr - 1) % columns));
		}

		UIData CreateUIData(int row, int column, int columnSpan = 1)
		{
			var textBlock = new TextBlock { HorizontalAlignment = HorizontalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
			var progressBar = new ProgressBar { Height = 10, Minimum = 0, Maximum = 1, Margin = new Thickness(10, 0, 10, 0) };

			var stackPanel = new StackPanel { Visibility = Visibility.Hidden };
			stackPanel.Children.Add(textBlock);
			stackPanel.Children.Add(progressBar);
			Grid.SetRow(stackPanel, row);
			Grid.SetColumn(stackPanel, column);
			Grid.SetColumnSpan(stackPanel, columnSpan);
			tasksGrid.Children.Add(stackPanel);

			return new UIData { UIElement = stackPanel, ProgressBar = progressBar, Text = textBlock };
		}

		void StartTimer()
		{
			var drawTimer = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromMilliseconds(100), IsEnabled = true };
			drawTimer.Tick += (s, e) =>
			{
				if (waitHandle.WaitOne(0))
				{
					drawTimer.Stop();
					DialogResult = true;
					return;
				}

				for (var ctr = 0; ctr < progresses.Count; ++ctr)
					UpdateProgress(progresses[ctr], uiDatas[ctr]);
			};
			drawTimer.Start();
		}

		void UpdateProgress(TaskProgress progress, UIData uiData)
		{
			if (!progress.Working)
			{
				uiData.UIElement.Visibility = Visibility.Hidden;
				return;
			}

			uiData.UIElement.Visibility = Visibility.Visible;
			uiData.Text.Text = progress.Name;

			var percent = progress.Percent;
			if (!percent.HasValue)
				uiData.ProgressBar.Visibility = Visibility.Hidden;
			else
			{
				uiData.ProgressBar.Visibility = Visibility.Visible;
				uiData.ProgressBar.Value = percent.Value;
			}
		}

		public static bool Run(Window window, IReadOnlyList<TaskProgress> progresses, EventWaitHandle waitHandle) => new TaskRunnerDialog(progresses, waitHandle) { Owner = window }.ShowDialog();
	}
}
