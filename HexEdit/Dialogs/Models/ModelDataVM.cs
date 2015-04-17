using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NeoEdit.GUI.Dialogs;
using NeoEdit.HexEdit.Models;

namespace NeoEdit.HexEdit.Dialogs.Models
{
	class ModelDataVM : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged<T>(Expression<Func<ModelDataVM, T>> expression)
		{
			string name = ((expression.Body as MemberExpression).Member as PropertyInfo).Name;
			var handler = PropertyChanged;
			if (handler != null)
				handler(this, new PropertyChangedEventArgs(name));
		}

		ObservableCollection<ModelVM> models;
		public ObservableCollection<ModelVM> Models { get { return models; } set { models = value; OnPropertyChanged(a => a.Models); } }
		ModelVM _default;
		public ModelVM Default { get { return _default; } set { _default = value; OnPropertyChanged(a => a.Default); } }
		string fileName;
		public string FileName { get { return fileName; } set { fileName = value; OnPropertyChanged(a => a.FileName); } }

		public readonly ModelData modelData;
		public ModelDataVM(ModelData modelData)
		{
			this.modelData = modelData;
			Models = new ObservableCollection<ModelVM>(modelData.Models.Select(model => new ModelVM(this, model)));
			Default = Models.FirstOrDefault(model => model.model.GUID == modelData.Default);
			FileName = modelData.FileName;
		}

		public void Save()
		{
			modelData.Models.Clear();
			modelData.Models.AddRange(Models.Select(model => model.model));
			modelData.Default = Default == null ? null : Default.model.GUID;
			modelData.FileName = FileName;
		}

		public void NewModel()
		{
			var model = new ModelVM(this, new Model());
			if (model.EditDialog())
				Models.Add(model);
		}

		public void EditModel(ModelVM model)
		{
			if (model != null)
				model.EditDialog();
		}

		public void DeleteModel(IEnumerable<ModelVM> models)
		{
			if (!models.Any())
				return;

			if (new Message
			{
				Title = "Please confirm",
				Text = "Are you sure you want to delete these models?",
				Options = Message.OptionsEnum.YesNo,
				DefaultAccept = Message.OptionsEnum.Yes,
				DefaultCancel = Message.OptionsEnum.No,
			}.Show() != Message.OptionsEnum.Yes)
				return;

			foreach (var model in models)
				Models.Remove(model);
		}

		public void SetDefault(ModelVM model)
		{
			if (model != null)
				Default = model;
		}

		public bool EditDialog()
		{
			return ModelDataView.Run(this);
		}

		public ModelVM GetModelVM(string GUID)
		{
			return Models.FirstOrDefault(model => model.model.GUID == GUID);
		}
	}
}
