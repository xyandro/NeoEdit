using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.Dialogs;
using NeoEdit.HexEdit.Models;

namespace NeoEdit.HexEdit.Dialogs.Models
{
	partial class EditModelsDialog
	{
		[DepProp]
		public ObservableCollection<Model> Models { get { return UIHelper<EditModelsDialog>.GetPropValue<ObservableCollection<Model>>(this); } set { UIHelper<EditModelsDialog>.SetPropValue(this, value); } }

		static EditModelsDialog() { UIHelper<EditModelsDialog>.Register(); }

		EditModelsDialog(IEnumerable<Model> models)
		{
			InitializeComponent();

			Models = new ObservableCollection<Model>(models);
			Models.Add(new Model { ModelName = "Test" });
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		void NewModel(object sender, RoutedEventArgs e)
		{
			var model = EditModelDialog.Run(Models, new Model());
			if (model != null)
				Models.Add(model);
		}

		void EditModel(object sender, RoutedEventArgs e)
		{
			var model = models.SelectedItem as Model;
			if (model == null)
				return;

			model = EditModelDialog.Run(Models, model);
			if (model != null)
				Models[models.SelectedIndex] = model;
		}

		void EditModel(object sender, MouseButtonEventArgs e)
		{
			EditModel(null, (RoutedEventArgs)null);
		}

		void DeleteModel(object sender, RoutedEventArgs e)
		{
			if (models.SelectedIndex == -1)
				return;

			if (new Message
			{
				Title = "Please confirm",
				Text = "Are you sure you want to delete this model?",
				Options = Message.OptionsEnum.YesNo,
				DefaultAccept = Message.OptionsEnum.Yes,
				DefaultCancel = Message.OptionsEnum.No,
			}.Show() != Message.OptionsEnum.Yes)
				return;

			Models.RemoveAt(models.SelectedIndex);
		}

		public static IEnumerable<Model> Run(IEnumerable<Model> models)
		{
			var dialog = new EditModelsDialog(models);
			return dialog.ShowDialog() == true ? dialog.Models : null;
		}
	}
}
