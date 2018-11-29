namespace Xamarin.Forms.Internals
{
	internal class MultiBindingProxy : BindableObject
	{
		bool _suspendValueChangeNotification;

		public static readonly BindableProperty ValueProperty = BindableProperty.Create(
			"Value",
			typeof(object),
			typeof(MultiBindingProxy),
			null,
			propertyChanged:
				new BindableProperty.BindingPropertyChangedDelegate(
					(obj, oldVal, newVal)=>
						(obj as MultiBindingProxy).OnValueChanged(oldVal, newVal)));

		internal MultiBindingProxy(MultiBinding multiBinding)
		{
			this.MultiBinding = multiBinding;
		}

		public object Value
		{
			get
			{
				return GetValue(ValueProperty);
			}
		}
		
		internal MultiBinding MultiBinding { get; }

		internal void SetValueSilent(object value)
		{
			this._suspendValueChangeNotification = true;
			try
			{
				SetValue(ValueProperty, value);
			}
			finally
			{
				_suspendValueChangeNotification = false;
			}
		}

		void OnValueChanged(object oldValue, object newValue)
		{
			if (!_suspendValueChangeNotification)
				this.MultiBinding.ApplyBindingProxyValues(this);
		}
	}
}
