﻿using NeoEdit.Common.Transform;

namespace NeoEdit.Common.Models
{
	public class EditDataHashDialogResult
	{
		public Coder.CodePage CodePage { get; set; }
		public Hasher.Type HashType { get; set; }
		public byte[] HMACKey { get; set; }
	}
}