using System;
using System.Collections.Generic;

namespace Xamarin.Forms.Xaml
{
	class HydrationContext
	{
		public HydrationContext(RuntimeManagedTypeResolver managedTypeResolver)
		{
			Values = new Dictionary<INode, object>();
			Types = new Dictionary<IElementNode, Type>();
			ManagedTypeResolver = managedTypeResolver;
		}

		public Dictionary<INode, object> Values { get; }
		public Dictionary<IElementNode, Type> Types { get; }
		public HydrationContext ParentContext { get; set; }
		public Action<Exception> ExceptionHandler { get; set; }
		public object RootElement { get; set; }
		public RuntimeManagedTypeResolver ManagedTypeResolver { get; }
	}
}