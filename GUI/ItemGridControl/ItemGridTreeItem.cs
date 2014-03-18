using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.GUI.Common;

namespace NeoEdit.GUI.ItemGridControl
{
	public interface IItemGridTreeItem : IItemGridItem
	{
		string FullName { get; }
		IItemGridTreeItem GetParent();
		bool CanGetChildren();
		IEnumerable<IItemGridTreeItem> GetChildren();
	}

	public abstract class ItemGridTreeItem<ItemType> : ItemGridItem<ItemType>, IItemGridTreeItem
	{
		[DepProp]
		public string FullName { get { return GetValue<string>(); } private set { SetValue(value); } }

		public abstract IItemGridTreeItem GetParent();
		public virtual bool CanGetChildren() { return false; }
		public virtual IEnumerable<IItemGridTreeItem> GetChildren() { throw new NotImplementedException(); }

		protected ItemGridTreeItem(string FullName)
		{
			this.FullName = FullName;
		}

		protected virtual string GetPath(string fullName)
		{
			if (fullName == "")
				return "";

			var idx = fullName.LastIndexOf('\\');
			if (idx == -1)
				return "";

			return fullName.Substring(0, idx);
		}

		public ItemGridTreeItem<ItemType> GetChild(string fullName)
		{
			if (fullName == "")
				return this;

			var parts = new List<string>();
			while (fullName != "")
			{
				parts.Insert(0, fullName);
				fullName = GetPath(fullName);
			}

			var result = this;
			foreach (var part in parts)
			{
				if ((result == null) || (!result.CanGetChildren()))
					return null;
				result = result.GetChildren().FirstOrDefault(child => (child as ItemGridTreeItem<ItemType>).FullName.Equals(part, StringComparison.InvariantCultureIgnoreCase)) as ItemGridTreeItem<ItemType>;
			}

			return result;
		}
	}
}
