using System;
using System.Collections.Generic;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public static class NEAllFiles
	{
		static NEAllFiles()
		{
			neAllFilesData = new NEAllFilesData();
			neAllFilesData.allNEFiles = new OrderedHashSet<NEFiles>();
		}

		static void EnsureInTransaction()
		{
			if (!inTransaction)
				throw new Exception("Must start transaction before editing data");
		}

		static bool inTransaction = false;
		static NEAllFilesData saveNEAllFilesData, neAllFilesData;

		public static IReadOnlyOrderedHashSet<NEFiles> AllNEFiles
		{
			get => neAllFilesData.allNEFiles;
			set
			{
				EnsureInTransaction();
				neAllFilesData.allNEFiles = value;
			}
		}

		public static void SetAllNEFiles(IEnumerable<NEFiles> allNEFiles) => AllNEFiles = new OrderedHashSet<NEFiles>(allNEFiles);

		public static void BeginTransaction()
		{
			if (inTransaction)
				throw new Exception("Already in a transaction");

			inTransaction = true;
			saveNEAllFilesData = neAllFilesData;
			neAllFilesData = neAllFilesData.Clone();

			AllNEFiles.ForEach(neFiles => neFiles.BeginTransaction());
		}

		public static void Rollback()
		{
			EnsureInTransaction();
			AllNEFiles.ForEach(neFiles => neFiles.Rollback());
			neAllFilesData = saveNEAllFilesData;
			result = null;
			inTransaction = false;
		}

		public static void Commit()
		{
			EnsureInTransaction();
			foreach (var neFiles in AllNEFiles)
			{
				neFiles.Commit();

				if (neFiles.result.Clipboard != null)
					CreateResult().SetClipboard(neFiles.result.Clipboard);

				if (neFiles.result.KeysAndValues != null)
					CreateResult().SetKeysAndValues(neFiles.result.KeysAndValues);

				if (neFiles.result.DragFiles != null)
					CreateResult().SetDragFiles(neFiles.result.DragFiles);

				neFiles.ClearResult();
			}

			inTransaction = false;
		}

		public static NEAllFilesResult result { get; private set; }
		static NEAllFilesResult CreateResult()
		{
			if (result == null)
				result = new NEAllFilesResult();
			return result;
		}

		public static void ClearResult() => result = null;

		public static void AddNewFiles(NEFiles neFiles) => SetAllNEFiles(AllNEFiles.Concat(neFiles));

		public static void RemoveFiles(NEFiles neFiles) => SetAllNEFiles(AllNEFiles.Except(neFiles));
	}
}
