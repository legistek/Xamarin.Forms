﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Xamarin.Forms.Xaml;

namespace Xamarin.Forms.Build.Tasks
{
	// Used for XamlC type parsing to TypeReference
	class XamlCTypeParser : IXamlTypeParser
	{
		static Dictionary<ModuleDefinition, IList<XmlnsDefinitionAttribute>> s_xmlnsDefinitions =
			new Dictionary<ModuleDefinition, IList<XmlnsDefinitionAttribute>>();
		static object _nsLock = new object();
		Dictionary<XmlType, TypeReference> _typeCache = new Dictionary<XmlType, TypeReference>();
		Dictionary<Type, TypeReference> _specialTypes = new Dictionary<Type, TypeReference>();
		ModuleDefinition _currentModule;

		public XamlCTypeParser(ModuleDefinition module)
		{
			_currentModule = module;
		}

		public bool HasAttribute(XmlType xmlType, Type attrType)
		{
			TypeReference managedType = GetManagedType<TypeReference>(xmlType, null, out _);
			return managedType?.GetCustomAttribute(
				_currentModule,
				(attrType.Assembly.GetName().Name, attrType.Namespace, attrType.Name)) != null;
		}

		public bool DerivesFrom(XmlType xmlType, Type t)
		{
			TypeReference managedType = GetManagedType<TypeReference>(xmlType, null, out _);
			if (managedType != null)
			{
				if ( !_specialTypes.TryGetValue(t, out TypeReference specialType))
				{
					specialType = _currentModule.GetTypeDefinition((t.Assembly.GetName().Name, t.Namespace, t.Name));
					_specialTypes[t] = specialType;
				}
				return managedType.InheritsFromOrImplements(specialType);
			}
			return false;
		}

		public T GetManagedType<T>(XmlType xmlType, IXmlLineInfo lineInfo, out XamlParseException exception) where T : class
		{
			exception = null;
			if (!_typeCache.TryGetValue(xmlType, out TypeReference type))
			{
				try
				{
					type = GetTypeReference(xmlType, lineInfo);
				}
				catch (XamlParseException xpe)
				{
					exception = xpe;
				}
				_typeCache[xmlType] = type;
			}
			return type as T;
		}

		private TypeReference GetTypeReference(XmlType xmlType, IXmlLineInfo xmlInfo)
		{
			IList<XmlnsDefinitionAttribute> xmlnsDefinitions = null;
			lock (_nsLock)
			{
				if (!s_xmlnsDefinitions.TryGetValue(_currentModule, out xmlnsDefinitions))
					xmlnsDefinitions = GatherXmlnsDefinitionAttributes(_currentModule);
			}

			var typeArguments = xmlType.TypeArguments;

			IList<XamlLoader.FallbackTypeInfo> potentialTypes;
			TypeReference type = xmlType.GetTypeReference(
				xmlnsDefinitions,
				_currentModule.Assembly.Name.Name,
				(typeInfo) =>
				{
					string typeName = typeInfo.TypeName.Replace('+', '/'); //Nested types
					return _currentModule.GetTypeDefinition((typeInfo.AssemblyName, typeInfo.ClrNamespace, typeName));
				},
				out potentialTypes);

			if (type != null && typeArguments != null && type.HasGenericParameters)
			{
				type =
					_currentModule.ImportReference(type)
						.MakeGenericInstanceType(typeArguments.Select(x => GetTypeReference(x, xmlInfo)).ToArray());
			}

			if (type == null)
				throw new XamlParseException($"Type {xmlType.Name} not found in xmlns {xmlType.NamespaceUri}", xmlInfo);

			return _currentModule.ImportReference(type);
		}

		static IList<XmlnsDefinitionAttribute> GatherXmlnsDefinitionAttributes(ModuleDefinition module)
		{
			var xmlnsDefinitions = new List<XmlnsDefinitionAttribute>();

			if (module.AssemblyReferences?.Count > 0)
			{
				// Search for the attribute in the assemblies being
				// referenced.
				foreach (var asmRef in module.AssemblyReferences)
				{
					var asmDef = module.AssemblyResolver.Resolve(asmRef);
					foreach (var ca in asmDef.CustomAttributes)
					{
						if (ca.AttributeType.FullName == typeof(XmlnsDefinitionAttribute).FullName)
						{
							var attr = GetXmlnsDefinition(ca, asmDef);
							xmlnsDefinitions.Add(attr);
						}
					}
				}
			}
			else
			{
				// Use standard XF assemblies
				// (Should only happen in unit tests)
				var requiredAssemblies = new[] {
					typeof(XamlLoader).Assembly,
					typeof(View).Assembly,
				};
				foreach (var assembly in requiredAssemblies)
					foreach (XmlnsDefinitionAttribute attribute in assembly.GetCustomAttributes(typeof(XmlnsDefinitionAttribute), false))
					{
						attribute.AssemblyName = attribute.AssemblyName ?? assembly.FullName;
						xmlnsDefinitions.Add(attribute);
					}
			}

			s_xmlnsDefinitions[module] = xmlnsDefinitions;
			return xmlnsDefinitions;
		}

		internal static XmlnsDefinitionAttribute GetXmlnsDefinition(CustomAttribute ca, AssemblyDefinition asmDef)
		{
			var attr = new XmlnsDefinitionAttribute(
							ca.ConstructorArguments[0].Value as string,
							ca.ConstructorArguments[1].Value as string);

			string assemblyName = null;
			if (ca.Properties.Count > 0)
				assemblyName = ca.Properties[0].Argument.Value as string;
			attr.AssemblyName = assemblyName ?? asmDef.Name.FullName;
			return attr;
		}
	}
}
