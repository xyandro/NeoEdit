using System.Linq;
using System.Windows;
using System.Windows.Input;
using NeoEdit.GUI.Controls;

namespace NeoEdit.HexEdit.Dialogs.Models
{
	partial class ModelDataView
	{
		[DepProp]
		public ModelDataVM ModelDataVM { get { return UIHelper<ModelDataView>.GetPropValue<ModelDataVM>(this); } set { UIHelper<ModelDataView>.SetPropValue(this, value); } }

		static ModelDataView() { UIHelper<ModelDataView>.Register(); }

		ModelDataView(ModelDataVM modelDataVM)
		{
			InitializeComponent();
			ModelDataVM = modelDataVM;
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			ModelDataVM.Save();
			DialogResult = true;
		}

		void NewModel(object sender, RoutedEventArgs e)
		{
			ModelDataVM.NewModel();
		}

		void EditModel(object sender, RoutedEventArgs e)
		{
			ModelDataVM.EditModel(models.SelectedItem as ModelVM);
		}

		void EditModel(object sender, MouseButtonEventArgs e)
		{
			ModelDataVM.EditModel(models.SelectedItem as ModelVM);
		}

		void DeleteModel(object sender, RoutedEventArgs e)
		{
			ModelDataVM.DeleteModel(models.SelectedItems.Cast<ModelVM>().ToList());
		}

		void DefaultModel(object sender, RoutedEventArgs e)
		{
			ModelDataVM.SetDefault(models.SelectedItem as ModelVM);
		}

		static public bool Run(ModelDataVM modelDataVM)
		{
			return new ModelDataView(modelDataVM).ShowDialog() == true;
		}
	}
}
