﻿using NeoEdit.Common.Transform;

namespace NeoEdit.Common.Configuration
{
	public class Configuration_Edit_Advanced_CompressDecompress : IConfiguration
	{
		public Coder.CodePage InputCodePage { get; set; }
		public Compressor.Type CompressorType { get; set; }
		public Coder.CodePage OutputCodePage { get; set; }
	}
}
