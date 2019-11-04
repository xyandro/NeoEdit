using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TestWCF
{
	[DataContract]
	public class GetAllTypesReply
	{
		[DataMember] public bool BoolMin { get; set; } = false;
		[DataMember] public bool BoolMax { get; set; } = true;
		[DataMember] public bool BoolDefault { get; set; } = default(bool);
		[DataMember] public bool? BoolNonNull { get; set; } = default(bool);
		[DataMember] public bool? BoolNull { get; set; } = null;
		[DataMember] public byte ByteMin { get; set; } = byte.MinValue;
		[DataMember] public byte ByteMax { get; set; } = byte.MaxValue;
		[DataMember] public byte ByteDefault { get; set; } = default(byte);
		[DataMember] public byte? ByteNonNull { get; set; } = default(byte);
		[DataMember] public byte? ByteNull { get; set; } = null;
		[DataMember] public sbyte SbyteMin { get; set; } = sbyte.MinValue;
		[DataMember] public sbyte SbyteMax { get; set; } = sbyte.MaxValue;
		[DataMember] public sbyte SbyteDefault { get; set; } = default(sbyte);
		[DataMember] public sbyte? SbyteNonNull { get; set; } = default(sbyte);
		[DataMember] public sbyte? SbyteNull { get; set; } = null;
		[DataMember] public char CharMin { get; set; } = char.MinValue;
		[DataMember] public char CharMax { get; set; } = char.MaxValue;
		[DataMember] public char CharDefault { get; set; } = default(char);
		[DataMember] public char? CharNonNull { get; set; } = default(char);
		[DataMember] public char? CharNull { get; set; } = null;
		[DataMember] public decimal DecimalMin { get; set; } = decimal.MinValue;
		[DataMember] public decimal DecimalMax { get; set; } = decimal.MaxValue;
		[DataMember] public decimal DecimalDefault { get; set; } = default(decimal);
		[DataMember] public decimal? DecimalNonNull { get; set; } = default(decimal);
		[DataMember] public decimal? DecimalNull { get; set; } = null;
		[DataMember] public double DoubleMin { get; set; } = double.MinValue;
		[DataMember] public double DoubleMax { get; set; } = double.MaxValue;
		[DataMember] public double DoubleDefault { get; set; } = default(double);
		[DataMember] public double? DoubleNonNull { get; set; } = default(double);
		[DataMember] public double? DoubleNull { get; set; } = null;
		[DataMember] public float FloatMin { get; set; } = float.MinValue;
		[DataMember] public float FloatMax { get; set; } = float.MaxValue;
		[DataMember] public float FloatDefault { get; set; } = default(float);
		[DataMember] public float? FloatNonNull { get; set; } = default(float);
		[DataMember] public float? FloatNull { get; set; } = null;
		[DataMember] public int IntMin { get; set; } = int.MinValue;
		[DataMember] public int IntMax { get; set; } = int.MaxValue;
		[DataMember] public int IntDefault { get; set; } = default(int);
		[DataMember] public int? IntNonNull { get; set; } = default(int);
		[DataMember] public int? IntNull { get; set; } = null;
		[DataMember] public uint UintMin { get; set; } = uint.MinValue;
		[DataMember] public uint UintMax { get; set; } = uint.MaxValue;
		[DataMember] public uint UintDefault { get; set; } = default(uint);
		[DataMember] public uint? UintNonNull { get; set; } = default(uint);
		[DataMember] public uint? UintNull { get; set; } = null;
		[DataMember] public long LongMin { get; set; } = long.MinValue;
		[DataMember] public long LongMax { get; set; } = long.MaxValue;
		[DataMember] public long LongDefault { get; set; } = default(long);
		[DataMember] public long? LongNonNull { get; set; } = default(long);
		[DataMember] public long? LongNull { get; set; } = null;
		[DataMember] public ulong UlongMin { get; set; } = ulong.MinValue;
		[DataMember] public ulong UlongMax { get; set; } = ulong.MaxValue;
		[DataMember] public ulong UlongDefault { get; set; } = default(ulong);
		[DataMember] public ulong? UlongNonNull { get; set; } = default(ulong);
		[DataMember] public ulong? UlongNull { get; set; } = null;
		[DataMember] public short ShortMin { get; set; } = short.MinValue;
		[DataMember] public short ShortMax { get; set; } = short.MaxValue;
		[DataMember] public short ShortDefault { get; set; } = default(short);
		[DataMember] public short? ShortNonNull { get; set; } = default(short);
		[DataMember] public short? ShortNull { get; set; } = null;
		[DataMember] public ushort UshortMin { get; set; } = ushort.MinValue;
		[DataMember] public ushort UshortMax { get; set; } = ushort.MaxValue;
		[DataMember] public ushort UshortDefault { get; set; } = default(ushort);
		[DataMember] public ushort? UshortNonNull { get; set; } = default(ushort);
		[DataMember] public ushort? UshortNull { get; set; } = null;

		[DataContract]
		public struct SubStruct
		{
			[DataMember] public int IntValue { get; set; }
			[DataMember] public string StrValue { get; set; }

			public SubStruct(int intValue, string strValue)
			{
				IntValue = intValue;
				StrValue = strValue;
			}
		}
		[DataMember] public SubStruct SubStructValue { get; set; } = new SubStruct(123, "asdf");
		[DataMember] public SubStruct? SubStructNonNull { get; set; } = new SubStruct(234, "wert");
		[DataMember] public SubStruct? SubStructNull { get; set; } = null;

		[DataContract]
		public class SubClass
		{
			[DataMember] public int IntValue { get; set; }
			[DataMember] public string StrValue { get; set; }

			public SubClass(int intValue, string strValue)
			{
				IntValue = intValue;
				StrValue = strValue;
			}
		}

		[DataMember] public SubClass SubClassValue { get; set; } = new SubClass(345, "erty");
		[DataMember] public SubClass SubClassNonNull { get; set; } = new SubClass(456, "rtyu");
		[DataMember] public SubClass SubClassNull { get; set; } = null;

		[DataMember] public List<int> ListInt { get; set; } = new List<int> { 5, 10, 15, 20 };
		[DataMember] public List<long?> ListLong { get; set; } = new List<long?> { 500, 1000, null, 2000 };
		[DataMember] public List<SubStruct> ListSubStruct { get; set; } = new List<SubStruct> { new SubStruct(567, "tyui"), new SubStruct(678, "yuio") };
		[DataMember] public List<SubClass> ListSubClass { get; set; } = new List<SubClass> { new SubClass(901, "opqw"), null, new SubClass(012, "pqwe") };

		[IgnoreDataMember] public int TagIgnored { get; set; } = 5;
		public int TagUntagged { get; set; } = 6;
	}
}
