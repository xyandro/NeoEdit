using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	partial class NEFilesHandler
	{
		void EnsureInTransaction()
		{
			if (!inTransaction)
				throw new Exception("Must start transaction before editing data");
		}

		IReadOnlyOrderedHashSet<NEFileHandler> oldAllFiles, newAllFiles;
		public IReadOnlyOrderedHashSet<NEFileHandler> AllFiles
		{
			get => newAllFiles;
			set
			{
				EnsureInTransaction();
				newAllFiles = value;
			}
		}

		IReadOnlyOrderedHashSet<NEFileHandler> oldActiveFiles, newActiveFiles;
		public IReadOnlyOrderedHashSet<NEFileHandler> ActiveFiles
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

		public void ClearActiveFiles() => ActiveFiles = new OrderedHashSet<NEFileHandler>();

		public void SetActiveFile(NEFileHandler file) => ActiveFiles = new OrderedHashSet<NEFileHandler> { file };

		public void SetActiveFiles(IEnumerable<NEFileHandler> files) => ActiveFiles = new OrderedHashSet<NEFileHandler>(files);

		NEFileHandler oldFocused, newFocused;
		public NEFileHandler Focused
		{
			get => newFocused;
			set
			{
				EnsureInTransaction();
				newFocused = value;
			}
		}

		HashSet<NEFileHandler> transactionFiles;
		public void AddToTransaction(NEFileHandler neFile)
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

		public NEFilesHandlerResult result { get; private set; }
		NEFilesHandlerResult CreateResult()
		{
			if (result == null)
				result = new NEFilesHandlerResult();
			return result;
		}

		public void AddNewFile(NEFileHandler neFile) => CreateResult().AddNewFile(neFile);

		public void BeginTransaction()
		{
			if (inTransaction)
				throw new Exception("Already in a transaction");
			inTransaction = true;
			transactionFiles = new HashSet<NEFileHandler>();
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
