using System;
using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.HexEdit.Models
{
	class ModelResult : DependencyObject
	{
		[DepProp]
		public ModelAction Action { get { return UIHelper<ModelResult>.GetPropValue(() => this.Action); } set { UIHelper<ModelResult>.SetPropValue(() => this.Action, value); } }
		[DepProp]
		public int Num { get { return UIHelper<ModelResult>.GetPropValue(() => this.Num); } set { UIHelper<ModelResult>.SetPropValue(() => this.Num, value); } }
		[DepProp]
		public string Name { get { return UIHelper<ModelResult>.GetPropValue(() => this.Name); } set { UIHelper<ModelResult>.SetPropValue(() => this.Name, value); } }
		[DepProp]
		public string Value { get { return UIHelper<ModelResult>.GetPropValue(() => this.Value); } set { UIHelper<ModelResult>.SetPropValue(() => this.Value, value); } }
		[DepProp]
		public long StartByte { get { return UIHelper<ModelResult>.GetPropValue(() => this.StartByte); } set { UIHelper<ModelResult>.SetPropValue(() => this.StartByte, value); } }
		[DepProp]
		public int StartBit { get { return UIHelper<ModelResult>.GetPropValue(() => this.StartBit); } set { UIHelper<ModelResult>.SetPropValue(() => this.StartBit, value); } }
		[DepProp]
		public long EndByte { get { return UIHelper<ModelResult>.GetPropValue(() => this.EndByte); } set { UIHelper<ModelResult>.SetPropValue(() => this.EndByte, value); } }
		[DepProp]
		public int EndBit { get { return UIHelper<ModelResult>.GetPropValue(() => this.EndBit); } set { UIHelper<ModelResult>.SetPropValue(() => this.EndBit, value); } }
		[DepProp]
		public string Location { get { return UIHelper<ModelResult>.GetPropValue(() => this.Location); } set { UIHelper<ModelResult>.SetPropValue(() => this.Location, value); } }
		[DepProp]
		public string Length { get { return UIHelper<ModelResult>.GetPropValue(() => this.Length); } set { UIHelper<ModelResult>.SetPropValue(() => this.Length, value); } }

		static ModelResult() { UIHelper<ModelResult>.Register(); }

		public ModelResult(ModelAction action, int num, string name, string value, long startByte, int startBit, long endByte, int endBit)
		{
			Action = action;
			Num = num;
			Name = name;
			Value = value;
			StartByte = startByte;
			StartBit = startBit;
			EndByte = endByte;
			EndBit = endBit;

			Location = "";
			Location += startByte;
			if (startBit != 0)
				Location += "." + startBit;
			Location += " - " + endByte;
			if (endBit != 0)
				Location += "." + endBit;

			Length = GetLength(startByte, startBit, endByte, endBit);
		}

		static string GetLength(long startByte, int startBit, long endByte, int endBit)
		{
			while (startBit > endBit)
			{
				endBit += 8;
				--endByte;
			}
			var length = (endByte - startByte).ToString();
			if (endBit - startBit != 0)
				length += "." + (endBit - startBit);
			return length;
		}

		public override string ToString()
		{
			return String.Format("{1}: {2}.{3} - {4}.{5}", Name, Value, StartByte, StartBit, EndByte, EndBit);
		}
	}
}
