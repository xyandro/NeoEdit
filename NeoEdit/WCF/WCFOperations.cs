using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CSharp;
using Newtonsoft.Json;

namespace NeoEdit.Program.WCF
{
	public class WCFOperations : MarshalByRefObject
	{
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.Infrastructure)]
		public override object InitializeLifetimeService() => null;

		static AppDomain appDomain;
		static WCFOperations _wcfOperations;
		static WCFOperations wcfOperations
		{
			get
			{
				if (_wcfOperations == null)
				{
					appDomain = AppDomain.CreateDomain(nameof(WCFOperations));
					try
					{
						_wcfOperations = appDomain.CreateInstanceAndUnwrap(typeof(WCFOperations).Assembly.FullName, typeof(WCFOperations).FullName) as WCFOperations;
					}
					catch
					{
						AppDomain.Unload(appDomain);
						appDomain = null;
					}
				}

				return _wcfOperations;
			}
		}

		Dictionary<string, WCFClient> wcfClients = new Dictionary<string, WCFClient>();

		static public void ResetClients()
		{
			if (appDomain == null)
				return;

			AppDomain.Unload(appDomain);
			_wcfOperations = null;
			appDomain = null;
		}

		static public string GetWCFConfig(string serviceURL) => wcfOperations.DoGetWCFConfig(serviceURL);

		static public string ExecuteWCF(string str) => wcfOperations.DoExecuteWCF(str);

		public class WCFConfig
		{
			public List<WCFOperation> Operations { get; } = new List<WCFOperation>();
			public string Config { get; set; }
		}

		public class WCFOperation
		{
			public string Operation { get; set; }
			public string ServiceURL { get; set; }
			public string Contract { get; set; }
			public Dictionary<string, object> Parameters { get; } = new Dictionary<string, object>();
			public object Result { get; set; }
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

		WCFClient GetWCFClient(string serviceURL)
		{
			if (!wcfClients.ContainsKey(serviceURL))
				wcfClients[serviceURL] = new WCFClient(serviceURL);
			return wcfClients[serviceURL];
		}

		void DoResetClients() => wcfClients.Clear();

		string DoGetWCFConfig(string serviceURL)
		{
			try
			{
				var wcfClient = GetWCFClient(serviceURL);

				var wcfConfig = new WCFConfig();
				foreach (var contract in wcfClient.Contracts)
					using (var instance = wcfClient.CreateInstance(contract.Name, contract.Namespace) as IDisposable)
						foreach (var operation in contract.Operations)
						{
							var wcfOperation = new WCFOperation { Operation = operation.Name, ServiceURL = serviceURL, Contract = contract.Name };
							foreach (var parameter in instance.GetType().GetMethod(operation.Name).GetParameters())
								wcfOperation.Parameters[parameter.Name] = CreateWCFDefaultObject(parameter.ParameterType);
							wcfConfig.Operations.Add(wcfOperation);
						}
				wcfConfig.Config = wcfClient.Config;

				return ToJSON(wcfConfig, new HashSet<object>(wcfConfig.Operations.SelectMany(c => c.Parameters.Values)));
			}
			catch (Exception ex) { throw new Exception(ex.Message); }
		}

		string DoExecuteWCF(string str)
		{
			try
			{
				str = Regex.Replace(str, @"/\*.*?\*/", "", RegexOptions.Singleline | RegexOptions.Multiline);
				var wcfOperation = JsonConvert.DeserializeObject<WCFOperation>(str);

				var wcfClient = GetWCFClient(wcfOperation.ServiceURL);

				var contract = wcfClient.Contracts.FirstOrDefault(x => x.Name == wcfOperation.Contract);
				if (contract == null)
					throw new Exception($"Contract not found: {wcfOperation.Contract}");

				var operation = contract.Operations.FirstOrDefault(x => x.Name == wcfOperation.Operation);
				if (operation == null)
					throw new Exception($"Operation not found: {wcfOperation.Operation}");

				using (var instance = wcfClient.CreateInstance(contract.Name, contract.Namespace) as IDisposable)
				{
					var method = instance.GetType().GetMethod(operation.Name);
					var parameters = new List<object>();
					foreach (var parameter in method.GetParameters())
					{
						if (!wcfOperation.Parameters.ContainsKey(parameter.Name))
							throw new Exception($"Missing parameter: {parameter.Name}");
						var param = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(wcfOperation.Parameters[parameter.Name]), parameter.ParameterType);
						wcfOperation.Parameters[parameter.Name] = param;
						parameters.Add(param);
					}

					wcfOperation.Result = method.Invoke(instance, parameters.ToArray());
				}

				return ToJSON(wcfOperation, new HashSet<object>(wcfOperation.Parameters.Values));
			}
			catch (Exception ex) { throw new Exception(ex.Message); }
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

			if (labelTypeObjects.Contains(obj))
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
	}
}
