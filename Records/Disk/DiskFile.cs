using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NeoEdit.Common;

namespace NeoEdit.Records.Disk
{
	public class DiskFile : DiskRecord
	{
		public DiskFile(string uri)
			: base(uri)
		{
			var fileInfo = new FileInfo(FullName);
			if (fileInfo.Exists)
			{
				this[RecordProperty.PropertyName.Size] = fileInfo.Length;
				this[RecordProperty.PropertyName.WriteTime] = fileInfo.LastWriteTimeUtc;
				this[RecordProperty.PropertyName.CreateTime] = fileInfo.CreationTimeUtc;
				this[RecordProperty.PropertyName.AccessTime] = fileInfo.LastAccessTimeUtc;
			}
		}

		public override System.Collections.Generic.IEnumerable<RecordAction.ActionName> Actions
		{
			get
			{
				return new List<RecordAction.ActionName> { 
					RecordAction.ActionName.MD5,
					RecordAction.ActionName.Identify,
					RecordAction.ActionName.Open,
				}.Concat(base.Actions);
			}
		}

		public override bool IsFile { get { return true; } }

		protected override void SetProperty<T>(RecordProperty.PropertyName property, T value)
		{
			base.SetProperty<T>(property, value);
			switch (property)
			{
				case RecordProperty.PropertyName.Name:
					this[RecordProperty.PropertyName.NameWoExtension] = Path.GetFileNameWithoutExtension(value as string);
					this[RecordProperty.PropertyName.Extension] = Path.GetExtension(value as string);
					break;
			}
		}

		public override void Delete()
		{
			File.Delete(FullName);
		}

		public override void CalcMD5()
		{
			using (var md5 = MD5.Create())
			using (var stream = File.OpenRead(FullName))
				this[RecordProperty.PropertyName.MD5] = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
		}

		public override void Identify()
		{
			this[RecordProperty.PropertyName.Identify] = Identifier.Identify(FullName);
		}

		public override BinaryData Read()
		{
			return File.ReadAllBytes(FullName);
		}

		public override void Write(BinaryData data)
		{
			File.WriteAllBytes(FullName, data.GetAllBytes());
		}

		protected override void SyncCopy(Record source)
		{
			if (source is DiskFile)
			{
				File.Copy(source.FullName, FullName, true);
				return;
			}

			base.SyncCopy(source);
		}
	}
}
