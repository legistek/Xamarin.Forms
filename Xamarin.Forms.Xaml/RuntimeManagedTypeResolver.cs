using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using Xamarin.Forms.Internals;

namespace Xamarin.Forms.Xaml
{
	// Used for runtime XmlType resolution to CLR Type
	class RuntimeManagedTypeResolver : IXamlTypeInfo
	{
		Dictionary<XmlType, Type> _typeCache = new Dictionary<XmlType, Type>();
		Assembly _currentAssembly;
		static IList<XmlnsDefinitionAttribute> s_xmlnsDefinitions;

		public RuntimeManagedTypeResolver(Assembly currentAssembly = null)
		{
			// this is ideally the assembly with the XAML being parsed
			_currentAssembly = currentAssembly;
		}

		public bool HasAttribute(XmlType xmlType, Type attrType)
		{
			Type managedType = GetManagedType(xmlType, null, out _);
			return managedType?.GetTypeInfo()?.GetCustomAttribute(attrType) != null;
		}

		public bool IsType (XmlType xmlType, Type t)
		{
			Type managedType = GetManagedType(xmlType, null, out _);
			if ( managedType != null )
				return t.IsAssignableFrom(managedType);
			return false;
		}

		public Type GetManagedType(XmlType xmlType, IXmlLineInfo lineInfo, out XamlParseException exception)
		{
			exception = null;
			if (!_typeCache.TryGetValue(xmlType, out Type type))
			{
				type = GetElementType(xmlType, lineInfo, _currentAssembly, out exception);
				if (type != null)
					_typeCache[xmlType] = type;
			}
			return type;
		}

		static void GatherXmlnsDefinitionAttributes()
		{
			Assembly[] assemblies = null;
#if !NETSTANDARD2_0
			assemblies = new[] {
				typeof(XamlLoader).GetTypeInfo().Assembly,
				typeof(View).GetTypeInfo().Assembly,
			};
#else
			assemblies = AppDomain.CurrentDomain.GetAssemblies();
#endif

			s_xmlnsDefinitions = new List<XmlnsDefinitionAttribute>();

			foreach (var assembly in assemblies)
				foreach (XmlnsDefinitionAttribute attribute in assembly.GetCustomAttributes(typeof(XmlnsDefinitionAttribute)))
				{
					s_xmlnsDefinitions.Add(attribute);
					attribute.AssemblyName = attribute.AssemblyName ?? assembly.FullName;
				}
		}

		static Type GetElementType(XmlType xmlType, IXmlLineInfo xmlInfo, Assembly currentAssembly,
			out XamlParseException exception)
		{
#if NETSTANDARD2_0
			bool hasRetriedNsSearch = false;
#endif
			IList<XamlLoader.FallbackTypeInfo> potentialTypes;

#if NETSTANDARD2_0
		retry:
#endif
			if (s_xmlnsDefinitions == null)
				GatherXmlnsDefinitionAttributes();

			Type type = xmlType.GetTypeReference(
				s_xmlnsDefinitions,
				currentAssembly?.FullName,
				(typeInfo) =>
					Type.GetType($"{typeInfo.ClrNamespace}.{typeInfo.TypeName}, {typeInfo.AssemblyName}"),
				out potentialTypes);

			var typeArguments = xmlType.TypeArguments;
			exception = null;

			if (type != null && typeArguments != null)
			{
				XamlParseException innerexception = null;
				var args = typeArguments.Select(delegate (XmlType xmltype) {
					var t = GetElementType(xmltype, xmlInfo, currentAssembly, out XamlParseException xpe);
					if (xpe != null)
					{
						innerexception = xpe;
						return null;
					}
					return t;
				}).ToArray();
				if (innerexception != null)
				{
					exception = innerexception;
					return null;
				}
				type = type.MakeGenericType(args);
			}

#if NETSTANDARD2_0
			if (type == null)
			{
				// This covers the scenario where the AppDomain's loaded
				// assemblies might have changed since this method was first
				// called. This occurred during unit test runs and could
				// conceivably occur in the field. 
				if (!hasRetriedNsSearch) {
					hasRetriedNsSearch = true;
					s_xmlnsDefinitions = null;
					goto retry;
				}
			}
#endif

			if (XamlLoader.FallbackTypeResolver != null)
				type = XamlLoader.FallbackTypeResolver(potentialTypes, type);

			if (type == null)
				exception = new XamlParseException($"Type {xmlType.Name} not found in xmlns {xmlType.NamespaceUri}", xmlInfo);

			return type;
		}
	}
}
