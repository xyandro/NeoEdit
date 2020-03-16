using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using Build.BuildActions;

namespace Build
{
	partial class BuildWindow
	{
		static DependencyProperty ConfigurationsProperty = DependencyProperty.Register(nameof(Configurations), typeof(List<string>), typeof(BuildWindow));
		static DependencyProperty PlatformsProperty = DependencyProperty.Register(nameof(Platforms), typeof(List<string>), typeof(BuildWindow));
		static DependencyProperty ActionsProperty = DependencyProperty.Register(nameof(Actions), typeof(List<BaseAction>), typeof(BuildWindow));
		static DependencyProperty ProgressTextProperty = DependencyProperty.Register(nameof(ProgressText), typeof(string), typeof(BuildWindow));

		List<string> Configurations { get { return (List<string>)GetValue(ConfigurationsProperty); } set { SetValue(ConfigurationsProperty, value); } }
		List<string> Platforms { get { return (List<string>)GetValue(PlatformsProperty); } set { SetValue(PlatformsProperty, value); } }
		List<BaseAction> Actions { get { return (List<BaseAction>)GetValue(ActionsProperty); } set { SetValue(ActionsProperty, value); } }
		string ProgressText { get { return (string)GetValue(ProgressTextProperty); } set { SetValue(ProgressTextProperty, value); } }

		public BuildWindow()
		{
			InitializeComponent();

			Configurations = new List<string> { "Debug", "Release" };
			Platforms = new List<string> { "32", "64" };
			Actions = new List<BaseAction>
			{
				new VerifyCleanAction(),
				new UpdateAction(),
				new BuildAction(),
				new LoaderAction(),
				new CleanAction(),
				new DeleteReleasesAction(),
				new ReleaseAction(),
				new DeleteIgnoredAction(),
			};

			OnReset(null, null);

			ProgressText = "Select the desired options and then press \"Go\".\n";
		}

		void OnReset(object sender, RoutedEventArgs e)
		{
			configurations.SelectedItem = Configurations[1];
			platforms.SelectAll();
			actions.SelectAll();
		}

		void OnDeleteIgnored(object sender, RoutedEventArgs e) => Run(writeText => new DeleteIgnoredAction().Run(writeText, null, null));

		void EnableButtons(bool enabled)
		{
			deleteIgnoredButton.IsEnabled = enabled;
			goButton.IsEnabled = enabled;
		}

		void OnGo(object sender, RoutedEventArgs e)
		{
			// Preserve ordering
			var useConfiguration = (string)configurations.SelectedItem;
			var usePlatformsHash = new HashSet<string>(platforms.SelectedItems.Cast<string>());
			var usePlatforms = Platforms.Intersect(usePlatformsHash).ToList();
			var useActionsHash = new HashSet<BaseAction>(actions.SelectedItems.Cast<BaseAction>());
			var useActions = Actions.Intersect(useActionsHash).ToList();

			foreach (var action in useActions)
				if (!action.Prepare())
					return;

			Run(writeText =>
			{
				if (useConfiguration == null)
					throw new Exception("Please choose a configuration.");
				if (!usePlatforms.Any())
					throw new Exception("Please choose platforms.");
				if (!useActions.Any())
					throw new Exception("Nothing to do!");

				foreach (var action in useActions)
				{
					writeText($"Starting {action}...");
					var stopWatch = Stopwatch.StartNew();
					action.Run(writeText, useConfiguration, usePlatforms);
					stopWatch.Stop();
					writeText($"Finished {action} ({stopWatch.Elapsed.TotalSeconds} seconds).");
					writeText();
					Dispatcher.Invoke(() => actions.SelectedItems.Remove(action));
				}

				Dispatcher.Invoke(() => useActions.ForEach(action => actions.SelectedItems.Add(action)));
			});
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			if (!goButton.IsEnabled)
				e.Cancel = true;
			base.OnClosing(e);
		}

		void Run(Action<WriteTextDelegate> action)
		{
			EnableButtons(false);

			var text = "";
			var resetEvent = new AutoResetEvent(false);
			WriteTextDelegate writeText = str =>
			{
				if (str.StartsWith("\udead"))
				{
					var index = text.LastIndexOf('\n', Math.Max(0, text.Length - 2));
					if (index != -1)
						text = text.Substring(0, index + 1);
					str = str.Substring(1);
				}
				text += $"{str}\n";
				resetEvent.Set();
			};

			var bw = new BackgroundWorker();
			bw.DoWork += (s, e) =>
			{
				try
				{
					var stopWatch = Stopwatch.StartNew();
					action(writeText);
					stopWatch.Stop();
					writeText($"Done ({stopWatch.Elapsed.TotalSeconds} seconds).");
				}
				catch (Exception ex)
				{
					for (; ex != null; ex = ex.InnerException)
						writeText(ex.Message);
					writeText("Failed.");
				}
			};

			bw.RunWorkerCompleted += (s, e) => EnableButtons(true);
			bw.RunWorkerAsync();

			new Thread(() =>
			{
				var curText = default(string);
				while ((bw.IsBusy) || (curText != text))
				{
					if (bw.IsBusy)
						resetEvent.WaitOne(100);

					if (curText != text)
					{
						curText = text;
						Dispatcher.Invoke(() =>
						{
							ProgressText = curText;
							progressText.CaretIndex = curText.Length;
							progressText.ScrollToEnd();
						});
					}
				}
			}).Start();
		}
	}
}
