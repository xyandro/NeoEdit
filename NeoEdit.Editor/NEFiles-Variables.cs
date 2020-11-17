﻿using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	partial class NEFiles
	{
		void EnsureInTransaction()
		{
			if (state == null)
				throw new Exception("Must start transaction before editing data");
		}

		FilesList oldFilesList, newFilesList;
		FilesList GetUpdateFilesList()
		{
			EnsureInTransaction();
			if (newFilesList == oldFilesList)
				newFilesList = new FilesList(newFilesList);
			return newFilesList;
		}

		public IReadOnlyOrderedHashSet<NEFileHandler> AllFiles => newFilesList.AllFiles;

		void InsertFile(NEFileHandler neFile, int? index = null)
		{
			lock (this)
				GetUpdateFilesList().InsertFile(oldFilesList, neFile, index);
		}

		public void RemoveFile(NEFileHandler neFile)
		{
			lock (this)
				GetUpdateFilesList().RemoveFile(neFile);
		}

		public void MoveFile(NEFileHandler neFile, int index)
		{
			lock (this)
				GetUpdateFilesList().MoveFile(neFile, index);
		}

		public IReadOnlyOrderedHashSet<NEFileHandler> ActiveFiles => newFilesList.ActiveFiles;

		public void ClearAllActive() => GetUpdateFilesList().ClearActive();

		public void SetActive(NEFileHandler neFile, bool active = true) => GetUpdateFilesList().SetActive(neFile, active);

		public bool IsActive(NEFileHandler neFile) => newFilesList.IsActive(neFile);

		public NEFileHandler Focused
		{
			get => newFilesList.Focused;
			set => GetUpdateFilesList().Focused = value;
		}

		HashSet<NEFileHandler> transactionFiles;
		public void AddToTransaction(NEFileHandler neFile)
		{
			if (transactionFiles.Contains(neFile))
				return;
			transactionFiles.Add(neFile);
			neFile.BeginTransaction(state);
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

		public void BeginTransaction(EditorExecuteState state)
		{
			if (this.state != null)
				throw new Exception("Already in a transaction");
			this.state = state;
			transactionFiles = new HashSet<NEFileHandler>();
			ActiveFiles.ForEach(AddToTransaction);
		}

		void Rollback()
		{
			EnsureInTransaction();

			newFilesList = oldFilesList;
			newWindowLayout = oldWindowLayout;
			newActiveOnly = oldActiveOnly;
			newMacroVisualize = oldMacroVisualize;

			transactionFiles.ForEach(neFile => neFile.Rollback());
			transactionFiles = null;

			state = null;
		}

		public void Commit()
		{
			EnsureInTransaction();

			if (oldFilesList != newFilesList)
			{
				oldFilesList.AllFiles.Null(neFile => neFile.NEFiles).Where(neFile => !newFilesList.Contains(neFile)).ForEach(neFile => neFile.Closed());
				var now = DateTime.Now;
				newFilesList.ActiveFiles.ForEach(neFile => neFile.LastActive = now);
				oldFilesList = newFilesList;
			}
			oldWindowLayout = newWindowLayout;
			oldActiveOnly = newActiveOnly;
			oldMacroVisualize = newMacroVisualize;

			transactionFiles.ForEach(neFile => neFile.Commit());
			transactionFiles = null;

			state = null;
		}
	}
}
