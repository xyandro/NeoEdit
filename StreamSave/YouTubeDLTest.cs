using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NeoEdit.StreamSave
{
	[TestClass]
	public class YouTubeDLTest
	{
		[TestMethod]
		public void TestYouTubeList()
		{
			YouTubeDL.GetPlayListItems("https://www.youtube.com/playlist?list=PLzDWcvdzYAvqF6Dk6bWXyKcMQRbUCQaKp").Wait();
		}

		[TestMethod]
		public void TestYouTube()
		{
			YouTubeDL.DownloadStream(Directory.GetCurrentDirectory(), "https://www.youtube.com/watch?v=79DijItQXMM").Wait();
		}

		[TestMethod]
		public void TestVimeo()
		{
			YouTubeDL.DownloadStream(Directory.GetCurrentDirectory(), "https://vimeo.com/129230068").Wait();
		}

		[TestMethod]
		public void TestIMDB()
		{
			YouTubeDL.DownloadStream(Directory.GetCurrentDirectory(), "http://www.imdb.com/video/imdb/vi1245951769").Wait();
		}

		[TestMethod]
		public void TestVine()
		{
			YouTubeDL.DownloadStream(Directory.GetCurrentDirectory(), "https://vine.co/v/OZQ61X9KWwB").Wait();
		}
	}
}
