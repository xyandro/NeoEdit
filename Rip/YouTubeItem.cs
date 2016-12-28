using System;

namespace NeoEdit.Rip
{
	class YouTubeItem : RipItem
	{
		public string Title { get; }
		public string Name { get; }

		public override string FileName => Name;

		readonly YouTube YouTube;
		readonly YouTubeVideo Video;

		public YouTubeItem(YouTube youTube, YouTubeVideo video)
		{
			YouTube = youTube;
			Video = video;
			Title = $"{Video.Title}: {Video.Height}p";
			Name = Video.FileName;
		}

		public override string ToString() => Title;

		public override void Run(Func<bool> cancelled, Action<int> progress, string directory) => YouTube.Save(Video, GetFileName(directory), cancelled, progress).Wait();
	}
}
