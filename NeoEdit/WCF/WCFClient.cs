using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Web.Services.Discovery;
using System.Xml;
using System.Xml.Schema;
using Microsoft.CSharp;
using ServiceDescription = System.Web.Services.Description.ServiceDescription;

namespace NeoEdit.Program.WCF
{
	public class WCFClient
	{
		Collection<Binding> Bindings { get; set; }
		public Collection<ContractDescription> Contracts { get; set; }
		Collection<ServiceEndpoint> Endpoints { get; set; }
		public string Config { get; set; }

		readonly CodeCompileUnit codeCompileUnit = new CodeCompileUnit();
		readonly CodeDomProvider codeDomProvider = new CSharpCodeProvider();
		Assembly compiledAssembly;

		public WCFClient(string serviceUrl)
		{
			GetMetadata(serviceUrl);
			GenerateConfig();
			CompileProxyAssembly();
		}

		public object CreateInstance(string contractName, string contractNamespace)
		{
			var serviceEndpoint = GetServiceEndpoint(contractName, contractNamespace);
			return Activator.CreateInstance(GetProxyType(serviceEndpoint), GetBinding(serviceEndpoint), serviceEndpoint.Address);
		}

		Type GetProxyType(ServiceEndpoint serviceEndpoint)
		{
			var allTypes = compiledAssembly.GetTypes();

			Type serviceContractInterface = null;
			foreach (var type in allTypes)
			{
				if (!type.IsInterface)
					continue;
				var attribute = type.GetCustomAttribute<ServiceContractAttribute>();
				if (attribute == null)
					continue;
				var contractName = new XmlQualifiedName(attribute.Name ?? type.Name, string.IsNullOrWhiteSpace(attribute.Namespace) ? "http://tempuri.org/" : Uri.EscapeUriString(attribute.Namespace));
				if ((string.Compare(contractName.Name, serviceEndpoint.Contract.Name, StringComparison.OrdinalIgnoreCase) != 0) || (string.Compare(contractName.Namespace, serviceEndpoint.Contract.Namespace, StringComparison.OrdinalIgnoreCase) != 0))
					continue;
				serviceContractInterface = type;
				break;
			}
			if (serviceContractInterface == null)
				throw new Exception($"{nameof(GetProxyType)} failed");

			var serviceContractType = allTypes.Where(type => (type.IsClass) && (serviceContractInterface.IsAssignableFrom(type)) && (type.IsSubclassOf(typeof(ClientBase<>).MakeGenericType(serviceContractInterface)))).FirstOrDefault();
			if (serviceContractType == null)
				throw new Exception($"{nameof(GetProxyType)} failed");
			return serviceContractType;
		}

		ServiceEndpoint GetServiceEndpoint(string contractName, string contractNamespace)
		{
			var endpoint = Endpoints.FirstOrDefault(ep => (string.Compare(ep.Contract.Name, contractName, StringComparison.OrdinalIgnoreCase) == 0) && (string.Compare(ep.Contract.Namespace, contractNamespace, StringComparison.OrdinalIgnoreCase) == 0));
			if (endpoint == null)
				throw new Exception($"{nameof(GetServiceEndpoint)} failed");
			return endpoint;
		}

		static Binding GetBinding(ServiceEndpoint endpoint)
		{
			Binding binding;
			if (endpoint.Binding is WSHttpBinding)
			{
				var wsHttpBinding = new WSHttpBinding
				{
					MaxReceivedMessageSize = int.MaxValue,
					MaxBufferPoolSize = int.MaxValue,
					ReceiveTimeout = TimeSpan.FromHours(24),
					ReaderQuotas = { MaxArrayLength = int.MaxValue, MaxBytesPerRead = int.MaxValue, MaxDepth = int.MaxValue, MaxNameTableCharCount = int.MaxValue },
					Name = endpoint.Binding.Name,
					CloseTimeout = TimeSpan.FromSeconds(120),
					Namespace = endpoint.Binding.Namespace,
				};

				if (endpoint.Binding.Scheme == Uri.UriSchemeHttps)
					wsHttpBinding.Security = new WSHttpSecurity { Mode = SecurityMode.Transport, Transport = new HttpTransportSecurity { ClientCredentialType = HttpClientCredentialType.Windows } };
				else
					wsHttpBinding.Security = new WSHttpSecurity { Mode = SecurityMode.None };

				binding = wsHttpBinding;
			}
			else if (endpoint.Binding is BasicHttpBinding)
			{
				var basicHttpBinding = new BasicHttpBinding
				{
					MaxReceivedMessageSize = int.MaxValue,
					MaxBufferPoolSize = int.MaxValue,
					ReceiveTimeout = TimeSpan.FromHours(24),
					ReaderQuotas = { MaxArrayLength = int.MaxValue, MaxBytesPerRead = int.MaxValue, MaxDepth = int.MaxValue, MaxNameTableCharCount = int.MaxValue },
					Name = endpoint.Binding.Name,
					CloseTimeout = TimeSpan.FromSeconds(120),
					Namespace = endpoint.Binding.Namespace,
				};

				if (endpoint.Binding.Scheme == Uri.UriSchemeHttps)
					basicHttpBinding.Security = new BasicHttpSecurity { Mode = BasicHttpSecurityMode.Transport, Transport = new HttpTransportSecurity { ClientCredentialType = HttpClientCredentialType.Windows } };
				else
					basicHttpBinding.Security = new BasicHttpSecurity { Mode = BasicHttpSecurityMode.None };

				binding = basicHttpBinding;
			}
			else
				binding = endpoint.Binding;
			return binding;
		}

		static MetadataExchangeClient CreateMetadataExchangeClient(Uri serviceUri)
		{
			if (serviceUri.Scheme == Uri.UriSchemeHttp)
			{
				var wsHttpBinding = (WSHttpBinding)MetadataExchangeBindings.CreateMexHttpBinding();
				wsHttpBinding.MaxReceivedMessageSize = 67108864L;
				wsHttpBinding.ReaderQuotas.MaxNameTableCharCount = 1048576;
				return new MetadataExchangeClient(wsHttpBinding);
			}

			if (serviceUri.Scheme == Uri.UriSchemeHttps)
			{
				var wsHttpBinding = (WSHttpBinding)MetadataExchangeBindings.CreateMexHttpsBinding();
				wsHttpBinding.MaxReceivedMessageSize = 67108864L;
				wsHttpBinding.ReaderQuotas.MaxNameTableCharCount = 1048576;
				return new MetadataExchangeClient(wsHttpBinding);
			}

			if (serviceUri.Scheme == Uri.UriSchemeNetTcp)
			{
				var tcpBinding = (CustomBinding)MetadataExchangeBindings.CreateMexTcpBinding();
				tcpBinding.Elements.Find<TcpTransportBindingElement>().MaxReceivedMessageSize = 67108864L;
				return new MetadataExchangeClient(tcpBinding);
			}

			if (serviceUri.Scheme == Uri.UriSchemeNetPipe)
			{
				var namedPipeBinding = (CustomBinding)MetadataExchangeBindings.CreateMexNamedPipeBinding();
				namedPipeBinding.Elements.Find<NamedPipeTransportBindingElement>().MaxReceivedMessageSize = 67108864L;
				return new MetadataExchangeClient(namedPipeBinding);
			}

			return null;
		}

		static Collection<MetadataSection> TryDownloadByMetadataExchangeClient(Uri serviceUri)
		{
			try
			{
				var mexClient = CreateMetadataExchangeClient(serviceUri);
				if (mexClient == null)
					return null;
				mexClient.OperationTimeout = TimeSpan.FromSeconds(30);
				return mexClient.GetMetadata().MetadataSections;
			}
			catch
			{
				return null;
			}
		}

		static Uri GetDefaultMexUri(Uri serviceUri) => serviceUri.AbsoluteUri.EndsWith("/", StringComparison.OrdinalIgnoreCase) ? new Uri(serviceUri, "./mex") : new Uri(serviceUri.AbsoluteUri + "/mex");

		static MetadataSection GetMetadataSection(object document)
		{
			if (document is ServiceDescription serviceDescription)
				return MetadataSection.CreateFromServiceDescription(serviceDescription);
			if (document is XmlSchema xmlSchema)
				return MetadataSection.CreateFromSchema(xmlSchema);
			if ((document is XmlElement xmlElement) && ((xmlElement.NamespaceURI == "http://schemas.xmlsoap.org/ws/2004/09/policy") || (xmlElement.NamespaceURI == "http://www.w3.org/ns/ws-policy")) && (xmlElement.LocalName == "Policy"))
				return MetadataSection.CreateFromPolicy(xmlElement, null);
			return new MetadataSection { Metadata = document };
		}

		void GetMetadata(string serviceUrl)
		{
			var serviceUri = new Uri(serviceUrl);

			var metadata = TryDownloadByMetadataExchangeClient(serviceUri);
			if (metadata == null)
				metadata = TryDownloadByMetadataExchangeClient(GetDefaultMexUri(serviceUri));
			if ((metadata == null) && (serviceUri.Scheme == Uri.UriSchemeHttp) || (serviceUri.Scheme == Uri.UriSchemeHttps))
			{
				var discoveryClientProtocol = new DiscoveryClientProtocol { AllowAutoRedirect = true, UseDefaultCredentials = true };
				ServicePointManager.ServerCertificateValidationCallback += (se, cert, chain, sslerror) => true;
				discoveryClientProtocol.DiscoverAny(serviceUrl);
				discoveryClientProtocol.ResolveAll();

				if (discoveryClientProtocol.Documents.Values != null)
				{
					metadata = new Collection<MetadataSection>();
					foreach (var document in discoveryClientProtocol.Documents.Values)
						metadata.Add(GetMetadataSection(document));
				}
			}

			var importer = new WsdlImporter(new MetadataSet(metadata));
			var xsdDataContractImporter = new XsdDataContractImporter(codeCompileUnit) { Options = new ImportOptions { GenerateSerializable = true, GenerateInternal = false, ImportXmlType = true, EnableDataBinding = false, CodeProvider = codeDomProvider } };
			importer.State.Add(typeof(XsdDataContractImporter), xsdDataContractImporter);

			foreach (var importExtension in importer.WsdlImportExtensions)
				if (importExtension is DataContractSerializerMessageContractImporter dataContractSerializerMessageContractImporter)
					dataContractSerializerMessageContractImporter.Enabled = true;

			if (!importer.State.ContainsKey(typeof(WrappedOptions)))
				importer.State.Add(typeof(WrappedOptions), new WrappedOptions { WrappedFlag = false });

			Bindings = importer.ImportAllBindings();
			Contracts = importer.ImportAllContracts();
			Endpoints = importer.ImportAllEndpoints();

			if (importer.Errors?.Any(error => !error.IsWarning) == true)
				throw new Exception($"{nameof(GetMetadata)} failed");
		}

		void GenerateConfig()
		{
			var tempConfigFileName = Path.GetTempFileName();
			File.WriteAllText(tempConfigFileName, "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\r\n<configuration>\r\n</configuration>");

			var config = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap { ExeConfigFilename = tempConfigFileName, MachineConfigFilename = ConfigurationManager.OpenMachineConfiguration().FilePath }, ConfigurationUserLevel.None);

			var contractGenerator = new ServiceContractGenerator(codeCompileUnit, config);
			contractGenerator.Options |= ServiceContractGenerationOptions.ClientClass;

			foreach (var contract in Contracts)
				contractGenerator.GenerateServiceContractType(contract);
			foreach (var endpoint in Endpoints)
				contractGenerator.GenerateServiceEndpoint(endpoint, out var channelEndpointElement);

			if (contractGenerator.Errors?.Any(error => !error.IsWarning) == true)
				throw new Exception($"{nameof(GenerateConfig)} failed");

			config.NamespaceDeclared = false;
			config.Save();

			Config = File.ReadAllText(tempConfigFileName);
			File.Delete(tempConfigFileName);
		}

		void RemoveDuplicateTypes()
		{
			var existingTypes = new HashSet<string>(AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).Select(x => x.FullName));
			foreach (CodeNamespace ns in codeCompileUnit.Namespaces)
			{
				var useNS = string.IsNullOrEmpty(ns.Name) ? "" : $"{ns.Name}.";
				var duplicates = ns.Types.OfType<CodeTypeDeclaration>().Where(t => existingTypes.Contains($"{useNS}{t.Name}")).ToList();
				duplicates.ForEach(t => ns.Types.Remove(t));
			}
		}

		void CompileProxyAssembly()
		{
			RemoveDuplicateTypes();

			string proxyCode;
			using (var writer = new StringWriter())
			{
				codeDomProvider.GenerateCodeFromCompileUnit(codeCompileUnit, writer, new CodeGeneratorOptions { BracingStyle = "C" });
				writer.Flush();
				proxyCode = writer.ToString();
			}

			var compilerParameters = new CompilerParameters();
			compilerParameters.ReferencedAssemblies.Add(typeof(ServiceContractAttribute).Assembly.Location);
			compilerParameters.ReferencedAssemblies.Add(typeof(ServiceDescription).Assembly.Location);
			compilerParameters.ReferencedAssemblies.Add(typeof(DataContractAttribute).Assembly.Location);
			compilerParameters.ReferencedAssemblies.Add(typeof(XmlElement).Assembly.Location);
			compilerParameters.ReferencedAssemblies.Add(typeof(Uri).Assembly.Location);
			compilerParameters.ReferencedAssemblies.Add(typeof(DataSet).Assembly.Location);

			var compilerResults = codeDomProvider.CompileAssemblyFromSource(compilerParameters, proxyCode);
			compiledAssembly = Assembly.LoadFile(compilerResults.PathToAssembly);
		}
	}
}
