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

		NEFilesData saveFilesData, filesData;

		public IReadOnlyOrderedHashSet<NEFile> AllFiles
		{
			get => filesData.allFiles;
			set
			{
				EnsureInTransaction();
				filesData.allFiles = value;
			}
		}

		public IReadOnlyOrderedHashSet<NEFile> ActiveFiles
		{
			get => filesData.activeFiles;
			set
			{
				EnsureInTransaction();
				filesData.activeFiles = value;
				if (!filesData.activeFiles.Contains(Focused))
					Focused = filesData.activeFiles.FirstOrDefault();
			}
		}

		public void ClearActiveFiles() => ActiveFiles = new OrderedHashSet<NEFile>();

		public void SetActiveFile(NEFile file) => ActiveFiles = new OrderedHashSet<NEFile> { file };

		public void SetActiveFiles(IEnumerable<NEFile> files) => ActiveFiles = new OrderedHashSet<NEFile>(files);

		public NEFile Focused
		{
			get => filesData.focused;
			set
			{
				EnsureInTransaction();
				filesData.focused = value;
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

		public WindowLayout WindowLayout
		{
			get => filesData.windowLayout;
			set
			{
				EnsureInTransaction();
				filesData.windowLayout = value;
			}
		}

		public bool ActiveOnly
		{
			get => filesData.activeOnly;
			set
			{
				EnsureInTransaction();
				filesData.activeOnly = value;
			}
		}

		public bool MacroVisualize
		{
			get => filesData.macroVisualize;
			set
			{
				EnsureInTransaction();
				filesData.macroVisualize = value;
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
			saveFilesData = filesData;
			filesData = filesData.Clone();
			transactionFiles = new HashSet<NEFile>();
			ActiveFiles.ForEach(AddToTransaction);
		}

		void Rollback()
		{
			EnsureInTransaction();

			filesData = saveFilesData;

			transactionFiles.ForEach(neFile => neFile.Rollback());
			transactionFiles = null;

			inTransaction = false;
			result = null;
		}

		public void Commit()
		{
			EnsureInTransaction();

			transactionFiles.ForEach(neFile => neFile.Commit());
			transactionFiles = null;

			inTransaction = false;
			result = null;
		}
	}
}
