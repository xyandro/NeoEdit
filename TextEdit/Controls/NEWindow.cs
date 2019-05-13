using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using NeoEdit.TextEdit;
using NeoEdit.TextEdit.Misc;

namespace NeoEdit.TextEdit.Controls
{
	public class NEWindow : Window
	{
		static readonly string settingsFile = Path.Combine(Helpers.NeoEditAppData, "Settings.xml");

		static NEWindow()
		{
			if (File.Exists(settingsFile))
			{
				try
				{
					var xml = XElement.Load(settingsFile);
					try { minimizeToTray = bool.Parse(xml.Element(nameof(MinimizeToTray)).Value); } catch { }
					try { escapeClearsSelections = bool.Parse(xml.Element(nameof(EscapeClearsSelections)).Value); } catch { }
					try { youTubeDLPath = xml.Element(nameof(YouTubeDLPath)).Value; } catch { }
					try { ffmpegPath = xml.Element(nameof(FFmpegPath)).Value; } catch { }
					try { Font.FontSize = int.Parse(xml.Element(nameof(Font.FontSize)).Value); } catch { }
				}
				catch { }
			}

			Font.FontSizeChanged += newSize => SaveSettings();
		}

		static void SaveSettings()
		{
			try
			{
				var xml = new XElement("Settings");
				xml.Add(new XElement(nameof(MinimizeToTray), minimizeToTray));
				xml.Add(new XElement(nameof(EscapeClearsSelections), escapeClearsSelections));
				xml.Add(new XElement(nameof(YouTubeDLPath), youTubeDLPath));
				xml.Add(new XElement(nameof(FFmpegPath), ffmpegPath));
				xml.Add(new XElement(nameof(Font.FontSize), Font.FontSize));
				xml.Save(settingsFile);
			}
			catch { }
		}

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

		static bool minimizeToTray = false;
		public static bool MinimizeToTray
		{
			get { return minimizeToTray; }
			set
			{
				minimizeToTray = value;
				SaveSettings();
				minimizeToTrayChanged?.Invoke(null, new EventArgs());
			}
		}

		static EventHandler minimizeToTrayChanged;
		public static event EventHandler MinimizeToTrayChanged { add { minimizeToTrayChanged += value; } remove { minimizeToTrayChanged -= value; } }

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

		System.Windows.Forms.NotifyIcon ni;
		protected override void OnStateChanged(EventArgs e)
		{
			base.OnStateChanged(e);
			if (WindowState == WindowState.Minimized)
			{
				if (MinimizeToTray)
				{
					ni = new System.Windows.Forms.NotifyIcon
					{
						Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetEntryAssembly().Location),
						Visible = true,
					};
					ni.Click += (s, e2) => Restore();
					Hide();
				}
			}
		}

		public bool Restore()
		{
			if (ni == null)
				return false;

			base.Show();
			WindowState = WindowState.Normal;
			ni.Dispose();
			ni = null;
			return true;
		}
	}
}
