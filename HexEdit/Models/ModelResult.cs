using System;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.HexEdit.Models
{
	class ModelResult : DependencyObject
	{
		[DepProp]
		public ModelAction Action { get { return UIHelper<ModelResult>.GetPropValue<ModelAction>(this); } set { UIHelper<ModelResult>.SetPropValue(this, value); } }
		[DepProp]
		public int Num { get { return UIHelper<ModelResult>.GetPropValue<int>(this); } set { UIHelper<ModelResult>.SetPropValue(this, value); } }
		[DepProp]
		public string Name { get { return UIHelper<ModelResult>.GetPropValue<string>(this); } set { UIHelper<ModelResult>.SetPropValue(this, value); } }
		[DepProp]
		public string Value { get { return UIHelper<ModelResult>.GetPropValue<string>(this); } set { UIHelper<ModelResult>.SetPropValue(this, value); } }
		[DepProp]
		public long StartByte { get { return UIHelper<ModelResult>.GetPropValue<long>(this); } set { UIHelper<ModelResult>.SetPropValue(this, value); } }
		[DepProp]
		public int StartBit { get { return UIHelper<ModelResult>.GetPropValue<int>(this); } set { UIHelper<ModelResult>.SetPropValue(this, value); } }
		[DepProp]
		public long EndByte { get { return UIHelper<ModelResult>.GetPropValue<long>(this); } set { UIHelper<ModelResult>.SetPropValue(this, value); } }
		[DepProp]
		public int EndBit { get { return UIHelper<ModelResult>.GetPropValue<int>(this); } set { UIHelper<ModelResult>.SetPropValue(this, value); } }
		[DepProp]
		public string Location { get { return UIHelper<ModelResult>.GetPropValue<string>(this); } set { UIHelper<ModelResult>.SetPropValue(this, value); } }
		[DepProp]
		public string Length { get { return UIHelper<ModelResult>.GetPropValue<string>(this); } set { UIHelper<ModelResult>.SetPropValue(this, value); } }

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
				Location += $".{startBit}";
			Location += $" - {endByte}";
			if (endBit != 0)
				Location += $".{endBit}";

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
				length += $".{endBit - startBit}";
			return length;
		}

		public override string ToString() => $"{Value}: {StartByte}.{StartBit} - {EndByte}.{EndBit}";
	}
}
