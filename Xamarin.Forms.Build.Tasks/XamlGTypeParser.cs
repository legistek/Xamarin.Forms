using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Xamarin.Forms.Xaml;

namespace Xamarin.Forms.Build.Tasks
{
	class XamlGTypeParser : IDisposable
	{
		List<XmlnsDefinitionAttribute> _xmlnsDefinitions;
		Dictionary<string, ModuleDefinition> _xmlnsModules;
		string[] _references;

		public XamlGTypeParser(string references)
		{
			_references = references?.Split(';')?.Distinct()?.ToArray() ?? new string[0];
		}

		public CodeTypeReference GetType(XmlType xmlType)
		{
			CodeTypeReference returnType = null;
			var ns = GetClrNamespace(xmlType.NamespaceUri);
			if (ns == null)
			{
				// It's a custom namespace URL.
				returnType = GetCustomNamespaceUrlType(xmlType);
			}
			else
			{
				var type = xmlType.Name;
				type = $"{ns}.{type}";

				if (xmlType.TypeArguments != null)
					type = $"{type}`{xmlType.TypeArguments.Count}";

				returnType = new CodeTypeReference(type);
			}

			returnType.Options |= CodeTypeReferenceOptions.GlobalReference;

			if (xmlType.TypeArguments != null)
				foreach (var typeArg in xmlType.TypeArguments)
					returnType.TypeArguments.Add(GetType(typeArg));

			return returnType;
		}

		static string GetClrNamespace(string namespaceuri)
		{
			if (namespaceuri == XamlParser.X2009Uri)
				return "System";
			if (namespaceuri != XamlParser.X2006Uri &&
				!namespaceuri.StartsWith("clr-namespace", StringComparison.InvariantCulture) &&
				!namespaceuri.StartsWith("using:", StringComparison.InvariantCulture))
				return null;
			return XmlnsHelper.ParseNamespaceFromXmlns(namespaceuri);
		}

		void GatherXmlnsDefinitionAttributes()
		{
			_xmlnsDefinitions = new List<XmlnsDefinitionAttribute>();
			_xmlnsModules = new Dictionary<string, ModuleDefinition>();

			foreach (var path in _references)
			{
				string asmName = Path.GetFileName(path);
				if (AssemblyIsSystem(asmName))
					// Skip the myriad "System." assemblies and others
					continue;

				using (var asmDef = AssemblyDefinition.ReadAssembly(path))
				{
					foreach (var ca in asmDef.CustomAttributes)
					{
						if (ca.AttributeType.FullName == typeof(XmlnsDefinitionAttribute).FullName)
						{
							_xmlnsDefinitions.Add(XamlCTypeParser.GetXmlnsDefinition(ca, asmDef));
							_xmlnsModules[asmDef.FullName] = asmDef.MainModule;
						}
					}
				}
			}
		}

		bool AssemblyIsSystem(string name)
		{
			if (name.StartsWith("System.", StringComparison.CurrentCultureIgnoreCase))
				return true;
			else if (name.Equals("mscorlib.dll", StringComparison.CurrentCultureIgnoreCase))
				return true;
			else if (name.Equals("netstandard.dll", StringComparison.CurrentCultureIgnoreCase))
				return true;
			else
				return false;
		}

		CodeTypeReference GetCustomNamespaceUrlType(XmlType xmlType)
		{
			if (_xmlnsDefinitions == null)
				GatherXmlnsDefinitionAttributes();

			IList<XamlLoader.FallbackTypeInfo> potentialTypes;
			TypeReference typeReference = xmlType.GetTypeReference<TypeReference>(
				_xmlnsDefinitions,
				null,
				(typeInfo) =>
				{
					ModuleDefinition module = null;
					if (!_xmlnsModules.TryGetValue(typeInfo.AssemblyName, out module))
						return null;
					string typeName = typeInfo.TypeName.Replace('+', '/'); //Nested types
					string fullName = $"{typeInfo.ClrNamespace}.{typeInfo.TypeName}";
					return module.Types.Where(t => t.FullName == fullName).FirstOrDefault();
				},
				out potentialTypes);

			if (typeReference == null)
				throw new Exception($"Type {xmlType.Name} not found in xmlns {xmlType.NamespaceUri}");

			return new CodeTypeReference(typeReference.FullName);
		}

		public void Dispose()
		{
			if (_xmlnsModules != null)
			{
				foreach (var moduleDef in _xmlnsModules.Values)
				{
					moduleDef.Dispose();
				}
			}
		}
	}
}
