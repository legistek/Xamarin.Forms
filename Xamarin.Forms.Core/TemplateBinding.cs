using System;
using System.Globalization;

namespace Xamarin.Forms
{
	// Theoretically this class shouldn't be necessary with
	// Binding.RelativeSource but it's here to avoid breaking
	// changes.
	public sealed class TemplateBinding : Binding
	{
		public TemplateBinding()
		{		
		}

		public TemplateBinding(string path, BindingMode mode = BindingMode.Default, IValueConverter converter = null, object converterParameter = null, string stringFormat = null)
		{
			if (path == null)
				throw new ArgumentNullException("path");
			if (string.IsNullOrWhiteSpace(path))
				throw new ArgumentException("path can not be an empty string", "path");

			AllowChaining = true;
			Path = path;
			Converter = converter;
			ConverterParameter = converterParameter;
			Mode = mode;
			StringFormat = stringFormat;
		}

		public override RelativeSourceBinding RelativeSource
		{
			get => RelativeSourceBinding.TemplatedParent;
		}
	}	
}