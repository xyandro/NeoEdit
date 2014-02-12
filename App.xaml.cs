using System;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace NeoEdit
{
	public partial class App : Application
	{
		App()
		{
			foreach (var arg in Environment.GetCommandLineArgs().Skip(1))
			{
				if (arg.StartsWith("test"))
				{
					var tests = Assembly.GetExecutingAssembly().GetTypes().Where(type => (type.IsClass) && (type.Namespace == "NeoEdit.Test")).ToList();
					if (arg.StartsWith("test="))
						tests = tests.Where(type => type.Name == arg.Substring(5)).ToList();

					foreach (var test in tests)
					{
						var run = test.GetMethod("Run", BindingFlags.Public | BindingFlags.Static);
						if (run != null)
							try { run.Invoke(null, null); }
							catch (Exception ex) { throw ex; }
					}
				}
				Application.Current.Shutdown();
			}
		}
	}
}
