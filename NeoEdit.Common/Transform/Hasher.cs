using System;
using System.IO;
using Classless.Hasher;
using Classless.Hasher.Mac;

namespace NeoEdit.Common.Transform
{
	public static class Hasher
	{
		public enum Type
		{
			None,
			QuickHash,
			Adler32,
			APHash,
			BkdrHash,
			Cksum,
			Crc_8Bit,
			Crc_8BitIcode,
			Crc_8BitItu,
			Crc_8BitMaxim,
			Crc_8BitWcdma,
			Crc_16Bit,
			Crc_16BitCcitt,
			Crc_16BitKermit,
			Crc_16BitMaxim,
			Crc_16BitUsb,
			Crc_16BitArc,
			Crc_16BitIbm,
			Crc_16BitLha,
			Crc_16BitCcittFalse,
			Crc_16BitX25,
			Crc_16BitXkermit,
			Crc_16BitXmodem,
			Crc_16BitZmodem,
			Crc_24Bit,
			Crc_24BitOpenPgp,
			Crc_32Bit,
			Crc_32BitBzip2,
			Crc_32BitPkzip,
			Crc_32BitIscsi,
			Crc_32BitItu,
			Crc_32BitJam,
			Crc_32BitMpeg2,
			Crc_32BitPosix,
			Crc_32BitCksum,
			Crc_64Bit,
			Crc_64BitWE,
			Crc_64BitIso,
			Crc_64BitJones,
			Dha256,
			DjbHash,
			ElfHash,
			Fcs16,
			Fcs32,
			Fletcher_8,
			Fletcher_16,
			Fletcher_32,
			FNV_32_0,
			FNV_32_1,
			FNV_32_1a,
			FNV_64_0,
			FNV_64_1,
			FNV_64_1a,
			GHash3,
			GHash5,
			GOST,
			HAS160,
			HAVAL_3_128,
			HAVAL_4_128,
			HAVAL_5_128,
			HAVAL_3_160,
			HAVAL_4_160,
			HAVAL_5_160,
			HAVAL_3_192,
			HAVAL_4_192,
			HAVAL_5_192,
			HAVAL_3_224,
			HAVAL_4_224,
			HAVAL_5_224,
			HAVAL_3_256,
			HAVAL_4_256,
			HAVAL_5_256,
			JenkinsHash,
			JSHash,
			MD2,
			MD4,
			MD5,
			Panama,
			Pjw32,
			RIPEMD128,
			RIPEMD160,
			RIPEMD256,
			RIPEMD320,
			RSHash,
			SdbmHash,
			SHA0,
			SHA1,
			SHA224,
			SHA256,
			SHA384,
			SHA512,
			Snefru_4_128,
			Snefru_4_256,
			Snefru_8_128,
			Snefru_8_256,
			Sum_8,
			Sum_16,
			Sum_24,
			Sum_32,
			Sum_64,
			SumBsd,
			SumSysV,
			Tiger_3_128,
			Tiger_3_160,
			Tiger_3_192,
			Tiger2_128,
			Tiger2_160,
			Tiger2_192,
			Whirlpool,
			Xum32,
			Xor8,
		}

		static System.Security.Cryptography.HashAlgorithm GetHashAlgorithm(Type type)
		{
			switch (type)
			{
				case Type.Adler32: return new Adler32();
				case Type.APHash: return new APHash();
				case Type.BkdrHash: return new BkdrHash();
				case Type.Cksum: return new Cksum();
				case Type.Crc_8Bit: return new Crc(CrcParameters.GetParameters(CrcStandard.Crc8Bit));
				case Type.Crc_8BitIcode: return new Crc(CrcParameters.GetParameters(CrcStandard.Crc8BitIcode));
				case Type.Crc_8BitItu: return new Crc(CrcParameters.GetParameters(CrcStandard.Crc8BitItu));
				case Type.Crc_8BitMaxim: return new Crc(CrcParameters.GetParameters(CrcStandard.Crc8BitMaxim));
				case Type.Crc_8BitWcdma: return new Crc(CrcParameters.GetParameters(CrcStandard.Crc8BitWcdma));
				case Type.Crc_16Bit: return new Crc(CrcParameters.GetParameters(CrcStandard.Crc16Bit));
				case Type.Crc_16BitCcitt: return new Crc(CrcParameters.GetParameters(CrcStandard.Crc16BitCcitt));
				case Type.Crc_16BitKermit: return new Crc(CrcParameters.GetParameters(CrcStandard.Crc16BitKermit));
				case Type.Crc_16BitMaxim: return new Crc(CrcParameters.GetParameters(CrcStandard.Crc16BitMaxim));
				case Type.Crc_16BitUsb: return new Crc(CrcParameters.GetParameters(CrcStandard.Crc16BitUsb));
				case Type.Crc_16BitArc: return new Crc(CrcParameters.GetParameters(CrcStandard.Crc16BitArc));
				case Type.Crc_16BitIbm: return new Crc(CrcParameters.GetParameters(CrcStandard.Crc16BitIbm));
				case Type.Crc_16BitLha: return new Crc(CrcParameters.GetParameters(CrcStandard.Crc16BitLha));
				case Type.Crc_16BitCcittFalse: return new Crc(CrcParameters.GetParameters(CrcStandard.Crc16BitCcittFalse));
				case Type.Crc_16BitX25: return new Crc(CrcParameters.GetParameters(CrcStandard.Crc16BitX25));
				case Type.Crc_16BitXkermit: return new Crc(CrcParameters.GetParameters(CrcStandard.Crc16BitXkermit));
				case Type.Crc_16BitXmodem: return new Crc(CrcParameters.GetParameters(CrcStandard.Crc16BitXmodem));
				case Type.Crc_16BitZmodem: return new Crc(CrcParameters.GetParameters(CrcStandard.Crc16BitZmodem));
				case Type.Crc_24Bit: return new Crc(CrcParameters.GetParameters(CrcStandard.Crc24Bit));
				case Type.Crc_24BitOpenPgp: return new Crc(CrcParameters.GetParameters(CrcStandard.Crc24BitOpenPgp));
				case Type.Crc_32Bit: return new Crc(CrcParameters.GetParameters(CrcStandard.Crc32Bit));
				case Type.Crc_32BitBzip2: return new Crc(CrcParameters.GetParameters(CrcStandard.Crc32BitBzip2));
				case Type.Crc_32BitPkzip: return new Crc(CrcParameters.GetParameters(CrcStandard.Crc32BitPkzip));
				case Type.Crc_32BitIscsi: return new Crc(CrcParameters.GetParameters(CrcStandard.Crc32BitIscsi));
				case Type.Crc_32BitItu: return new Crc(CrcParameters.GetParameters(CrcStandard.Crc32BitItu));
				case Type.Crc_32BitJam: return new Crc(CrcParameters.GetParameters(CrcStandard.Crc32BitJam));
				case Type.Crc_32BitMpeg2: return new Crc(CrcParameters.GetParameters(CrcStandard.Crc32BitMpeg2));
				case Type.Crc_32BitPosix: return new Crc(CrcParameters.GetParameters(CrcStandard.Crc32BitPosix));
				case Type.Crc_32BitCksum: return new Crc(CrcParameters.GetParameters(CrcStandard.Crc32BitCksum));
				case Type.Crc_64Bit: return new Crc(CrcParameters.GetParameters(CrcStandard.Crc64Bit));
				case Type.Crc_64BitWE: return new Crc(CrcParameters.GetParameters(CrcStandard.Crc64BitWE));
				case Type.Crc_64BitIso: return new Crc(CrcParameters.GetParameters(CrcStandard.Crc64BitIso));
				case Type.Crc_64BitJones: return new Crc(CrcParameters.GetParameters(CrcStandard.Crc64BitJones));
				case Type.Dha256: return new Dha256();
				case Type.DjbHash: return new DjbHash();
				case Type.ElfHash: return new ElfHash();
				case Type.Fcs16: return new Fcs16();
				case Type.Fcs32: return new Fcs32();
				case Type.Fletcher_8: return new Fletcher(FletcherParameters.GetParameters(FletcherStandard.Fletcher8Bit));
				case Type.Fletcher_16: return new Fletcher(FletcherParameters.GetParameters(FletcherStandard.Fletcher16Bit));
				case Type.Fletcher_32: return new Fletcher(FletcherParameters.GetParameters(FletcherStandard.Fletcher32Bit));
				case Type.FNV_32_0: return new Fnv(FnvParameters.GetParameters(FnvStandard.Fnv32BitType0));
				case Type.FNV_32_1: return new Fnv(FnvParameters.GetParameters(FnvStandard.Fnv32BitType1));
				case Type.FNV_32_1a: return new Fnv(FnvParameters.GetParameters(FnvStandard.Fnv32BitType1A));
				case Type.FNV_64_0: return new Fnv(FnvParameters.GetParameters(FnvStandard.Fnv64BitType0));
				case Type.FNV_64_1: return new Fnv(FnvParameters.GetParameters(FnvStandard.Fnv64BitType1));
				case Type.FNV_64_1a: return new Fnv(FnvParameters.GetParameters(FnvStandard.Fnv64BitType1A));
				case Type.GHash3: return new GHash(GHashParameters.GetParameters(GHashStandard.GHash3));
				case Type.GHash5: return new GHash(GHashParameters.GetParameters(GHashStandard.GHash5));
				case Type.GOST: return new GostHash();
				case Type.HAS160: return new Has160();
				case Type.HAVAL_3_128: return new Haval(HavalParameters.GetParameters(HavalStandard.Haval128Bit3Pass));
				case Type.HAVAL_4_128: return new Haval(HavalParameters.GetParameters(HavalStandard.Haval128Bit4Pass));
				case Type.HAVAL_5_128: return new Haval(HavalParameters.GetParameters(HavalStandard.Haval128Bit5Pass));
				case Type.HAVAL_3_160: return new Haval(HavalParameters.GetParameters(HavalStandard.Haval160Bit3Pass));
				case Type.HAVAL_4_160: return new Haval(HavalParameters.GetParameters(HavalStandard.Haval160Bit4Pass));
				case Type.HAVAL_5_160: return new Haval(HavalParameters.GetParameters(HavalStandard.Haval160Bit5Pass));
				case Type.HAVAL_3_192: return new Haval(HavalParameters.GetParameters(HavalStandard.Haval192Bit3Pass));
				case Type.HAVAL_4_192: return new Haval(HavalParameters.GetParameters(HavalStandard.Haval192Bit4Pass));
				case Type.HAVAL_5_192: return new Haval(HavalParameters.GetParameters(HavalStandard.Haval192Bit5Pass));
				case Type.HAVAL_3_224: return new Haval(HavalParameters.GetParameters(HavalStandard.Haval224Bit3Pass));
				case Type.HAVAL_4_224: return new Haval(HavalParameters.GetParameters(HavalStandard.Haval224Bit4Pass));
				case Type.HAVAL_5_224: return new Haval(HavalParameters.GetParameters(HavalStandard.Haval224Bit5Pass));
				case Type.HAVAL_3_256: return new Haval(HavalParameters.GetParameters(HavalStandard.Haval256Bit3Pass));
				case Type.HAVAL_4_256: return new Haval(HavalParameters.GetParameters(HavalStandard.Haval256Bit4Pass));
				case Type.HAVAL_5_256: return new Haval(HavalParameters.GetParameters(HavalStandard.Haval256Bit5Pass));
				case Type.JenkinsHash: return new JenkinsHash();
				case Type.JSHash: return new JSHash();
				case Type.MD2: return new MD2();
				case Type.MD4: return new MD4();
				case Type.MD5: return new MD5();
				case Type.Panama: return new Panama();
				case Type.Pjw32: return new Pjw32();
				case Type.RIPEMD128: return new RipeMD128();
				case Type.RIPEMD160: return new RipeMD160();
				case Type.RIPEMD256: return new RipeMD256();
				case Type.RIPEMD320: return new RipeMD320();
				case Type.RSHash: return new RSHash();
				case Type.SdbmHash: return new SdbmHash();
				case Type.SHA0: return new Sha0();
				case Type.SHA1: return new Sha1();
				case Type.SHA224: return new Sha224();
				case Type.SHA256: return new Sha256();
				case Type.SHA384: return new Sha384();
				case Type.SHA512: return new Sha512();
				case Type.Snefru_4_128: return new Snefru2(Snefru2Parameters.GetParameters(Snefru2Standard.Snefru128Bit4Pass));
				case Type.Snefru_4_256: return new Snefru2(Snefru2Parameters.GetParameters(Snefru2Standard.Snefru256Bit4Pass));
				case Type.Snefru_8_128: return new Snefru2(Snefru2Parameters.GetParameters(Snefru2Standard.Snefru128Bit8Pass));
				case Type.Snefru_8_256: return new Snefru2(Snefru2Parameters.GetParameters(Snefru2Standard.Snefru256Bit8Pass));
				case Type.Sum_8: return new Sum(SumParameters.GetParameters(SumStandard.Sum8Bit));
				case Type.Sum_16: return new Sum(SumParameters.GetParameters(SumStandard.Sum16Bit));
				case Type.Sum_24: return new Sum(SumParameters.GetParameters(SumStandard.Sum24Bit));
				case Type.Sum_32: return new Sum(SumParameters.GetParameters(SumStandard.Sum32Bit));
				case Type.Sum_64: return new Sum(SumParameters.GetParameters(SumStandard.Sum64Bit));
				case Type.SumBsd: return new SumBsd();
				case Type.SumSysV: return new SumSysV();
				case Type.Tiger_3_128: return new Tiger(TigerParameters.GetParameters(TigerStandard.Tiger128BitVersion1));
				case Type.Tiger_3_160: return new Tiger(TigerParameters.GetParameters(TigerStandard.Tiger160BitVersion1));
				case Type.Tiger_3_192: return new Tiger(TigerParameters.GetParameters(TigerStandard.Tiger192BitVersion1));
				case Type.Tiger2_128: return new Tiger(TigerParameters.GetParameters(TigerStandard.Tiger128BitVersion2));
				case Type.Tiger2_160: return new Tiger(TigerParameters.GetParameters(TigerStandard.Tiger160BitVersion2));
				case Type.Tiger2_192: return new Tiger(TigerParameters.GetParameters(TigerStandard.Tiger192BitVersion2));
				case Type.Whirlpool: return new Whirlpool();
				case Type.Xum32: return new Xum32();
				case Type.Xor8: return new Xor8();
				default: throw new NotImplementedException();
			}
		}

		static string Get(Stream input, Type type, byte[] key, Action<long> progress)
		{
			if (type == Type.QuickHash)
				return Coder.BytesToString(ComputeQuickHash(input), Coder.CodePage.Hex);

			var hashAlg = GetHashAlgorithm(type);
			if ((key != null) && (hashAlg is BlockHashAlgorithm))
				hashAlg = new Hmac(hashAlg as BlockHashAlgorithm, key);
			hashAlg.Initialize();
			var buffer = new byte[65536];
			while (true)
			{
				progress?.Invoke(input.Position);

				var block = input.Read(buffer, 0, buffer.Length);
				if (block == 0)
					break;
				hashAlg.TransformBlock(buffer, 0, block, null, 0);
			}
			hashAlg.TransformFinalBlock(buffer, 0, 0);
			return Coder.BytesToString(hashAlg.Hash, Coder.CodePage.Hex);
		}

		public static string Get(string fileName, Type type, byte[] key, Action<long> progress)
		{
			using (var stream = File.OpenRead(fileName))
				return Get(stream, type, key, progress);
		}

		public static string Get(byte[] data, Type type, byte[] key)
		{
			using (var stream = new MemoryStream(data))
				return Get(stream, type, key, null);
		}

		public static byte[] ComputeQuickHash(Stream stream)
		{
			const int QuickHashBlockSize = 2048;

			var hash = MD5.Create();
			hash.Initialize();

			var blockSize = (int)Math.Min(stream.Length, QuickHashBlockSize);
			var buffer = new byte[blockSize];

			var length = BitConverter.GetBytes(stream.Length);
			hash.TransformBlock(length, 0, length.Length, null, 0);

			// First block
			stream.Position = 0;
			stream.Read(buffer, 0, blockSize);
			hash.TransformBlock(buffer, 0, buffer.Length, null, 0);

			// Last block
			stream.Position = stream.Length - blockSize;
			stream.Read(buffer, 0, blockSize);
			hash.TransformFinalBlock(buffer, 0, buffer.Length);

			return hash.Hash;
		}
	}
}
