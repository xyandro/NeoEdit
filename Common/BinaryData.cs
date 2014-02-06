using System;
using System.Text;

namespace NeoEdit.Common
{
	public class BinaryData
	{
		readonly byte[] data;
		public BinaryData(byte[] _data)
		{
			data = _data;
		}

		public byte this[long index]
		{
			get { return data[index]; }
		}

		public long Length
		{
			get { return data.Length; }
		}

		public long IndexOf(byte value, long start)
		{
			return Array.IndexOf(data, value, (int)start);
		}

		public long LastIndexOf(byte value, long start)
		{
			return Array.LastIndexOf(data, value, (int)start);
		}

		public void Copy(long sourceIndex, byte[] dest, long destIndex, long count)
		{
			Array.Copy(data, sourceIndex, dest, destIndex, count);
		}

		public string GetString(Encoding encoding, long index, long count)
		{
			return encoding.GetString(data, (int)index, (int)count);
		}

		public void Reverse()
		{
			Array.Reverse(data);
		}

		public override string ToString()
		{
			return BitConverter.ToString(data);
		}

		public string MD5()
		{
			using (var md5 = System.Security.Cryptography.MD5.Create())
				return BitConverter.ToString(md5.ComputeHash(data)).Replace("-", "").ToLower();
		}

		public byte[] Data
		{
			get { return data; }
		}
	}
}
