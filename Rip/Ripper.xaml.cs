using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NeoEdit.Common;
using NeoEdit.GUI.Controls;
using NeoEdit.Rip.Dialogs;

namespace NeoEdit.Rip
{
	partial class Ripper
	{
		[DepProp]
		ObservableCollection<RipItem> RipItems { get { return UIHelper<Ripper>.GetPropValue<ObservableCollection<RipItem>>(this); } set { UIHelper<Ripper>.SetPropValue(this, value); } }

		YouTube youTube = new YouTube();
		static Ripper() { UIHelper<Ripper>.Register(); }

		public Ripper()
		{
			RipMenuItem.RegisterCommands(this, (command, multiStatus) => RunCommand(command));
			InitializeComponent();
			RipItems = new ObservableCollection<RipItem>();
		}

		protected override void OnClosed(EventArgs e)
		{
			youTube.Dispose();
			base.OnClosed(e);
		}

		void RunCommand(RipCommand command)
		{
			switch (command)
			{
				case RipCommand.File_Exit: Close(); break;
				case RipCommand.Add_CD: Command_Add_CD(); break;
				case RipCommand.Add_YouTube: Command_Add_YouTube(); break;
			}
		}

		void Command_Add_CD()
		{
			using (var drive = AddCDDialog.Run(this))
			{
				if (drive == null)
					return;

				foreach (var track in drive.GetTracks())
					RipItems.Add(track);
			}
		}

		void Command_Add_YouTube()
		{
			var result = AddYouTubeDialog.Run(this, youTube);
			if (result == null)
				return;

			foreach (var item in result)
				RipItems.Add(item);
		}

		void OnRemoveClick(object sender = null, RoutedEventArgs e = null) => ripItems.SelectedItems.Cast<RipItem>().ToList().ForEach(item => RipItems.Remove(item));

		void OnSelectExistingClick(object sender, RoutedEventArgs e)
		{
			ripItems.UnselectAll();
			RipItems.Where(ripItem => File.Exists(ripItem.GetFileName(RipDirectory))).ForEach(ripItem => ripItems.SelectedItems.Add(ripItem));
		}

		void OnInvertClick(object sender, RoutedEventArgs e)
		{
			var newItems = RipItems.Except(ripItems.SelectedItems.Cast<RipItem>()).ToList();
			ripItems.SelectedItems.Clear();
			foreach (var item in newItems)
				ripItems.SelectedItems.Add(item);
		}

		void OnGoClick(object sender = null, RoutedEventArgs e = null)
		{
			var directory = RipDirectory;
			MultiProgressDialog.RunAsync(this, "Ripping...", RipItems, async (item, progress, cancelled) => await item.Run(progress, cancelled, directory));
		}

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Delete: OnRemoveClick(); break;
			}
			base.OnPreviewKeyDown(e);
		}
	}
}
