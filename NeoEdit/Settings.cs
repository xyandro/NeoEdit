using System;
using System.IO;
using System.Xml.Linq;

namespace NeoEdit.Program
{
	public static class Settings
	{
		static readonly string settingsFile = Path.Combine(Helpers.NeoEditAppData, $"Settings{(Helpers.IsDebugBuild ? "-Debug" : "")}.xml");

		static Settings()
		{
			if (File.Exists(settingsFile))
			{
				try
				{
					var xml = XElement.Load(settingsFile);
					try { dontExitOnClose = bool.Parse(xml.Element(nameof(DontExitOnClose)).Value); } catch { }
					try { escapeClearsSelections = bool.Parse(xml.Element(nameof(EscapeClearsSelections)).Value); } catch { }
					try { youTubeDLPath = xml.Element(nameof(YouTubeDLPath)).Value; } catch { }
					try { ffmpegPath = xml.Element(nameof(FFmpegPath)).Value; } catch { }
					try { Font.FontSize = int.Parse(xml.Element(nameof(Font.FontSize)).Value); } catch { }
					try { windowPosition = xml.Element(nameof(WindowPosition)).Value; } catch { }
				}
				catch { }
			}

			Font.FontSizeChanged += (s, e) => SaveSettings();
		}

		static void SaveSettings()
		{
			try
			{
				var xml = new XElement("Settings");
				xml.Add(new XElement(nameof(DontExitOnClose), dontExitOnClose));
				xml.Add(new XElement(nameof(EscapeClearsSelections), escapeClearsSelections));
				xml.Add(new XElement(nameof(YouTubeDLPath), youTubeDLPath));
				xml.Add(new XElement(nameof(FFmpegPath), ffmpegPath));
				xml.Add(new XElement(nameof(Font.FontSize), Font.FontSize));
				xml.Add(new XElement(nameof(WindowPosition), windowPosition));
				xml.Save(settingsFile);
			}
			catch { }
		}

		static bool dontExitOnClose = false;
		public static bool DontExitOnClose
		{
			get { return dontExitOnClose; }
			set
			{
				dontExitOnClose = value;
				SaveSettings();
				DontExitOnCloseChanged?.Invoke(null, new EventArgs());
			}
		}

		public static event EventHandler DontExitOnCloseChanged;

		static bool escapeClearsSelections = true;
		public static bool EscapeClearsSelections
		{
			get { return escapeClearsSelections; }
			set
			{
				escapeClearsSelections = value;
				SaveSettings();
				EscapeClearsSelectionsChanged?.Invoke(null, new EventArgs());
			}
		}

		public static event EventHandler EscapeClearsSelectionsChanged;

		static string youTubeDLPath = "";
		public static string YouTubeDLPath
		{
			get { return youTubeDLPath; }
			set
			{
				youTubeDLPath = value;
				SaveSettings();
				YouTubeDLPathChanged?.Invoke(null, new EventArgs());
			}
		}

		public static event EventHandler YouTubeDLPathChanged;

		static string ffmpegPath = "";
		public static string FFmpegPath
		{
			get { return ffmpegPath; }
			set
			{
				ffmpegPath = value;
				SaveSettings();
				FFmpegPathChanged?.Invoke(null, new EventArgs());
			}
		}

		public static event EventHandler FFmpegPathChanged;

		static string windowPosition;
		public static string WindowPosition
		{
			get { return windowPosition; }
			set
			{
				windowPosition = value;
				SaveSettings();
			}
		}

		public static bool CanExtract { get; } = string.IsNullOrEmpty(typeof(Settings).Assembly.Location);
	}
}
