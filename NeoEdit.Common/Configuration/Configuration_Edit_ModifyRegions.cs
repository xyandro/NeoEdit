using System.Collections.Generic;

namespace NeoEdit.Common.Configuration
{
	public class Configuration_Edit_ModifyRegions : IConfiguration
	{
		public enum Actions
		{
			Select_Select,
			Select_Previous,
			Select_Next,
			Select_Enclosing,
			Select_WithEnclosing,
			Select_WithoutEnclosing,
			Modify_Set,
			Modify_Clear,
			Modify_Remove,
			Modify_Add,
			Modify_Unite,
			Modify_Intersect,
			Modify_Exclude,
			Modify_Repeat,
			Copy_Enclosing,
			Copy_EnclosingIndex,
			Transform_Flatten,
			Transform_Transpose,
			Transform_RotateLeft,
			Transform_RotateRight,
			Transform_Rotate180,
			Transform_MirrorHorizontal,
			Transform_MirrorVertical,
		}

		public List<int> Regions { get; set; }
		public Actions Action { get; set; }
	}
}
