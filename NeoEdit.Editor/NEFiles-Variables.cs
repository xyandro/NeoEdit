using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	partial class NEFiles
	{
		void EnsureInTransaction()
		{
			if (!inTransaction)
				throw new Exception("Must start transaction before editing data");
		}

		IReadOnlyOrderedHashSet<NEFile> oldAllFiles, newAllFiles;
		public IReadOnlyOrderedHashSet<NEFile> AllFiles
		{
			get => newAllFiles;
			set
			{
				EnsureInTransaction();
				newAllFiles = value;
			}
		}

		IReadOnlyOrderedHashSet<NEFile> oldActiveFiles, newActiveFiles;
		public IReadOnlyOrderedHashSet<NEFile> ActiveFiles
		{
			get => newActiveFiles;
			set
			{
				EnsureInTransaction();
				newActiveFiles = value;
				if (!newActiveFiles.Contains(Focused))
					Focused = newActiveFiles.FirstOrDefault();
			}
		}

		public void ClearActiveFiles() => ActiveFiles = new OrderedHashSet<NEFile>();

		public void SetActiveFile(NEFile file) => ActiveFiles = new OrderedHashSet<NEFile> { file };

		public void SetActiveFiles(IEnumerable<NEFile> files) => ActiveFiles = new OrderedHashSet<NEFile>(files);

		NEFile oldFocused, newFocused;
		public NEFile Focused
		{
			get => newFocused;
			set
			{
				EnsureInTransaction();
				newFocused = value;
			}
		}

		HashSet<NEFile> transactionFiles;
		public void AddToTransaction(NEFile neFile)
		{
			if (transactionFiles.Contains(neFile))
				return;
			transactionFiles.Add(neFile);
			neFile.BeginTransaction();
		}

		WindowLayout oldWindowLayout, newWindowLayout;
		public WindowLayout WindowLayout
		{
			get => newWindowLayout;
			set
			{
				EnsureInTransaction();
				newWindowLayout = value;
			}
		}

		bool oldActiveOnly, newActiveOnly;
		public bool ActiveOnly
		{
			get => newActiveOnly;
			set
			{
				EnsureInTransaction();
				newActiveOnly = value;
			}
		}

		bool oldMacroVisualize = true, newMacroVisualize = true;
		public bool MacroVisualize
		{
			get => newMacroVisualize;
			set
			{
				EnsureInTransaction();
				newMacroVisualize = value;
			}
		}

		public NEFilesResult result { get; private set; }
		NEFilesResult CreateResult()
		{
			if (result == null)
				result = new NEFilesResult();
			return result;
		}

		public void AddNewFile(NEFile neFile) => CreateResult().AddNewFile(neFile);

		public void BeginTransaction()
		{
			if (inTransaction)
				throw new Exception("Already in a transaction");
			inTransaction = true;
			transactionFiles = new HashSet<NEFile>();
			ActiveFiles.ForEach(AddToTransaction);
		}

		void Rollback()
		{
			EnsureInTransaction();

			newAllFiles = oldAllFiles;
			newActiveFiles = oldActiveFiles;
			newFocused = oldFocused;
			newWindowLayout = oldWindowLayout;
			newActiveOnly = oldActiveOnly;
			newMacroVisualize = oldMacroVisualize;

			transactionFiles.ForEach(neFile => neFile.Rollback());
			transactionFiles = null;

			inTransaction = false;
			result = null;
		}

		public void Commit()
		{
			EnsureInTransaction();

			oldAllFiles = newAllFiles;
			oldActiveFiles = newActiveFiles;
			oldFocused = newFocused;
			oldWindowLayout = newWindowLayout;
			oldActiveOnly = newActiveOnly;
			oldMacroVisualize = newMacroVisualize;

			transactionFiles.ForEach(neFile => neFile.Commit());
			transactionFiles = null;

			inTransaction = false;
			result = null;
		}
	}
}
