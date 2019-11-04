using System;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace TestWCF
{
	class Program
	{
		static void Main()
		{
			var baseAddress = new Uri("http://localhost:8080/hello");
			using (var host = new ServiceHost(typeof(TestEndPoint), baseAddress))
			{
				var smb = new ServiceMetadataBehavior { HttpGetEnabled = true };
				smb.MetadataExporter.PolicyVersion = PolicyVersion.Policy15;
				host.Description.Behaviors.Add(smb);

				host.Open();

				Console.WriteLine("The service is ready at {0}", baseAddress);
				Console.WriteLine("Press 'Q' to stop the service.");
				while (Console.ReadKey(true).Key != ConsoleKey.Q) { }

				host.Close();
			}
		}
	}
}
