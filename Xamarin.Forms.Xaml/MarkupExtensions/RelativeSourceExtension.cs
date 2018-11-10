using System;

namespace Xamarin.Forms.Xaml
{
	[ContentProperty("Mode")]
	[AcceptEmptyServiceProvider]
	public sealed class RelativeSourceExtension : IMarkupExtension<RelativeSourceBinding>
	{
		Type _ancestorType;
		int _ancestorLevel = 1;

		public RelativeSourceBindingMode Mode
		{
			get;
			set;
		}

		public int AncestorLevel
		{
			get => _ancestorLevel;
			set
			{
				_ancestorLevel = value;
				if (_ancestorLevel > 0)
					this.Mode = RelativeSourceBindingMode.FindAncestor;
			}
		}

		public Type AncestorType
		{
			get => _ancestorType;
			set
			{
				_ancestorType = value;
				if (_ancestorType != null)
					this.Mode = RelativeSourceBindingMode.FindAncestor;
			}
		}

		RelativeSourceBinding IMarkupExtension<RelativeSourceBinding>.ProvideValue(IServiceProvider serviceProvider)
		{
			switch (this.Mode)
			{
				case RelativeSourceBindingMode.Self:
					return RelativeSourceBinding.Self;
				case RelativeSourceBindingMode.TemplatedParent:
					return RelativeSourceBinding.TemplatedParent;
				case RelativeSourceBindingMode.FindAncestor:
					if (AncestorType == null)
						throw new Exception(
							$"{nameof(RelativeSourceBindingMode.FindAncestor)} {nameof(Binding.RelativeSource)} " +
							$"binding must specify valid {nameof(AncestorType)}");
					return new RelativeSourceBinding(RelativeSourceBindingMode.FindAncestor)
					{
						AncestorType = AncestorType,
						AncestorLevel = AncestorLevel
					};
				default:
					throw new NotImplementedException();
			}
		}

		public object ProvideValue(IServiceProvider serviceProvider)
		{
			return (this as IMarkupExtension<RelativeSourceBinding>).ProvideValue(serviceProvider);
		}
	}
}