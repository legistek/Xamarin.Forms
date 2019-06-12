using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;
using Xamarin.Forms.Xaml;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Gh6452 : ContentPage
	{
		public Gh6452()
		{
			InitializeComponent();			
		}

		public Gh6452(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		[TestFixture]
		class Tests
		{
			[SetUp]
			public void SetUp()
			{
				Device.PlatformServices = new MockPlatformServices();				
			}

			[TestCase(false)]
			public void SpecialTypesRecognizedWithNsPrefixCompiled(bool useCompiledXaml)
			{
				MockCompiler.Compile(typeof(Gh6452));
			}

			[TestCase(false)]
			[TestCase(true)]
			public void SpecialTypesRecognizedWithNsPrefix(bool useCompiledXaml)
			{
				Gh6452 issue = new Gh6452(useCompiledXaml);  
				Assert.IsTrue(issue.TryGetResource("TestTemplate", out object templObj));

				var dt = templObj as Forms.DataTemplate;
				Assert.IsNotNull(dt);   

				var templLabel = dt.CreateContent() as Label;
				Assert.IsNotNull(templLabel);   

				templLabel.BindingContext = "Peter";
				Assert.AreEqual(templLabel.Text, "Peter");

				var redLabel = issue.FindByName("redLabel") as Label;
				Assert.IsNotNull(redLabel);
				Assert.AreEqual(redLabel.TextColor, Color.FromHex("FFFF0000"));
				Assert.AreEqual(redLabel.BackgroundColor, Color.FromHex("FF00FF00"));

				var control = issue.FindByName<ContentView>("control");
				Assert.IsNotNull(control);
				var controlLabel = control.AllChildren.FirstOrDefault() as Label;
				Assert.IsNotNull(controlLabel);
				Assert.AreEqual(controlLabel.Text, "Control Label");
			}
		}
	}

	public class Gh6452Model
	{
		public string[] People { get; } = 
		{ 
			"Larry",
			"Curly",
			"Moe" 
		};  
	}

	public class HierarchicalDataTempate : DataTemplate
	{
	}
}