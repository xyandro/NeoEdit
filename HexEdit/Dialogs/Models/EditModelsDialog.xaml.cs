using System.ComponentModel;
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

		protected override void OnClosing(CancelEventArgs e)
		{
			if (ModelData.Modified)
			{
				switch (new Message
				{
					Title = "Save models",
					Text = "The models have been changed.  Would you like to save them?",
					Options = Message.OptionsEnum.YesNoCancel,
					DefaultAccept = Message.OptionsEnum.Yes,
					DefaultCancel = Message.OptionsEnum.Cancel,
				}.Show())
				{
					case Message.OptionsEnum.Yes:
						if (!SaveModels())
						{
							e.Cancel = true;
							return;
						}
						break;
					case Message.OptionsEnum.No: break;
					case Message.OptionsEnum.Cancel: e.Cancel = true; break;
				}
			}
		}

		void NewModel(object sender, RoutedEventArgs e)
		{
			var model = EditModelDialog.Run(ModelData, new Model());
			if (model == null)
				return;

			ModelData.Models.Add(model);
			ModelData.Modified = true;
		}

		void EditModel(object sender, RoutedEventArgs e)
		{
			var model = models.SelectedItem as Model;
			if (model == null)
				return;

			model = EditModelDialog.Run(ModelData, model);
			if (model == null)
				return;

			ModelData.Models[models.SelectedIndex] = model;
			ModelData.Modified = true;
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
			ModelData.Modified = true;
		}

		void SaveModels(object sender, RoutedEventArgs e)
		{
			SaveModels();
		}

		bool SaveModels()
		{
			var dialog = new SaveFileDialog
			{
				DefaultExt = "xml",
				Filter = "Model files|*.xml|All files|*.*",
				FileName = ModelData.FileName,
			};
			if (dialog.ShowDialog() != true)
				return false;

			ModelData.FileName = dialog.FileName;
			ModelData.ToXML().Save(ModelData.FileName);
			ModelData.Modified = false;
			return true;
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
			ModelData.Modified = false;
		}

		public static ModelData Run(ModelData modelData)
		{
			var dialog = new EditModelsDialog(modelData.Copy());
			return dialog.ShowDialog() == true ? dialog.ModelData : null;
		}
	}
}
