using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace NeoEdit.Program
{
	public class GIFWriter : IDisposable
	{
		const long SourceGlobalColorInfoPosition = 10, SourceImageBlockPosition = 789;

		readonly BinaryWriter writer;
		readonly int repeat;
		bool firstFrame = true;

		public GIFWriter(string fileName, int repeat = 0)
		{
			if (fileName == null)
				throw new ArgumentNullException(nameof(fileName));

			if (repeat < 0)
				throw new ArgumentOutOfRangeException(nameof(repeat));

			writer = new BinaryWriter(File.Create(fileName));
			this.repeat = repeat;
		}

		public void WriteFrame(Image image, int delay)
		{
			using (var source = new MemoryStream())
			{
				image.Save(source, ImageFormat.Gif);

				// Steal the global color table info
				if (firstFrame)
					InitHeader(source, image.Width, image.Height);

				WriteGraphicControlBlock(source, delay);
				WriteImageBlock(source, !firstFrame, 0, 0, image.Width, image.Height);
			}

			if (firstFrame)
				firstFrame = false;
		}

		void InitHeader(Stream source, int width, int height)
		{
			// File Header
			writer.Write("GIF".ToCharArray()); // File type
			writer.Write("89a".ToCharArray()); // File Version

			writer.Write((short)width); // Initial Logical Width
			writer.Write((short)height); // Initial Logical Height

			source.Position = SourceGlobalColorInfoPosition;
			writer.Write((byte)source.ReadByte()); // Global Color Table Info
			writer.Write((byte)0); // Background Color Index
			writer.Write((byte)0); // Pixel aspect ratio
			WriteColorTable(source);

			// App Extension Header for Repeating
			if (repeat != 0)
			{
				writer.Write(unchecked((short)0xff21)); // Application Extension Block Identifier
				writer.Write((byte)0x0b); // Application Block Size
				writer.Write("NETSCAPE2.0".ToCharArray()); // Application Identifier
				writer.Write((byte)3); // Application block length
				writer.Write((byte)1);
				writer.Write((short)repeat); // Repeat count for images.
				writer.Write((byte)0); // terminator
			}
		}

		void WriteColorTable(Stream source)
		{
			source.Position = 13; // Locating the image color table
			var colorTable = new byte[768];
			source.Read(colorTable, 0, colorTable.Length);
			writer.Write(colorTable, 0, colorTable.Length);
		}

		void WriteGraphicControlBlock(Stream source, int delay)
		{
			source.Position = 781; // Locating the source GCE
			var blockhead = new byte[8];
			source.Read(blockhead, 0, blockhead.Length); // Reading source GCE

			writer.Write(unchecked((short)0xf921)); // Identifier
			writer.Write((byte)0x04); // Block Size
			writer.Write((byte)(blockhead[3] & 0xf7 | 0x08)); // Setting disposal flag
			writer.Write((short)(delay / 10)); // Setting frame delay
			writer.Write(blockhead[6]); // Transparent color index
			writer.Write((byte)0); // Terminator
		}

		void WriteImageBlock(Stream source, bool includeColorTable, int x, int y, int width, int height)
		{
			source.Position = SourceImageBlockPosition; // Locating the image block
			var header = new byte[11];
			source.Read(header, 0, header.Length);
			writer.Write(header[0]); // Separator
			writer.Write((short)x); // Position X
			writer.Write((short)y); // Position Y
			writer.Write((short)width); // Width
			writer.Write((short)height); // Height

			if (includeColorTable) // If first frame, use global color table - else use local
			{
				source.Position = SourceGlobalColorInfoPosition;
				writer.Write((byte)(source.ReadByte() & 0x3f | 0x80)); // Enabling local color table
				WriteColorTable(source);
			}
			else writer.Write((byte)(header[9] & 0x07 | 0x07)); // Disabling local color table

			writer.Write(header[10]); // LZW Min Code Size

			// Read/Write image data
			source.Position = SourceImageBlockPosition + header.Length;

			var dataLength = source.ReadByte();
			while (dataLength > 0)
			{
				var imgData = new byte[dataLength];
				source.Read(imgData, 0, dataLength);

				writer.Write((byte)dataLength);
				writer.Write(imgData, 0, dataLength);
				dataLength = source.ReadByte();
			}

			writer.Write((byte)0); // Terminator
		}

		public void Dispose()
		{
			// Complete File
			writer.Write((byte)0x3b); // File Trailer

			writer.BaseStream.Dispose();
			writer.Dispose();
		}
	}
}
