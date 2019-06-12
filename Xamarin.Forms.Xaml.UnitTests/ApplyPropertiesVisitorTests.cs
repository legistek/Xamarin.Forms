using NUnit.Framework;
using System.Collections;
using Xamarin.Forms.Xaml;

namespace Xamarin.Forms.Xaml.UnitTests
{
	[TestFixture]
	public static class ApplyPropertiesVisitorTests
	{
		public class MarkupExtension : IMarkupExtension
		{
			public object ProvideValue(System.IServiceProvider serviceProvider)
			{
				return "provided value";
			}
		}

		public class ArrayListOwner
		{
			public ArrayList ArrayList { get; } = new ArrayList();
		}

		[Test]
		public static void ProvideValueForCollectionItem()
		{
			const string NAMESPACE = "clr-namespace:Xamarin.Forms.Xaml.UnitTests;assembly=Xamarin.Forms.Xaml.UnitTests";
			var resolver = new MockNameSpaceResolver();
			var parser = new RuntimeXamlTypeParser();
			var type = new XmlType(NAMESPACE, "ApplyPropertiesVisitorTests+MarkupExtension", null);
			var listNode = new ListNode(new[]
			{
				new ElementNode(type, NAMESPACE, resolver, parser),
				new ElementNode(type, NAMESPACE, resolver, parser)
			}, resolver);
			var rootElement = new ArrayListOwner();
			var rootType = new XmlType(NAMESPACE, "ApplyPropertiesVisitorTests+ArrayListOwner", null);
			var rootNode = new XamlLoader.RuntimeRootNode(rootType, rootElement, null, parser);
			var context = new HydrationContext(parser) { RootElement = rootElement };

			rootNode.Properties.Add(new XmlName(null, "ArrayList"), listNode);
			rootNode.Accept(new XamlNodeVisitor((node, parent) => node.Parent = parent), null);
			rootNode.Accept(new CreateValuesVisitor(context), null);
			rootNode.Accept(new ApplyPropertiesVisitor(context), null);

			CollectionAssert.AreEqual(new[] { "provided value", "provided value" }, rootElement.ArrayList);
		}
	}
}
