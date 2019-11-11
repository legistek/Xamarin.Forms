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
			set
			{
				SetValue(ValueProperty, value);
			}
		}
		
		internal MultiBinding MultiBinding { get; }

		internal void SetValueSilent(BindableProperty property, object value)
		{
			bool suspended = this._suspendValueChangeNotification;
			this._suspendValueChangeNotification = true;
			try
			{
				SetValue(property, value);
			}
			finally
			{
				_suspendValueChangeNotification = suspended;
			}
		}

		void OnValueChanged(object oldValue, object newValue)
		{
			if (!_suspendValueChangeNotification)
				this.MultiBinding.ApplyBindingProxyValues(this);
		}
	}
}
