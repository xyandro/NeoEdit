using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using Microsoft.Win32;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.Dialogs;
using NeoEdit.HexEdit.Models;

namespace NeoEdit.HexEdit.Dialogs.Models
{
	partial class EditModelsDialog
	{
		[DepProp]
		public ModelData ModelData { get { return UIHelper<EditModelsDialog>.GetPropValue<ModelData>(this); } set { UIHelper<EditModelsDialog>.SetPropValue(this, value); } }

		static EditModelsDialog() { UIHelper<EditModelsDialog>.Register(); }

		EditModelsDialog(ModelData modelData)
		{
			InitializeComponent();

			ModelData = modelData;
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		void NewModel(object sender, RoutedEventArgs e)
		{
			var model = EditModelDialog.Run(ModelData, new Model());
			if (model != null)
				ModelData.Models.Add(model);
		}

		void EditModel(object sender, RoutedEventArgs e)
		{
			var model = models.SelectedItem as Model;
			if (model == null)
				return;

			model = EditModelDialog.Run(ModelData, model);
			if (model != null)
				ModelData.Models[models.SelectedIndex] = model;
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

			ModelData.Models.RemoveAt(models.SelectedIndex);
		}

		void SaveModels(object sender, RoutedEventArgs e)
		{
			var dialog = new SaveFileDialog
			{
				DefaultExt = "xml",
				Filter = "Model files|*.xml|All files|*.*",
				FileName = ModelData.FileName,
			};
			if (dialog.ShowDialog() != true)
				return;

			ModelData.FileName = dialog.FileName;
			ModelData.ToXML().Save(ModelData.FileName);
		}

		void LoadModels(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog
			{
				DefaultExt = "xml",
				Filter = "Model files|*.xml|All files|*.*",
				FileName = ModelData.FileName,
			};
			if (dialog.ShowDialog() != true)
				return;

			ModelData = ModelData.FromXML(XElement.Load(dialog.FileName));
			ModelData.FileName = dialog.FileName;
		}

		public static ModelData Run(ModelData modelData)
		{
			var dialog = new EditModelsDialog(modelData.Copy());
			return dialog.ShowDialog() == true ? dialog.ModelData : null;
		}
	}
}
