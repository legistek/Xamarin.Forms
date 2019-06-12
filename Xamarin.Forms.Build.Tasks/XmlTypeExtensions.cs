using System.Xml;
using Mono.Cecil;
using Xamarin.Forms.Xaml;

namespace Xamarin.Forms.Build.Tasks
{
	static class XmlTypeExtensions
	{	
		public static TypeReference GetTypeReference(string xmlType, BaseNode node, ILContext context)
		{
			var split = xmlType.Split(':');
			if (split.Length > 2)
				throw new XamlParseException($"Type \"{xmlType}\" is invalid", node as IXmlLineInfo);

			string prefix, name;
			if (split.Length == 2) {
				prefix = split[0];
				name = split[1];
			} else {
				prefix = "";
				name = split[0];
			}
			var namespaceuri = node.NamespaceResolver.LookupNamespace(prefix) ?? "";
			return context.TypeParser.GetManagedType<TypeReference>(
				new XmlType(namespaceuri, name, null), 
				node as IXmlLineInfo,
				out _);
		}
	}
}
