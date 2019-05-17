using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace NeoEdit.Loader
{
	class Config : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		void RaisePropertyChanged(string property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));

		string x32Start;
		string x64Start;
		string x32Path;
		string x64Path;
		string password;
		string output;
		Regex match = new Regex(@"\.(exe|dll|txt)$", RegexOptions.IgnoreCase);
		bool canExtract;
		bool nGen;
		bool isConsole;
		ObservableCollection<ResourceHeader> resourceHeaders = new ObservableCollection<ResourceHeader>();

		public string X32Start { get { return x32Start; } private set { SetStrValue(ref x32Start, value); } }
		public string X64Start { get { return x64Start; } private set { SetStrValue(ref x64Start, value); } }
		public string X32Path { get { return x32Path; } private set { SetStrValue(ref x32Path, value); } }
		public string X64Path { get { return x64Path; } private set { SetStrValue(ref x64Path, value); } }
		public string Password { get { return password; } set { SetStrValue(ref password, value); } }
		public string Output { get { return output; } set { SetStrValue(ref output, value); } }
		public string Match { get { return match.ToString(); } set { SetValue(ref match, new Regex(value, RegexOptions.IgnoreCase)); } }
		public bool CanExtract { get { return canExtract; } set { SetValue(ref canExtract, value); } }
		public bool NGen { get { return nGen; } set { SetValue(ref nGen, value); } }
		public bool IsConsole { get { return isConsole; } set { SetValue(ref isConsole, value); } }
		public ObservableCollection<ResourceHeader> ResourceHeaders { get { return resourceHeaders; } set { SetValue(ref resourceHeaders, value); } }

		public string X32StartFull => X32Start == null ? null : Path.Combine(X32Path, X32Start);
		public string X64StartFull => X64Start == null ? null : Path.Combine(X64Path, X64Start);

		public bool IsMatch(string filename) => match.IsMatch(filename);

		public byte[] ToBytes()
		{
			using (var ms = new MemoryStream())
			using (var writer = new BinaryWriter(ms, Encoding.UTF8, true))
			{
				writer.Write(X32Start ?? "");
				writer.Write(X64Start ?? "");
				writer.Write(CanExtract);
				writer.Write(NGen);
				writer.Write(Password != null);

				writer.Write(ResourceHeaders.Count);
				foreach (var resourceHeader in ResourceHeaders)
				{
					var data = resourceHeader.ToBytes();
					writer.Write(data.Length);
					writer.Write(data);
				}
				return ms.ToArray();
			}
		}

		public static Config FromBytes(byte[] data)
		{
			using (var ms = new MemoryStream(data))
			using (var reader = new BinaryReader(ms, Encoding.UTF8, true))
			{
				var config = new Config();
				config.X32Start = reader.ReadString();
				config.X64Start = reader.ReadString();
				config.CanExtract = reader.ReadBoolean();
				config.NGen = reader.ReadBoolean();
				config.Password = reader.ReadBoolean() ? "prompt" : null;

				config.ResourceHeaders.Clear();
				var resourceHeaderCount = reader.ReadInt32();
				for (var ctr = 0; ctr < resourceHeaderCount; ++ctr)
					config.ResourceHeaders.Add(ResourceHeader.FromBytes(reader.ReadBytes(reader.ReadInt32())));
				return config;
			}
		}

		public void SetStart(string fileName)
		{
			var peInfo = new PEInfo(fileName);
			if (peInfo.BitDepth.HasFlag(BitDepths.x32))
			{
				X32Start = Path.GetFileName(fileName);
				X32Path = Path.GetDirectoryName(fileName);
			}
			if (peInfo.BitDepth.HasFlag(BitDepths.x64))
			{
				X64Start = Path.GetFileName(fileName);
				X64Path = Path.GetDirectoryName(fileName);
			}

			IsConsole = peInfo.IsConsole;

			var entry = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
			for (var ctr = 1; ; ++ctr)
			{
				var output = Path.Combine(entry, Path.GetFileNameWithoutExtension(fileName) + (ctr == 1 ? "" : ctr.ToString()) + Path.GetExtension(fileName));
				if (File.Exists(output))
					continue;
				Output = output;
				break;
			}
		}

		public void SetPath(string path)
		{
			bool done = false;
			while (path.EndsWith("\\"))
				path = path.Substring(0, path.Length - 1);

			if (X32StartFull?.StartsWith(path + "\\", StringComparison.OrdinalIgnoreCase) == true)
			{
				X32Start = X32StartFull.Substring(path.Length + 1);
				X32Path = path;
				done = true;
			}

			if (X64StartFull?.StartsWith(path + "\\", StringComparison.OrdinalIgnoreCase) == true)
			{
				X64Start = X64StartFull.Substring(path.Length + 1);
				X64Path = path;
				done = true;
			}

			if (!done)
				throw new Exception("Invalid path; must be path of x32 or x64 start");
		}

		void SetValue<T>(ref T str, T value, [CallerMemberName] string property = "")
		{
			if (str.Equals(value))
				return;
			str = value;
			RaisePropertyChanged(property);
		}

		void SetStrValue(ref string str, string value, [CallerMemberName] string property = "")
		{
			if (string.IsNullOrWhiteSpace(value))
				value = null;
			if (str == value)
				return;
			str = value;
			RaisePropertyChanged(property);
		}
	}
}
