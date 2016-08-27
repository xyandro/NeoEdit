using NeoEdit.GUI.Controls;

namespace NeoEdit.ImageEdit
{
	class ImageEditMenuItem : NEMenuItem<ImageEditCommand> { }
	class MultiMenuItem : MultiMenuItem<ImageEditor, ImageEditCommand> { }

	partial class ImageEditMenu
	{
		[DepProp]
		public new ImageEditTabs Parent { get { return UIHelper<ImageEditMenu>.GetPropValue<ImageEditTabs>(this); } set { UIHelper<ImageEditMenu>.SetPropValue(this, value); } }

		static ImageEditMenu() { UIHelper<ImageEditMenu>.Register(); }

		public ImageEditMenu() { InitializeComponent(); }
	}
}
