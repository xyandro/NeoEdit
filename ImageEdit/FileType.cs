using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.ImageEdit
{
	class FileTypeExtensionsAttribute : Attribute
	{
		public string[] Extensions { get; }

		public FileTypeExtensionsAttribute(params string[] extensions)
		{
			Extensions = extensions;
		}
	}

	public enum FileType
	{
		None,
		[FileTypeExtensions("bmp", "dib")]
		Bitmap,
		[FileTypeExtensions("jpg", "jpeg", "jpe", "jfif")]
		Jpeg,
		[FileTypeExtensions("gif")]
		Gif,
		[FileTypeExtensions("tif", "tiff")]
		Tiff,
		[FileTypeExtensions("png")]
		Png,
	}

	static class FileTypeExtensions
	{
		static IEnumerable<Tuple<FileType, string[]>> GetTypes() => Helpers.GetValues<FileType>().Where(type => type != FileType.None).Select(type => Tuple.Create(type, typeof(FileType).GetMember(type.ToString()).First().GetCustomAttributes(typeof(FileTypeExtensionsAttribute), false).Cast<FileTypeExtensionsAttribute>().First().Extensions));

		public static string GetSaveFilter() => string.Join("|", GetTypes().Select(tuple => $"{tuple.Item1}|{string.Join(";", tuple.Item2.Select(ext => $"*.{ext}"))}"));

		public static int GetSaveFilterIndex(string fileName) => GetTypes().Indexes(tuple => tuple.Item1 == GetFileType(fileName)).Single() + 1;

		public static string GetOpenFilter()
		{
			var types = GetTypes();
			var filters = types.Select(tuple => $"{tuple.Item1}|{string.Join(";", tuple.Item2.Select(ext => $"*.{ext}"))}").ToList();
			filters.Insert(0, $"All images|{string.Join(";", types.SelectMany(tuple => tuple.Item2.Select(ext => $"*.{ext}")))}");
			return string.Join("|", filters);
		}

		public static FileType GetFileType(string fileName)
		{
			var ext = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
			var fileType = GetTypes().Where(tuple => tuple.Item2.Contains(ext)).Select(tuple => tuple.Item1).SingleOrDefault();
			if (fileType == FileType.None)
				throw new Exception("Invalid file extension");
			return fileType;
		}
	}
}
