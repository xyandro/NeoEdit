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
					try { exitOnClose = bool.Parse(xml.Element(nameof(ExitOnClose)).Value); } catch { }
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
				xml.Add(new XElement(nameof(ExitOnClose), exitOnClose));
				xml.Add(new XElement(nameof(EscapeClearsSelections), escapeClearsSelections));
				xml.Add(new XElement(nameof(YouTubeDLPath), youTubeDLPath));
				xml.Add(new XElement(nameof(FFmpegPath), ffmpegPath));
				xml.Add(new XElement(nameof(Font.FontSize), Font.FontSize));
				xml.Add(new XElement(nameof(WindowPosition), windowPosition));
				xml.Save(settingsFile);
			}
			catch { }
		}

		static bool exitOnClose = true;
		public static bool ExitOnClose
		{
			get { return exitOnClose; }
			set
			{
				exitOnClose = value;
				SaveSettings();
				exitOnCloseChanged?.Invoke(null, new EventArgs());
			}
		}

		static EventHandler exitOnCloseChanged;
		public static event EventHandler ExitOnCloseChanged { add { exitOnCloseChanged += value; } remove { exitOnCloseChanged -= value; } }

		static bool escapeClearsSelections = true;
		public static bool EscapeClearsSelections
		{
			get { return escapeClearsSelections; }
			set
			{
				escapeClearsSelections = value;
				SaveSettings();
				escapeClearsSelectionsChanged?.Invoke(null, new EventArgs());
			}
		}

		static EventHandler escapeClearsSelectionsChanged;
		public static event EventHandler EscapeClearsSelectionsChanged { add { escapeClearsSelectionsChanged += value; } remove { escapeClearsSelectionsChanged -= value; } }

		static string youTubeDLPath = "";
		public static string YouTubeDLPath
		{
			get { return youTubeDLPath; }
			set
			{
				youTubeDLPath = value;
				SaveSettings();
			}
		}

		static EventHandler youTubeDLPathChanged;
		public static event EventHandler YouTubeDLPathChanged { add { youTubeDLPathChanged += value; } remove { youTubeDLPathChanged -= value; } }

		static string ffmpegPath = "";
		public static string FFmpegPath
		{
			get { return ffmpegPath; }
			set
			{
				ffmpegPath = value;
				SaveSettings();
			}
		}

		static EventHandler ffmpegPathChanged;
		public static event EventHandler FFmpegPathChanged { add { ffmpegPathChanged += value; } remove { ffmpegPathChanged -= value; } }

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

	}
}
