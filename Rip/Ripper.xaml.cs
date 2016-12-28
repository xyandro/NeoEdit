using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NeoEdit.Common;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Dialogs;
using NeoEdit.Rip.Dialogs;

namespace NeoEdit.Rip
{
	partial class Ripper
	{
		[DepProp]
		ObservableCollection<RipItem> RipItems { get { return UIHelper<Ripper>.GetPropValue<ObservableCollection<RipItem>>(this); } set { UIHelper<Ripper>.SetPropValue(this, value); } }
		[DepProp]
		string OutputDirectory { get { return UIHelper<Ripper>.GetPropValue<string>(this); } set { UIHelper<Ripper>.SetPropValue(this, value); } }

		static Ripper() { UIHelper<Ripper>.Register(); }

		public Ripper()
		{
			RipMenuItem.RegisterCommands(this, (command, multiStatus) => RunCommand(command));
			InitializeComponent();
			RipItems = new ObservableCollection<RipItem>();
			OutputDirectory = Directory.GetCurrentDirectory();
		}

		void RunCommand(RipCommand command)
		{
			switch (command)
			{
				case RipCommand.File_Exit: Close(); break;
				case RipCommand.Add_CD: Command_Add_CD(); break;
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

		void OnRemoveClick(object sender = null, RoutedEventArgs e = null) => ripItems.SelectedItems.Cast<RipItem>().ToList().ForEach(item => RipItems.Remove(item));

		void OnSelectExistingClick(object sender, RoutedEventArgs e)
		{
			ripItems.UnselectAll();
			RipItems.Where(ripItem => File.Exists(ripItem.GetFileName(OutputDirectory))).ForEach(ripItem => ripItems.SelectedItems.Add(ripItem));
		}

		void OnGoClick(object sender = null, RoutedEventArgs e = null)
		{
			var directory = OutputDirectory;
			foreach (var ripItem in RipItems)
				ProgressDialog.Run(this, ripItem.ToString(), (cancelled, progress) => ripItem.Run(cancelled, progress, directory));
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
