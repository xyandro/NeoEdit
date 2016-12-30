using System;
using System.Threading;
using System.Threading.Tasks;
using NeoEdit.Common;

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

		public async override Task Run(IProgress<ProgressReport> progress, CancellationToken token, string directory) => await YouTube.Save(Video, GetFileName(directory), progress, token);
	}
}
