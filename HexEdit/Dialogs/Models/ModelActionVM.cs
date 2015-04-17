using System;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NeoEdit.Common.Transform;
using NeoEdit.HexEdit.Models;

namespace NeoEdit.HexEdit.Dialogs.Models
{
	class ModelActionVM : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged<T>(Expression<Func<ModelActionVM, T>> expression)
		{
			string name = ((expression.Body as MemberExpression).Member as PropertyInfo).Name;
			var handler = PropertyChanged;
			if (handler != null)
				handler(this, new PropertyChangedEventArgs(name));
		}

		ModelAction.ActionType type;
		public ModelAction.ActionType Type { get { return type; } set { type = value; OnPropertyChanged(a => a.Type); } }
		Coder.CodePage codePage;
		public Coder.CodePage CodePage { get { return codePage; } set { codePage = value; OnPropertyChanged(a => a.CodePage); } }
		ModelAction.ActionStringType stringType;
		public ModelAction.ActionStringType StringType { get { return stringType; } set { stringType = value; OnPropertyChanged(a => a.StringType); } }
		Coder.CodePage encoding;
		public Coder.CodePage Encoding { get { return encoding; } set { encoding = value; OnPropertyChanged(a => a.Encoding); } }
		string fixedWidth;
		public string FixedWidth { get { return fixedWidth; } set { fixedWidth = value; OnPropertyChanged(a => a.FixedWidth); } }
		string model;
		public string Model { get { return model; } set { model = value; OnPropertyChanged(a => a.Model); } }
		string location;
		public string Location { get { return location; } set { location = value; OnPropertyChanged(a => a.Location); } }
		int alignmentBits;
		public int AlignmentBits { get { return alignmentBits; } set { alignmentBits = value; OnPropertyChanged(a => a.AlignmentBits); } }
		ModelAction.ActionRepeatType repeatType;
		public ModelAction.ActionRepeatType RepeatType { get { return repeatType; } set { repeatType = value; OnPropertyChanged(a => a.RepeatType); } }
		string repeat;
		public string Repeat { get { return repeat; } set { repeat = value; OnPropertyChanged(a => a.Repeat); } }
		string saveName;
		public string SaveName { get { return saveName; } set { saveName = value; OnPropertyChanged(a => a.SaveName); } }
		public string Description { get { return action.Description(modelDataVM.Models.Select(model => model.model)); } set { OnPropertyChanged(a => a.Description); } }

		public readonly ModelDataVM modelDataVM;
		public readonly ModelAction action;
		public ModelActionVM(ModelDataVM modelDataVM, ModelAction action)
		{
			this.modelDataVM = modelDataVM;
			this.action = action;
			Type = action.Type;
			CodePage = action.CodePage;
			StringType = action.StringType;
			Encoding = action.Encoding;
			FixedWidth = action.FixedWidth;
			Model = action.Model;
			Location = action.Location;
			AlignmentBits = action.AlignmentBits;
			RepeatType = action.RepeatType;
			Repeat = action.Repeat;
			SaveName = action.SaveName;
			Description = null; // Flag for reevaluation
		}

		public void Save()
		{
			action.Type = Type;
			action.CodePage = CodePage;
			action.StringType = StringType;
			action.Encoding = Encoding;
			action.FixedWidth = FixedWidth;
			action.Model = Model;
			action.Location = Location;
			action.AlignmentBits = AlignmentBits;
			action.RepeatType = RepeatType;
			action.Repeat = Repeat;
			action.SaveName = SaveName;
			Description = null; // Flag for reevaluation
		}

		public bool EditDialog()
		{
			return ModelActionView.Run(modelDataVM, this);
		}
	}
}
