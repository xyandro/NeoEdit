using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Services.Discovery;
using System.Xml;
using System.Xml.Schema;
using Microsoft.CSharp;
using NeoEdit.Program.Dialogs;
using Newtonsoft.Json;
using ServiceDescription = System.Web.Services.Description.ServiceDescription;

namespace NeoEdit.Program
{
	public class WCFClient : MarshalByRefObject
	{
		class WCFConfig
		{
			public List<WCFOperation> Operations { get; } = new List<WCFOperation>();
			public string Config { get; set; }
		}

		class WCFOperation
		{
			public string ServiceURL { get; set; }
			public string Namespace { get; set; }
			public string Contract { get; set; }
			public string Operation { get; set; }
			public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
			public object Result { get; set; }
		}

		readonly static List<AppDomain> appDomains = new List<AppDomain>();
		readonly static Dictionary<string, WCFClient> wcfClients = new Dictionary<string, WCFClient>();

		static WCFClient GetWCFClient(string serviceURL)
		{
			if (!wcfClients.ContainsKey(serviceURL))
			{
				var wcfClientAssemblyName = typeof(WCFClient).Assembly.Location;
				if (string.IsNullOrWhiteSpace(wcfClientAssemblyName))
					throw new Exception($"Can't resolve {nameof(WCFClient)} assembly. It most likely needs to be extracted from the loader.");

				var appDomain = AppDomain.CreateDomain(serviceURL);
				try
				{
					wcfClients[serviceURL] = appDomain.CreateInstanceFromAndUnwrap(wcfClientAssemblyName, typeof(WCFClient).FullName) as WCFClient;
					wcfClients[serviceURL].Load(serviceURL);
				}
				catch
				{
					wcfClients.Remove(serviceURL);
					AppDomain.Unload(appDomain);
					throw;
				}
				appDomains.Add(appDomain);
			}

			return wcfClients[serviceURL];
		}

		static public List<string> InterceptCalls(string serviceURL, string interceptURL)
		{
			var wcfClient = GetWCFClient(serviceURL);
			wcfClient.StartInterceptCalls(interceptURL);
			WCFInterceptDialog.Run();
			return wcfClient.EndInterceptCalls();
		}

		static public void ResetClients()
		{
			foreach (var appDomain in appDomains)
				AppDomain.Unload(appDomain);
			appDomains.Clear();
			wcfClients.Clear();
		}

		static public string GetWCFConfig(string serviceURL) => GetWCFClient(serviceURL).DoGetWCFConfig();

		static public string ExecuteWCF(string str)
		{
			str = Regex.Replace(str, @"/\*.*?\*/", "", RegexOptions.Singleline | RegexOptions.Multiline);
			var client = GetWCFClient(JsonConvert.DeserializeObject<WCFOperation>(str).ServiceURL);
			return client.DoExecuteWCF(str);
		}

		public string ServiceURL { get; private set; }

		Collection<Binding> Bindings { get; set; }
		public Collection<ContractDescription> Contracts { get; set; }
		Collection<ServiceEndpoint> Endpoints { get; set; }
		public string Config { get; set; }

		readonly CodeCompileUnit codeCompileUnit = new CodeCompileUnit();
		readonly CodeDomProvider codeDomProvider = new CSharpCodeProvider();
		Assembly compiledAssembly;

		void Load(string serviceUrl)
		{
			ServiceURL = serviceUrl;
			GetMetadata();
			GenerateConfig();
			CompileProxyAssembly();
		}

		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.Infrastructure)]
		public override object InitializeLifetimeService() => null;

		public object CreateInstance(string contractNamespace, string contractName)
		{
			var serviceEndpoint = GetServiceEndpoint(contractNamespace, contractName);
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
				if (string.Compare(string.IsNullOrWhiteSpace(attribute.Namespace) ? "http://tempuri.org/" : attribute.Namespace, serviceEndpoint.Contract.Namespace, StringComparison.OrdinalIgnoreCase) != 0)
					continue;
				if (string.Compare(attribute.Name ?? type.Name, serviceEndpoint.Contract.Name, StringComparison.OrdinalIgnoreCase) != 0)
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

		ServiceEndpoint GetServiceEndpoint(string contractNamespace, string contractName)
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
					SendTimeout = TimeSpan.FromSeconds(10),
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
					SendTimeout = TimeSpan.FromSeconds(10),
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

		void GetMetadata()
		{
			var serviceUri = new Uri(ServiceURL);

			var metadata = TryDownloadByMetadataExchangeClient(serviceUri);
			if (metadata == null)
				metadata = TryDownloadByMetadataExchangeClient(GetDefaultMexUri(serviceUri));
			if ((metadata == null) && (serviceUri.Scheme == Uri.UriSchemeHttp) || (serviceUri.Scheme == Uri.UriSchemeHttps))
			{
				var discoveryClientProtocol = new DiscoveryClientProtocol { AllowAutoRedirect = true, UseDefaultCredentials = true };
				ServicePointManager.ServerCertificateValidationCallback += (se, cert, chain, sslerror) => true;
				discoveryClientProtocol.DiscoverAny(ServiceURL);
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

		void AddInterceptor(StringWriter writer)
		{
			writer.Write($@"
namespace NeoEdit.WCFInterceptor
{{
	using System;
	using System.Collections.Generic;
	using System.ServiceModel;
	using System.ServiceModel.Description;

");

			var namespaceCtr = 0;
			var tuples = new List<Tuple<string, string, string, string, CodeMemberMethod>>();
			foreach (CodeNamespace ns in codeCompileUnit.Namespaces)
				foreach (CodeTypeDeclaration codeType in ns.Types)
				{
					var attr = codeType.CustomAttributes.OfType<CodeAttributeDeclaration>().FirstOrDefault(codeAttribute => codeAttribute.Name == typeof(ServiceContractAttribute).FullName);
					if (attr == null)
						continue;

					var contractNamespace = attr.Arguments.OfType<CodeAttributeArgument>().Where(x => x.Name == "Namespace").Select(x => x.Value).OfType<CodePrimitiveExpression>().Select(x => x.Value).OfType<string>().DefaultIfEmpty("http://tempuri.org/").First();
					var contractName = attr.Arguments.OfType<CodeAttributeArgument>().Where(x => x.Name == "Name").Select(x => x.Value).OfType<CodePrimitiveExpression>().Select(x => x.Value).OfType<string>().DefaultIfEmpty(codeType.Name).First();
					var codeNamespace = $"Interceptor{++namespaceCtr}";

					writer.Write($@"
	namespace {codeNamespace}
	{{
		[ServiceContract(Namespace = @""{contractNamespace.Replace(@"""", @"""""")}"")]
		public interface {contractName}
		{{
");
					foreach (CodeTypeMember codeTypeMember in codeType.Members)
						if (codeTypeMember is CodeMemberMethod codeMemberMethod)
						{
							if (!codeMemberMethod.CustomAttributes.OfType<CodeAttributeDeclaration>().Any(codeAttribute => codeAttribute.Name == typeof(OperationContractAttribute).FullName))
								continue;

							var returnType = codeMemberMethod.ReturnType.BaseType;
							if (returnType == typeof(void).FullName)
								returnType = "void";

							writer.Write($@"
			[OperationContract] {returnType} {codeMemberMethod.Name}({string.Join(", ", codeMemberMethod.Parameters.OfType<CodeParameterDeclarationExpression>().Select(param => $"{param.Type.BaseType} {param.Name}"))});
");
							tuples.Add(Tuple.Create(returnType, codeNamespace, contractNamespace, contractName, codeMemberMethod));
						}

					writer.Write($@"
		}}
	}}
");
				}

			writer.Write($@"
	public class InterceptorImplementation : {string.Join(", ", tuples.Select(tuple => $"{tuple.Item2}.{tuple.Item4}").Distinct())}
	{{
");

			foreach (var tuple in tuples)
			{
				writer.Write($@"
		{tuple.Item1} {tuple.Item2}.{tuple.Item4}.{tuple.Item5.Name}({string.Join(", ", tuple.Item5.Parameters.OfType<CodeParameterDeclarationExpression>().Select(param => $"{param.Type.BaseType} {param.Name}"))})
		{{
			{(tuple.Item1 == "void" ? "" : $"return ({tuple.Item1})")}Interceptor.CurInterceptor.AddCall(@""{tuple.Item3.Replace(@"""", @"""""")}"", ""{tuple.Item4}"", ""{tuple.Item5.Name}"", new Dictionary<string, object> {{ {string.Join(", ", tuple.Item5.Parameters.OfType<CodeParameterDeclarationExpression>().Select(param => $@"{{ ""{param.Name}"", {param.Name} }}"))} }});
		}}
");
			}

			writer.Write($@"
	}}

	public class Interceptor
	{{
		public class Call
		{{
			public string Namespace {{ get; set; }}
			public string Contract {{ get; set; }}
			public string Operation {{ get; set; }}
			public Dictionary<string, object> Parameters {{ get; set; }}
			public object Result {{ get; set; }}
		}}

		static public Interceptor CurInterceptor {{ get; set; }}

		public List<Call> Calls {{ get; private set; }}
		Func<string, string, string, Dictionary<string, object>, object> getResult;
		ServiceHost host;

		public Interceptor()
		{{
			CurInterceptor = this;
			Calls = new List<Call>();
		}}

		public void Start(string uri, Func<string, string, string, Dictionary<string, object>, object> getResult)
		{{
			this.getResult = getResult;

			host = new ServiceHost(typeof(InterceptorImplementation), new Uri(uri));
			var smb = new ServiceMetadataBehavior {{ HttpGetEnabled = true }};
			smb.MetadataExporter.PolicyVersion = PolicyVersion.Policy15;
			host.Description.Behaviors.Add(smb);
			host.Open();
		}}

		public void End()
		{{
			host.Close();
		}}

		public object AddCall(string @namespace, string contract, string operation, Dictionary<string, object> parameters)
		{{
			var result = getResult(@namespace, contract, operation, parameters);
			Calls.Add(new Call
			{{
				Namespace = @namespace,
				Contract = contract,
				Operation = operation,
				Parameters = parameters,
				Result = result,
			}});
			return result;
		}}
	}}
}}
");
		}

		void CompileProxyAssembly()
		{
			RemoveDuplicateTypes();

			string proxyCode;
			using (var writer = new StringWriter())
			{
				codeDomProvider.GenerateCodeFromCompileUnit(codeCompileUnit, writer, new CodeGeneratorOptions { BracingStyle = "C" });
				AddInterceptor(writer);
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

		object CreateWCFDefaultObject(Type type) => rCreateWCFDefaultObject(type, new HashSet<Type>());

		object rCreateWCFDefaultObject(Type type, HashSet<Type> seen)
		{
			try
			{
				type = Nullable.GetUnderlyingType(type) ?? type;

				if (type == typeof(string))
					return "";
				if (type == typeof(DateTime))
					return DateTime.Now;
				if (type == typeof(DateTimeOffset))
					return DateTimeOffset.Now;

				if (type.IsArray)
				{
					var listItem = rCreateWCFDefaultObject(type.GetElementType(), seen);
					var array = Activator.CreateInstance(type, listItem == null ? 0 : 1);
					if (listItem != null)
						((Array)array).SetValue(listItem, 0);
					return array;
				}

				if (type.IsEnum)
				{
					var values = Enum.GetValues(type);
					if (values.Length > 0)
						return values.GetValue(0);
					return 0;
				}

				var obj = Activator.CreateInstance(type);

				if (type.GetCustomAttribute<DataContractAttribute>() != null)
				{
					// If we get back to the same type, it will happen again. Break the loop.
					if (seen.Contains(type))
						return null;
					seen.Add(type);

					foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
					{
						if ((!prop.CanWrite) || (prop.GetCustomAttribute<DataMemberAttribute>() == null))
							continue;
						var value = rCreateWCFDefaultObject(prop.PropertyType, seen);
						if (value != null)
							try { prop.SetValue(obj, value); } catch { }
					}

					seen.Remove(type);
				}

				var dictInterface = type.GetInterfaces().Where(i => (i.IsGenericType) && (i.GetGenericTypeDefinition() == typeof(IDictionary<,>))).FirstOrDefault();
				if ((obj is IDictionary dict) && (dictInterface != null))
				{
					var keyType = dictInterface.GetGenericArguments()[0];
					var valueType = dictInterface.GetGenericArguments()[1];
					dict.Add(rCreateWCFDefaultObject(keyType, seen), rCreateWCFDefaultObject(valueType, seen));
				}

				return obj;
			}
			catch { }
			return null;
		}

		void DoResetClients() => wcfClients.Clear();

		string DoGetWCFConfig()
		{
			try
			{
				var wcfConfig = new WCFConfig();
				foreach (var contract in Contracts)
					using (var instance = CreateInstance(contract.Namespace, contract.Name) as IDisposable)
						foreach (var operation in contract.Operations)
						{
							var wcfOperation = new WCFOperation { ServiceURL = ServiceURL, Namespace = contract.Namespace, Contract = contract.Name, Operation = operation.Name };
							foreach (var parameter in instance.GetType().GetMethod(operation.Name).GetParameters())
								wcfOperation.Parameters[parameter.Name] = CreateWCFDefaultObject(parameter.ParameterType);
							wcfConfig.Operations.Add(wcfOperation);
						}
				wcfConfig.Config = Config;

				return ToJSON(wcfConfig, new HashSet<object>(wcfConfig.Operations.SelectMany(c => c.Parameters.Values)));
			}
			catch (Exception ex) { throw AggregateException(ex); }
		}

		string DoExecuteWCF(string str)
		{
			try
			{
				var wcfOperation = JsonConvert.DeserializeObject<WCFOperation>(str);
				wcfOperation.Result = DoExecuteWCF(wcfOperation.Namespace, wcfOperation.Contract, wcfOperation.Operation, wcfOperation.Parameters);
				return ToJSON(wcfOperation, new HashSet<object>(wcfOperation.Parameters.Values));
			}
			catch (Exception ex) { throw AggregateException(ex); }
		}

		object DoExecuteWCF(string namespaceName, string contractName, string operationName, Dictionary<string, object> useParameters)
		{
			try
			{
				var contract = Contracts.FirstOrDefault(x => (x.Namespace == namespaceName) && (x.Name == contractName));
				if (contract == null)
					throw new Exception($"Contract not found: {contractName}");

				var operation = contract.Operations.FirstOrDefault(x => x.Name == operationName);
				if (operation == null)
					throw new Exception($"Operation not found: {operationName}");

				using (var instance = CreateInstance(contract.Namespace, contract.Name) as IDisposable)
				{
					var method = instance.GetType().GetMethod(operation.Name);
					var parameters = new List<object>();
					foreach (var parameter in method.GetParameters())
					{
						if (!useParameters.ContainsKey(parameter.Name))
							throw new Exception($"Missing parameter: {parameter.Name}");
						var param = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(useParameters[parameter.Name]), parameter.ParameterType);
						useParameters[parameter.Name] = param;
						parameters.Add(param);
					}

					return method.Invoke(instance, parameters.ToArray());
				}
			}
			catch (Exception ex) { throw AggregateException(ex); }
		}

		Exception AggregateException(Exception ex)
		{
			var sb = new StringBuilder();
			for (var x = ex; x != null; x = x.InnerException)
				sb.AppendLine(x.Message);
			return new Exception(sb.ToString());
		}

		static string GetTypeName(Type type)
		{
			if (type.IsArray)
				return $"{GetTypeName(type.GetElementType())}[]";

			var nullableType = Nullable.GetUnderlyingType(type);
			if (nullableType != null)
				return $"{GetTypeName(nullableType)}?";

			if ((type.IsGenericType) && (!type.IsGenericTypeDefinition))
			{
				var name = GetTypeName(type.GetGenericTypeDefinition());
				return $"{name.Substring(0, name.IndexOf('<'))}<{string.Join(", ", type.GenericTypeArguments.Select(a => GetTypeName(a)))}>";
			}

			using (var provider = new CSharpCodeProvider())
			{
				var typeRef = new CodeTypeReference(type);
				var typeName = provider.GetTypeOutput(typeRef);
				var lastDot = typeName.LastIndexOf('.');
				if (lastDot != -1)
					typeName = typeName.Substring(lastDot + 1);

				return typeName;
			}
		}

		static string ToJSON<T>(T obj, HashSet<object> labelTypeObjects) => ToJSON(typeof(T), obj, labelTypeObjects);

		static string ToJSON(Type type, object obj, HashSet<object> labelTypeObjects)
		{
			var sb = new StringBuilder();
			rToJSON(type, obj, sb, "", false, labelTypeObjects);
			sb.Append("\r\n");
			return sb.ToString();
		}

		static void rToJSON(Type showType, object obj, StringBuilder sb, string spacing, bool labelTypes, HashSet<object> labelTypeObjects)
		{
			void AddType(bool spaceBefore, bool spaceAfter)
			{
				if (!labelTypes)
					return;

				if (spaceBefore)
					sb.Append(" ");

				var optionsType = Nullable.GetUnderlyingType(showType) ?? showType;
				var options = optionsType.IsEnum ? $" ({string.Join(", ", Enum.GetValues(optionsType).Cast<object>().Select(x => x.ToString()))})" : "";
				sb.Append($"/* {GetTypeName(showType)}{options} */");

				if (spaceAfter)
					sb.Append(" ");
			}

			if (obj == null)
			{
				sb.Append("null");
				AddType(true, false);
				return;
			}

			var type = obj.GetType();

			if ((showType == typeof(object)) && (labelTypeObjects.Contains(obj)))
			{
				labelTypes = true;
				showType = type;
			}

			if ((type.IsPrimitive) || (obj is string) || (obj is decimal))
			{
				sb.Append(JsonConvert.SerializeObject(obj));
				AddType(true, false);
				return;
			}

			if ((obj is DateTime) || (obj is DateTimeOffset) || (type.IsEnum))
			{
				sb.Append($"\"{obj}\"");
				AddType(true, false);
				return;
			}

			var dictInterface = type.GetInterfaces().Where(i => (i.IsGenericType) && (i.GetGenericTypeDefinition() == typeof(IDictionary<,>))).FirstOrDefault();
			if ((obj is IDictionary dict) && (dictInterface != null))
			{
				var valueType = dictInterface.GetGenericArguments()[1];
				AddType(false, true);
				sb.Append("{");
				var first = true;
				foreach (DictionaryEntry pair in dict)
				{
					if (first)
						first = false;
					else
						sb.Append($",");
					sb.Append($"\r\n{spacing}\t\"{pair.Key}\": ");
					rToJSON(valueType, pair.Value, sb, spacing + "\t", labelTypes, labelTypeObjects);
				}
				if (!first)
					sb.Append($"\r\n{spacing}");
				sb.Append("}");

				return;
			}

			var listInterface = type.GetInterfaces().Where(i => (i.IsGenericType) && (i.GetGenericTypeDefinition() == typeof(IEnumerable<>))).FirstOrDefault();
			if (listInterface != null)
			{
				var valueType = listInterface.GetGenericArguments()[0];
				AddType(false, true);
				sb.Append("[");
				var first = true;
				foreach (var item in obj as IEnumerable)
				{
					if (first)
						first = false;
					else
						sb.Append($",");
					sb.Append($"\r\n{spacing}\t");
					rToJSON(valueType, item, sb, spacing + "\t", labelTypes, labelTypeObjects);
				}
				if (!first)
					sb.Append($"\r\n{spacing}");
				sb.Append("]");
				return;
			}

			{
				var isDataContract = obj.GetType().GetCustomAttribute<DataContractAttribute>() != null;

				AddType(false, true);
				sb.Append("{");
				var first = true;
				foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
				{
					if ((isDataContract) && (prop.GetCustomAttribute<DataMemberAttribute>() == null))
						continue;

					if (first)
						first = false;
					else
						sb.Append($",");
					sb.Append($"\r\n{spacing}\t\"{prop.Name}\": ");
					rToJSON(prop.PropertyType, prop.GetValue(obj), sb, spacing + "\t", labelTypes, labelTypeObjects);
				}
				if (!first)
					sb.Append($"\r\n{spacing}");
				sb.Append("}");
				return;
			}
		}

		dynamic interceptor;
		void StartInterceptCalls(string interceptURL)
		{
			try
			{
				interceptor = Activator.CreateInstance(compiledAssembly.GetType("NeoEdit.WCFInterceptor.Interceptor"));
				interceptor.Start(interceptURL, (Func<string, string, string, Dictionary<string, object>, object>)DoExecuteWCF);
			}
			catch (Exception ex) { throw AggregateException(ex); }
		}

		List<string> EndInterceptCalls()
		{
			try
			{
				interceptor.End();

				var results = new List<string>();
				foreach (var call in interceptor.Calls)
				{
					var wcfOperation = new WCFOperation
					{
						ServiceURL = ServiceURL,
						Namespace = call.Namespace,
						Contract = call.Contract,
						Operation = call.Operation,
						Parameters = call.Parameters,
						Result = call.Result,
					};
					results.Add(ToJSON(wcfOperation, new HashSet<object>(wcfOperation.Parameters.Values)));
				}
				return results;
			}
			catch (Exception ex) { throw AggregateException(ex); }
		}
	}
}
