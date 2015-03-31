using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Xml.Linq;
using NeoEdit.Common;
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
					case ActionType.Bit: return "Bit";
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

		internal XElement ToXML()
		{
			return new XElement("Action",
				new XAttribute("Type", Type),
				new XAttribute("CodePage", CodePage),
				new XAttribute("StringType", StringType),
				new XAttribute("Encoding", Encoding),
				new XAttribute("FixedLength", FixedLength),
				Model == null ? null : new XAttribute("Model", Model.GUID),
				new XAttribute("Location", Location ?? ""),
				new XAttribute("AlignmentBits", AlignmentBits),
				new XAttribute("Repeat", Repeat ?? ""),
				new XAttribute("SaveName", SaveName ?? "")
			);
		}

		static internal ModelAction FromXML(XElement xml)
		{
			var action = new ModelAction
			{
				Type = Helpers.ParseEnum<ActionType>(xml.Attribute("Type").Value),
				CodePage = Helpers.ParseEnum<Coder.CodePage>(xml.Attribute("CodePage").Value),
				StringType = Helpers.ParseEnum<ActionStringType>(xml.Attribute("StringType").Value),
				Encoding = Helpers.ParseEnum<Coder.CodePage>(xml.Attribute("Encoding").Value),
				FixedLength = int.Parse(xml.Attribute("FixedLength").Value),
				Location = xml.Attribute("Location").Value,
				AlignmentBits = int.Parse(xml.Attribute("AlignmentBits").Value),
				Repeat = xml.Attribute("Repeat").Value,
				SaveName = xml.Attribute("SaveName").Value,
			};
			if (String.IsNullOrWhiteSpace(action.Location))
				action.Location = null;
			if (String.IsNullOrWhiteSpace(action.Repeat))
				action.Repeat = null;
			if (String.IsNullOrWhiteSpace(action.SaveName))
				action.SaveName = null;
			return action;
		}

		internal void FromXML(XElement xml, IEnumerable<Model> models)
		{
			var modelAttr = xml.Attribute("Model");
			if (modelAttr != null)
			{
				var modelGUID = modelAttr.Value;
				Model = models.FirstOrDefault(model => model.GUID == modelGUID);
			}
		}

		internal ModelAction Copy()
		{
			return new ModelAction { Type = Type, CodePage = CodePage, StringType = StringType, Encoding = Encoding, FixedLength = FixedLength, Model = Model, Location = Location, AlignmentBits = AlignmentBits, Repeat = Repeat, SaveName = SaveName };
		}
	}
}
