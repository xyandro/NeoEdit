﻿using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace NeoEdit.Common
{
	public static class Settings
	{
		static readonly string settingsFile = Path.Combine(Helpers.NeoEditAppData, $"Settings{(Helpers.IsDebugBuild ? "-Debug" : "")}.xml");

		static Settings()
		{
			try
			{
				if (!File.Exists(settingsFile))
					return;

				var xml = XElement.Load(settingsFile);
				try { dontExitOnClose = bool.Parse(xml.Element(nameof(DontExitOnClose)).Value); } catch { }
				try { youTubeDLPath = xml.Element(nameof(YouTubeDLPath)).Value; } catch { }
				try { ffmpegPath = xml.Element(nameof(FFmpegPath)).Value; } catch { }
				try { fontSize = double.Parse(xml.Element(nameof(FontSize)).Value); } catch { }
				try { showSpecialChars = bool.Parse(xml.Element(nameof(ShowSpecialChars)).Value); } catch { }
				try { windowPosition = xml.Element(nameof(WindowPosition)).Value; } catch { }
				try { wcfURLs = new List<string>(xml.Element(nameof(WCFURLs)).Value.Split('\n')); } catch { }
			}
			catch { }
		}

		static void SaveSettings()
		{
			try
			{
				var xml = new XElement("Settings");
				xml.Add(new XElement(nameof(DontExitOnClose), dontExitOnClose));
				xml.Add(new XElement(nameof(YouTubeDLPath), youTubeDLPath));
				xml.Add(new XElement(nameof(FFmpegPath), ffmpegPath));
				xml.Add(new XElement(nameof(FontSize), fontSize));
				xml.Add(new XElement(nameof(ShowSpecialChars), showSpecialChars));
				xml.Add(new XElement(nameof(WindowPosition), windowPosition));
				xml.Add(new XElement(nameof(WCFURLs), string.Join("\n", wcfURLs)));
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
			}
		}

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

		static double fontSize = 14;
		public static double FontSize
		{
			get { return fontSize; }
			set
			{
				fontSize = value;
				SaveSettings();
			}
		}

		static bool showSpecialChars = false;
		public static bool ShowSpecialChars
		{
			get { return showSpecialChars; }
			set
			{
				showSpecialChars = value;
				SaveSettings();
			}
		}

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

		public static List<string> wcfURLs = new List<string>();
		public static IReadOnlyList<string> WCFURLs => wcfURLs;
		public static void AddWCFUrl(string url)
		{
			wcfURLs.Remove(url);
			wcfURLs.Insert(0, url);
			SaveSettings();
		}

		public static bool CanExtract { get; } = string.IsNullOrEmpty(typeof(Settings).Assembly.Location);
	}
}
