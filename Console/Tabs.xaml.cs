﻿using System.Collections.ObjectModel;
using System.Windows.Controls;
using NeoEdit.GUI.Common;

namespace NeoEdit.Console
{
	public class Tabs : Tabs<Console> { }

	public partial class ConsoleTabs
	{
		[DepProp]
		public ObservableCollection<Console> Consoles { get { return UIHelper<ConsoleTabs>.GetPropValue<ObservableCollection<Console>>(this); } set { UIHelper<ConsoleTabs>.SetPropValue(this, value); } }
		[DepProp]
		public Console Active { get { return UIHelper<ConsoleTabs>.GetPropValue<Console>(this); } set { UIHelper<ConsoleTabs>.SetPropValue(this, value); } }
		[DepProp]
		public Tabs.ViewType View { get { return UIHelper<ConsoleTabs>.GetPropValue<Tabs.ViewType>(this); } set { UIHelper<ConsoleTabs>.SetPropValue(this, value); } }

		static ConsoleTabs() { UIHelper<ConsoleTabs>.Register(); }

		public ConsoleTabs(string path = null)
		{
			ConsoleMenuItem.RegisterCommands(this, (s, e, command) => RunCommand(command));
			InitializeComponent();

			Consoles = new ObservableCollection<Console>();
			Add(new Console());
		}

		void Add(Console console)
		{
			Consoles.Add(console);
			Active = console;
		}

		void RunCommand(ConsoleCommand command)
		{
			switch (command)
			{
				case ConsoleCommand.File_New: Add(new Console()); break;
				case ConsoleCommand.View_Tiles: View = View == GUI.Common.Tabs<Console>.ViewType.Tiles ? GUI.Common.Tabs<Console>.ViewType.Tabs : GUI.Common.Tabs<Console>.ViewType.Tiles; break;
			}
		}

		Label GetLabel(Console console)
		{
			return console.GetLabel();
		}
	}
}
