namespace Xamarin.Forms.Internals
{
	internal class MultiBindingProxy : BindableObject
	{		
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

		internal bool SuspendValueChangeNotification { get; set; }
		
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

		internal void SetValueSilent(object value)
		{
			bool suspended = this.SuspendValueChangeNotification;
			this.SuspendValueChangeNotification = true;
			try
			{
				SetValue(ValueProperty, value);
			}
			finally
			{
				SuspendValueChangeNotification = suspended;
			}
		}

		void OnValueChanged(object oldValue, object newValue)
		{
			if (!SuspendValueChangeNotification)
				this.MultiBinding.ApplyBindingProxyValues(this);
		}
	}
}
