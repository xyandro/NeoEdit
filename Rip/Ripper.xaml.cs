using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NeoEdit.Common;
using NeoEdit.Common.NEClipboards;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Dialogs;
using NeoEdit.Rip.Dialogs;

namespace NeoEdit.Rip
{
	partial class Ripper
	{
		[DepProp]
		ObservableCollection<RipItem> RipItems { get { return UIHelper<Ripper>.GetPropValue<ObservableCollection<RipItem>>(this); } set { UIHelper<Ripper>.SetPropValue(this, value); } }

		static Ripper() { UIHelper<Ripper>.Register(); }

		public Ripper()
		{
			RipMenuItem.RegisterCommands(this, (command, multiStatus) => RunCommand(command));
			InitializeComponent();
			RipItems = new ObservableCollection<RipItem>();
		}

		void RunCommand(RipCommand command)
		{
			switch (command)
			{
				case RipCommand.File_Exit: Close(); break;
				case RipCommand.Edit_CopyTitles: Command_Edit_CopyTitles(); break;
				case RipCommand.Edit_CopyFileNames: Command_Edit_CopyFileNames(); break;
				case RipCommand.Add_CD: Command_Add_CD(); break;
			}
		}

		void Command_Edit_CopyTitles() => NEClipboard.Current = NEClipboard.CreateStrings(ripItems.SelectedItems.Cast<RipItem>().Select(item => item.Title).ToList());

		void Command_Edit_CopyFileNames() => NEClipboard.Current = NEClipboard.CreateStrings(ripItems.SelectedItems.Cast<RipItem>().Select(item => item.FileName).ToList());

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
