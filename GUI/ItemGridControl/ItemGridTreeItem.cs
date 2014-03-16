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

		public ItemGridTreeItem<ItemType> GetChild(string fullName)
		{
			if (fullName == "")
				return this;

			var parts = fullName.Split('\\').ToList();
			for (var ctr1 = 0; ctr1 < parts.Count - 1; ++ctr1)
				parts[ctr1 + 1] = parts[ctr1] + @"\" + parts[ctr1 + 1];

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
