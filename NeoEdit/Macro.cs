using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Threading;
using System.Xml.Linq;
using Microsoft.Win32;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program
{
	public class Macro
	{
		static readonly Dictionary<NECommand, bool?> macroInclude;

		static Macro()
		{
			macroInclude = Helpers.GetValues<NECommand>().ToDictionary(command => command, command => typeof(NECommand).GetMember(command.ToString()).First().GetCustomAttributes(typeof(NoMacroAttribute), false).Cast<NoMacroAttribute>().Select(attr => attr.IncludeHandled ? default(bool?) : false).DefaultIfEmpty(true).First());
			macroInclude = new Dictionary<NECommand, bool?>();
			foreach (var command in Helpers.GetValues<NECommand>())
				switch (typeof(NECommand).GetMember(command.ToString()).First().GetCustomAttribute<NoMacroAttribute>()?.IncludeHandled)
				{
					case true: macroInclude[command] = null; break;
					case false: macroInclude[command] = false; break;
					case null: macroInclude[command] = true; break;
				}
		}


		bool stop = false;
		readonly List<ExecuteState> actions = new List<ExecuteState>();

		public void AddAction(ExecuteState state)
		{
			if (!(macroInclude[state.Command] ?? state.Handled))
				return;

			// Only save relevant fields
			state = new ExecuteState(state.Command)
			{
				PreExecuteData = state.PreExecuteData,
				Configuration = state.Configuration,
				ShiftDown = state.ShiftDown,
				ControlDown = state.ControlDown,
				AltDown = state.AltDown,
				MultiStatus = state.MultiStatus,
				Key = state.Key,
				Text = state.Text,
			};
			var last = actions.LastOrDefault();
			if ((state.Command == NECommand.Internal_Text) && (last?.Command == NECommand.Internal_Text))
				last.Text += state.Text;
			else
				actions.Add(state);
		}

		public void Play(TabsWindow tabsWindow, Action<Macro> setMacroPlaying, Action finished = null)
		{
			setMacroPlaying(this);
			var timer = new DispatcherTimer(DispatcherPriority.ApplicationIdle);
			var ctr = 0;
			timer.Tick += (s, e) => tabsWindow.Dispatcher.Invoke(() =>
			{
				try
				{
					if (stop)
						throw new Exception("Macro processing aborted.");

					if (ctr >= actions.Count)
					{
						timer.Stop();
						setMacroPlaying(null);
						if (finished != null)
							finished();
						return;
					}

					var action = actions[ctr++];
					tabsWindow.HandleCommand(action, true);
				}
				catch
				{
					timer.Stop();
					setMacroPlaying(null);
					throw;
				}
			});
			timer.Start();
		}

		public void Stop() => stop = true;

		public readonly static string MacroDirectory = Path.Combine(Helpers.NeoEditAppData, "Macro");

		public static string ChooseMacro()
		{
			var dialog = new OpenFileDialog
			{
				DefaultExt = "xml",
				Filter = "Macro files|*.xml|All files|*.*",
				InitialDirectory = MacroDirectory,
			};
			if (dialog.ShowDialog() != true)
				return null;
			return dialog.FileName;
		}

		public void Save(string fileName = null, bool macroDirRelative = false)
		{
			Directory.CreateDirectory(MacroDirectory);
			if (fileName == null)
			{
				var dialog = new SaveFileDialog
				{
					DefaultExt = "xml",
					Filter = "Macro files|*.xml|All files|*.*",
					FileName = "Macro.xml",
					InitialDirectory = MacroDirectory,
				};
				if (dialog.ShowDialog() != true)
					return;

				fileName = dialog.FileName;
			}
			else if (macroDirRelative)
				fileName = Path.Combine(MacroDirectory, fileName);

			XMLConverter.ToXML(this).Save(fileName);
		}

		public static Macro Load(string fileName = null, bool macroDirRelative = false)
		{
			if (fileName == null)
			{
				fileName = Macro.ChooseMacro();
				if (fileName == null)
					return null;
			}
			else if (macroDirRelative)
				fileName = Path.Combine(MacroDirectory, fileName);

			return XMLConverter.FromXML<Macro>(XElement.Load(fileName));
		}
	}
}
