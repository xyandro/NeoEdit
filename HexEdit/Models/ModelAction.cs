using System;
using System.Windows;
using System.Xml.Linq;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Common;

namespace NeoEdit.HexEdit.Models
{
	class ModelAction : DependencyObject
	{
		public enum ActionType
		{
			None,
			Bit,
			BasicType,
			String,
			Unused,
			Model,
		}

		public enum ActionStringType
		{
			StringWithLength,
			StringNullTerminated,
			StringFixedWidth,
		}

		[DepProp]
		public ActionType Type { get { return UIHelper<ModelAction>.GetPropValue<ActionType>(this); } set { UIHelper<ModelAction>.SetPropValue(this, value); } }
		[DepProp]
		public Coder.CodePage CodePage { get { return UIHelper<ModelAction>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<ModelAction>.SetPropValue(this, value); } } // For basic type: codepage; for varstring: length type
		[DepProp]
		public ActionStringType StringType { get { return UIHelper<ModelAction>.GetPropValue<ActionStringType>(this); } set { UIHelper<ModelAction>.SetPropValue(this, value); } }
		[DepProp]
		public Coder.CodePage Encoding { get { return UIHelper<ModelAction>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<ModelAction>.SetPropValue(this, value); } }
		[DepProp]
		public int FixedLength { get { return UIHelper<ModelAction>.GetPropValue<int>(this); } set { UIHelper<ModelAction>.SetPropValue(this, value); } } // For fixed-length string or unused
		[DepProp]
		public Model Model { get { return UIHelper<ModelAction>.GetPropValue<Model>(this); } set { UIHelper<ModelAction>.SetPropValue(this, value); } }
		[DepProp]
		public string Location { get { return UIHelper<ModelAction>.GetPropValue<string>(this); } set { UIHelper<ModelAction>.SetPropValue(this, value); } }
		[DepProp]
		public int AlignmentBits { get { return UIHelper<ModelAction>.GetPropValue<int>(this); } set { UIHelper<ModelAction>.SetPropValue(this, value); } }
		[DepProp]
		public string Repeat { get { return UIHelper<ModelAction>.GetPropValue<string>(this); } set { UIHelper<ModelAction>.SetPropValue(this, value); } }
		[DepProp]
		public string SaveName { get { return UIHelper<ModelAction>.GetPropValue<string>(this); } set { UIHelper<ModelAction>.SetPropValue(this, value); } }

		public string Description
		{
			get
			{
				switch (Type)
				{
					case ActionType.BasicType: return CodePage.ToString();
					case ActionType.String:
						{
							var encoding = Coder.GetDescription(Encoding, true);
							switch (StringType)
							{
								case ActionStringType.StringWithLength: return String.Format("Variable length string ({0} length, {1})", CodePage.ToString(), encoding);
								case ActionStringType.StringNullTerminated: return String.Format("Null-terminated string ({0})", encoding);
								case ActionStringType.StringFixedWidth: return String.Format("Fixed width string ({0} bytes, {1})", FixedLength, encoding);
							}
						}
						break;
					case ActionType.Unused: return String.Format("Unused ({0} bytes)", FixedLength);
					case ActionType.Model: return Model.ModelName;
				}
				return "Unknown";
			}
		}

		static ModelAction() { UIHelper<ModelAction>.Register(); }

		public ModelAction()
		{
			Type = ActionType.None;
			CodePage = Coder.CodePage.Int32LE;
			StringType = ActionStringType.StringWithLength;
			Encoding = Coder.CodePage.UTF8;
			FixedLength = 0;
			AlignmentBits = 8;
			Location = "[Current]";
			Repeat = "1";
			SaveName = null;
		}

		internal string ToXML()
		{
			return XMLConverter.ToXML(this).ToString();
		}

		internal static ModelAction FromXML(string str)
		{
			return XMLConverter.FromXML<ModelAction>(XElement.Parse(str));
		}

		internal ModelAction Copy()
		{
			return new ModelAction { Type = Type, CodePage = CodePage, StringType = StringType, Encoding = Encoding, FixedLength = FixedLength, Model = Model, Location = Location, AlignmentBits = AlignmentBits, Repeat = Repeat, SaveName = SaveName };
		}
	}
}
