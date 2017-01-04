using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NeoEdit.Common;

namespace NeoEdit.Rip
{
	class YouTubeVideo
	{
		public string Title { get; private set; }
		public string JSPlayer { get; private set; }
		public string URI { get; private set; }
		public bool Encrypted { get; private set; }
		public int FormatCode { get; private set; }

		public YouTubeVideo(string title, string jsPlayer, string uri, bool encrypted, int formatCode)
		{
			if (!FormatData.FormatInfo.ContainsKey(formatCode))
				throw new Exception("YouYube format code not recognized.");
			Title = title;
			JSPlayer = jsPlayer;
			URI = uri;
			Encrypted = encrypted;
			FormatCode = formatCode;
		}

		public void SetDecryptedURI(string uri)
		{
			if (!Encrypted)
				throw new Exception("URI already decrypted");
			URI = uri;
			Encrypted = false;
		}

		public string FileName
		{
			get
			{
				var invalid = new HashSet<char>(Path.GetInvalidFileNameChars());
				return $"{Regex.Replace(new string(Title.Select(ch => invalid.Contains(ch) ? ' ' : ch).ToArray()), @"\s+", " ").Trim()}.{Extension}";
			}
		}

		public override string ToString() => $"{FileName}: Resolution: {Resolution}, Video: {Video}, Audio: {Audio}, AdaptiveKind: {AdaptiveKind}";
		public string Description => $"{Extension}, {Resolution}, {Video}, {Audio}, {AdaptiveKind}";

		public enum AdaptiveKindEnum
		{
			None,
			Audio,
			Video,
		}

		FormatData FormatInfo => FormatData.FormatInfo[FormatCode];

		public string Extension => FormatInfo.Extension;
		public int? Width => FormatInfo.Width;
		public int? Height => FormatInfo.Height;
		public string Resolution => FormatInfo.Resolution;
		public string Audio => FormatInfo.Audio;
		public string Video => FormatInfo.Video;
		public int? AudioBitRate => FormatInfo.AudioBitRate;
		public bool Is3D => FormatInfo.Is3D;
		public AdaptiveKindEnum AdaptiveKind => FormatInfo.AdaptiveKind;
		public bool IsAdaptive => AdaptiveKind != AdaptiveKindEnum.None;

		static public List<string> Extensions => FormatData.FormatInfo.Values.Select(format => format.Extension).Distinct().OrderBy().ToList();
		static public List<string> Resolutions => FormatData.FormatInfo.Values.OrderByDescending(format => format.Height).ThenByDescending(format => format.Width).Select(format => format.Resolution).Distinct().ToList();
		static public List<string> Audios => FormatData.FormatInfo.Values.Select(format => format.Audio).Distinct().OrderBy().OrderBy(format => format == null).ToList();
		static public List<string> Videos => FormatData.FormatInfo.Values.Select(format => format.Video).Distinct().OrderBy().OrderBy(format => format == null).ToList();
		static public List<int?> AudioBitRates => FormatData.FormatInfo.Values.Select(format => format.AudioBitRate).Distinct().OrderByDescending().ToList();
		static public List<bool> Is3Ds => FormatData.FormatInfo.Values.Select(format => format.Is3D).Distinct().OrderByDescending().ToList();
		static public List<AdaptiveKindEnum> AdaptiveKinds => FormatData.FormatInfo.Values.Select(format => format.AdaptiveKind).Distinct().OrderBy().ToList();

		class FormatData
		{
			public int FormatCode { get; private set; }
			public string Extension { get; private set; }
			public int? Width { get; private set; }
			public int? Height { get; private set; }
			public string Audio { get; private set; }
			public string Video { get; private set; }
			public int? AudioBitRate { get; private set; }
			public bool Is3D { get; private set; }
			public AdaptiveKindEnum AdaptiveKind { get; private set; }
			public string Resolution
			{
				get
				{
					if (Width.HasValue)
					{
						if (Height.HasValue)
							return $"{Width}x{Height}";
						else
							return $"{Width}x?";
					}
					else if (Height.HasValue)
						return $"{Height}p";
					else
						return null;
				}
			}

			static public Dictionary<int, FormatData> FormatInfo { get; } = new List<FormatData>
			{
				new FormatData { FormatCode = 5  , Extension = "flv" , Width = 400 , Height = 240 , Audio = "mp3"   , Video = "h263", AudioBitRate = 64  , Is3D = false, AdaptiveKind = AdaptiveKindEnum.None  },
				new FormatData { FormatCode = 6  , Extension = "flv" , Width = 450 , Height = 270 , Audio = "mp3"   , Video = "h263", AudioBitRate = 64  , Is3D = false, AdaptiveKind = AdaptiveKindEnum.None  },
				new FormatData { FormatCode = 13 , Extension = "3gp" , Width = null, Height = null, Audio = "aac"   , Video = "mp4v", AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.None  },
				new FormatData { FormatCode = 17 , Extension = "3gp" , Width = 176 , Height = 144 , Audio = "aac"   , Video = "mp4v", AudioBitRate = 24  , Is3D = false, AdaptiveKind = AdaptiveKindEnum.None  },
				new FormatData { FormatCode = 18 , Extension = "mp4" , Width = 640 , Height = 360 , Audio = "aac"   , Video = "h264", AudioBitRate = 96  , Is3D = false, AdaptiveKind = AdaptiveKindEnum.None  },
				new FormatData { FormatCode = 22 , Extension = "mp4" , Width = 1280, Height = 720 , Audio = "aac"   , Video = "h264", AudioBitRate = 192 , Is3D = false, AdaptiveKind = AdaptiveKindEnum.None  },
				new FormatData { FormatCode = 34 , Extension = "flv" , Width = 640 , Height = 360 , Audio = "aac"   , Video = "h264", AudioBitRate = 128 , Is3D = false, AdaptiveKind = AdaptiveKindEnum.None  },
				new FormatData { FormatCode = 35 , Extension = "flv" , Width = 854 , Height = 480 , Audio = "aac"   , Video = "h264", AudioBitRate = 128 , Is3D = false, AdaptiveKind = AdaptiveKindEnum.None  },
				new FormatData { FormatCode = 36 , Extension = "3gp" , Width = 320 , Height = null, Audio = "aac"   , Video = "mp4v", AudioBitRate = 38  , Is3D = false, AdaptiveKind = AdaptiveKindEnum.None  },
				new FormatData { FormatCode = 37 , Extension = "mp4" , Width = 1920, Height = 1080, Audio = "aac"   , Video = "h264", AudioBitRate = 192 , Is3D = false, AdaptiveKind = AdaptiveKindEnum.None  },
				new FormatData { FormatCode = 38 , Extension = "mp4" , Width = 4096, Height = 3072, Audio = "aac"   , Video = "h264", AudioBitRate = 192 , Is3D = false, AdaptiveKind = AdaptiveKindEnum.None  },
				new FormatData { FormatCode = 43 , Extension = "webm", Width = 640 , Height = 360 , Audio = "vorbis", Video = "vp8" , AudioBitRate = 128 , Is3D = false, AdaptiveKind = AdaptiveKindEnum.None  },
				new FormatData { FormatCode = 44 , Extension = "webm", Width = 854 , Height = 480 , Audio = "vorbis", Video = "vp8" , AudioBitRate = 128 , Is3D = false, AdaptiveKind = AdaptiveKindEnum.None  },
				new FormatData { FormatCode = 45 , Extension = "webm", Width = 1280, Height = 720 , Audio = "vorbis", Video = "vp8" , AudioBitRate = 192 , Is3D = false, AdaptiveKind = AdaptiveKindEnum.None  },
				new FormatData { FormatCode = 46 , Extension = "webm", Width = 1920, Height = 1080, Audio = "vorbis", Video = "vp8" , AudioBitRate = 192 , Is3D = false, AdaptiveKind = AdaptiveKindEnum.None  },
				new FormatData { FormatCode = 59 , Extension = "mp4" , Width = 854 , Height = 480 , Audio = "aac"   , Video = "h264", AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.None  },
				new FormatData { FormatCode = 78 , Extension = "mp4" , Width = 854 , Height = 480 , Audio = "aac"   , Video = "h264", AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.None  },
				new FormatData { FormatCode = 82 , Extension = "mp4" , Width = null, Height = 360 , Audio = "aac"   , Video = "h264", AudioBitRate = 96  , Is3D = true , AdaptiveKind = AdaptiveKindEnum.None  },
				new FormatData { FormatCode = 83 , Extension = "mp4" , Width = null, Height = 480 , Audio = "aac"   , Video = "h264", AudioBitRate = 96  , Is3D = true , AdaptiveKind = AdaptiveKindEnum.None  },
				new FormatData { FormatCode = 84 , Extension = "mp4" , Width = null, Height = 720 , Audio = "aac"   , Video = "h264", AudioBitRate = 152 , Is3D = true , AdaptiveKind = AdaptiveKindEnum.None  },
				new FormatData { FormatCode = 85 , Extension = "mp4" , Width = null, Height = 1080, Audio = "aac"   , Video = "h264", AudioBitRate = 152 , Is3D = true , AdaptiveKind = AdaptiveKindEnum.None  },
				new FormatData { FormatCode = 91 , Extension = "mp4" , Width = null, Height = 144 , Audio = "aac"   , Video = "h264", AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.None  },
				new FormatData { FormatCode = 92 , Extension = "mp4" , Width = null, Height = 240 , Audio = "aac"   , Video = "h264", AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.None  },
				new FormatData { FormatCode = 93 , Extension = "mp4" , Width = null, Height = 360 , Audio = "aac"   , Video = "h264", AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.None  },
				new FormatData { FormatCode = 94 , Extension = "mp4" , Width = null, Height = 480 , Audio = "aac"   , Video = "h264", AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.None  },
				new FormatData { FormatCode = 95 , Extension = "mp4" , Width = null, Height = 720 , Audio = "aac"   , Video = "h264", AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.None  },
				new FormatData { FormatCode = 96 , Extension = "mp4" , Width = null, Height = 1080, Audio = "aac"   , Video = "h264", AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.None  },
				new FormatData { FormatCode = 100, Extension = "webm", Width = null, Height = 360 , Audio = "vorbis", Video = "vp8" , AudioBitRate = 128 , Is3D = true , AdaptiveKind = AdaptiveKindEnum.None  },
				new FormatData { FormatCode = 101, Extension = "webm", Width = null, Height = 480 , Audio = "vorbis", Video = "vp8" , AudioBitRate = 192 , Is3D = true , AdaptiveKind = AdaptiveKindEnum.None  },
				new FormatData { FormatCode = 102, Extension = "webm", Width = null, Height = 720 , Audio = "vorbis", Video = "vp8" , AudioBitRate = 192 , Is3D = true , AdaptiveKind = AdaptiveKindEnum.None  },
				new FormatData { FormatCode = 132, Extension = "mp4" , Width = null, Height = 240 , Audio = "aac"   , Video = "h264", AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.None  },
				new FormatData { FormatCode = 133, Extension = "mp4" , Width = null, Height = 240 , Audio = null    , Video = "h264", AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.Video },
				new FormatData { FormatCode = 134, Extension = "mp4" , Width = null, Height = 360 , Audio = null    , Video = "h264", AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.Video },
				new FormatData { FormatCode = 135, Extension = "mp4" , Width = null, Height = 480 , Audio = null    , Video = "h264", AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.Video },
				new FormatData { FormatCode = 136, Extension = "mp4" , Width = null, Height = 720 , Audio = null    , Video = "h264", AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.Video },
				new FormatData { FormatCode = 137, Extension = "mp4" , Width = null, Height = 1080, Audio = null    , Video = "h264", AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.Video },
				new FormatData { FormatCode = 138, Extension = "mp4" , Width = null, Height = null, Audio = null    , Video = "h264", AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.Video },
				new FormatData { FormatCode = 139, Extension = "m4a" , Width = null, Height = null, Audio = "aac"   , Video = null  , AudioBitRate = 48  , Is3D = false, AdaptiveKind = AdaptiveKindEnum.Audio },
				new FormatData { FormatCode = 140, Extension = "m4a" , Width = null, Height = null, Audio = "aac"   , Video = null  , AudioBitRate = 128 , Is3D = false, AdaptiveKind = AdaptiveKindEnum.Audio },
				new FormatData { FormatCode = 141, Extension = "m4a" , Width = null, Height = null, Audio = "aac"   , Video = null  , AudioBitRate = 256 , Is3D = false, AdaptiveKind = AdaptiveKindEnum.Audio },
				new FormatData { FormatCode = 151, Extension = "mp4" , Width = null, Height = 72  , Audio = "aac"   , Video = "h264", AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.None  },
				new FormatData { FormatCode = 160, Extension = "mp4" , Width = null, Height = 144 , Audio = null    , Video = "h264", AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.Video },
				new FormatData { FormatCode = 167, Extension = "webm", Width = 640 , Height = 360 , Audio = null    , Video = "vp8" , AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.Video },
				new FormatData { FormatCode = 168, Extension = "webm", Width = 854 , Height = 480 , Audio = null    , Video = "vp8" , AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.Video },
				new FormatData { FormatCode = 169, Extension = "webm", Width = 1280, Height = 720 , Audio = null    , Video = "vp8" , AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.Video },
				new FormatData { FormatCode = 170, Extension = "webm", Width = 1920, Height = 1080, Audio = null    , Video = "vp8" , AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.Video },
				new FormatData { FormatCode = 171, Extension = "webm", Width = null, Height = null, Audio = "vorbis", Video = null  , AudioBitRate = 128 , Is3D = false, AdaptiveKind = AdaptiveKindEnum.Audio },
				new FormatData { FormatCode = 172, Extension = "webm", Width = null, Height = null, Audio = "vorbis", Video = null  , AudioBitRate = 192 , Is3D = false, AdaptiveKind = AdaptiveKindEnum.Audio },
				new FormatData { FormatCode = 218, Extension = "webm", Width = 854 , Height = 480 , Audio = null    , Video = "vp8" , AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.Video },
				new FormatData { FormatCode = 219, Extension = "webm", Width = 854 , Height = 480 , Audio = null    , Video = "vp8" , AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.Video },
				new FormatData { FormatCode = 242, Extension = "webm", Width = null, Height = 240 , Audio = null    , Video = "vp9" , AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.Video },
				new FormatData { FormatCode = 243, Extension = "webm", Width = null, Height = 360 , Audio = null    , Video = "vp9" , AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.Video },
				new FormatData { FormatCode = 244, Extension = "webm", Width = null, Height = 480 , Audio = null    , Video = "vp9" , AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.Video },
				new FormatData { FormatCode = 245, Extension = "webm", Width = null, Height = 480 , Audio = null    , Video = "vp9" , AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.Video },
				new FormatData { FormatCode = 246, Extension = "webm", Width = null, Height = 480 , Audio = null    , Video = "vp9" , AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.Video },
				new FormatData { FormatCode = 247, Extension = "webm", Width = null, Height = 720 , Audio = null    , Video = "vp9" , AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.Video },
				new FormatData { FormatCode = 248, Extension = "webm", Width = null, Height = 1080, Audio = null    , Video = "vp9" , AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.Video },
				new FormatData { FormatCode = 249, Extension = "webm", Width = null, Height = null, Audio = "opus"  , Video = null  , AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.Audio },
				new FormatData { FormatCode = 250, Extension = "webm", Width = null, Height = null, Audio = "opus"  , Video = null  , AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.Audio },
				new FormatData { FormatCode = 251, Extension = "webm", Width = null, Height = null, Audio = "opus"  , Video = null  , AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.Audio },
				new FormatData { FormatCode = 256, Extension = "m4a" , Width = null, Height = null, Audio = "aac"   , Video = null  , AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.Audio },
				new FormatData { FormatCode = 258, Extension = "m4a" , Width = null, Height = null, Audio = "aac"   , Video = null  , AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.Audio },
				new FormatData { FormatCode = 264, Extension = "mp4" , Width = null, Height = 1440, Audio = null    , Video = "h264", AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.Video },
				new FormatData { FormatCode = 266, Extension = "mp4" , Width = null, Height = 2160, Audio = null    , Video = "h264", AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.Video },
				new FormatData { FormatCode = 271, Extension = "webm", Width = null, Height = 1440, Audio = null    , Video = "vp9" , AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.Video },
				new FormatData { FormatCode = 272, Extension = "webm", Width = null, Height = 2160, Audio = null    , Video = "vp9" , AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.Video },
				new FormatData { FormatCode = 278, Extension = "webm", Width = null, Height = 144 , Audio = null    , Video = "vp9" , AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.Video },
				new FormatData { FormatCode = 298, Extension = "mp4" , Width = null, Height = 720 , Audio = null    , Video = "h264", AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.Video },
				new FormatData { FormatCode = 299, Extension = "mp4" , Width = null, Height = 1080, Audio = null    , Video = "h264", AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.Video },
				new FormatData { FormatCode = 302, Extension = "webm", Width = null, Height = 720 , Audio = null    , Video = "vp9" , AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.Video },
				new FormatData { FormatCode = 303, Extension = "webm", Width = null, Height = 1080, Audio = null    , Video = "vp9" , AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.Video },
				new FormatData { FormatCode = 308, Extension = "webm", Width = null, Height = 1440, Audio = null    , Video = "vp9" , AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.Video },
				new FormatData { FormatCode = 313, Extension = "webm", Width = null, Height = 2160, Audio = null    , Video = "vp9" , AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.Video },
				new FormatData { FormatCode = 315, Extension = "webm", Width = null, Height = 2160, Audio = null    , Video = "vp9" , AudioBitRate = null, Is3D = false, AdaptiveKind = AdaptiveKindEnum.Video },
			}.ToDictionary(formatData => formatData.FormatCode);
		}
	}
}
