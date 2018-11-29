using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

using Xamarin.Forms.Internals;

namespace Xamarin.Forms
{
	[ContentProperty(nameof(Bindings))]
	public sealed class MultiBinding : BindingBase
	{
		IMultiValueConverter _converter;
		object _converterParameter;
		BindingExpression _expression;
		Collection<BindingBase> _bindings;
		List<MultiBindingProxy> _childProxies;
		MultiBindingProxy _mainProxy;

		public IMultiValueConverter Converter
		{
			get { return _converter; }
			set
			{
				ThrowIfApplied();
				_converter = value;
			}
		}

		public object ConverterParameter
		{
			get { return _converterParameter; }
			set
			{
				ThrowIfApplied();
				_converterParameter = value;
			}
		}

		public Collection<BindingBase> Bindings
		{
			get => _bindings ?? (_bindings = new Collection<BindingBase>());
			set => _bindings = value;
		}

		internal override BindingBase Clone()
		{
			return new MultiBinding()
			{
				Converter = Converter,
				ConverterParameter = ConverterParameter,
				Bindings = new Collection<BindingBase>(this.Bindings),
				FallbackValue = FallbackValue,
				Mode = Mode,
				TargetNullValue = TargetNullValue,
				StringFormat = StringFormat
			};
		}

		internal override void Apply(bool fromTarget)
		{
			base.Apply(fromTarget);

			if (_expression == null)
				_expression = new BindingExpression(this, Binding.SelfPath);

			ApplyBindingProxyValues(null);
			_expression.Apply(fromTarget);			
		}

		internal override void Apply(
			object context, 
			BindableObject bindObj, 
			BindableProperty targetProperty, 
			bool fromBindingContextChanged = false)
		{			
			base.Apply(_mainProxy, bindObj, targetProperty, fromBindingContextChanged);

			if (IsApplied && fromBindingContextChanged)
				return;

			CreateBindingProxies(bindObj);

			if (_expression == null)
				_expression = new BindingExpression(this, nameof(MultiBindingProxy.Value));

			ApplyBindingProxyValues(null);
			_expression.Apply(_mainProxy, bindObj, targetProperty);
		}

		internal override object GetSourceValue(object value, Type targetPropertyType)
		{
			// value should be the object[] we assigned to 
			// _mainProxy.Value in OnBindingProxyValueChanged
			if (value != _mainProxy.Value)
				return value;

			object[] values = value as object[];
			if (values == null)
				// likewise should never happen
				return value;

			return this.Converter?.Convert(
				values,
				targetPropertyType,
				this.ConverterParameter,
				CultureInfo.CurrentUICulture);
		}

		internal override void Unapply(bool fromBindingContextChanged = false)
		{
			if (fromBindingContextChanged && IsApplied)
				return;

			base.Unapply(fromBindingContextChanged: fromBindingContextChanged);

			if (_expression != null)
				_expression.Unapply();

			if (this._childProxies?.Count > 0)
			{
				_mainProxy.RemoveBinding(MultiBindingProxy.ValueProperty);
				foreach (var proxy in this._childProxies)
				{
					proxy.RemoveBinding(MultiBindingProxy.ValueProperty);
					proxy.RemoveBinding(MultiBindingProxy.BindingContextProperty);
				}
			}

			_mainProxy = null;
			_childProxies = null;
		}

		internal void ApplyBindingProxyValues(MultiBindingProxy trigger)
		{
			if (trigger == _mainProxy)
			{
				// triggered because the target property was updated
				ApplyTargetValueUpdate();
			}
			else
			{
				// Triggered manually or because of a child proxy
				// value update. Re-set the main proxy value to the 
				// object[] that is input to the IMultiValueConverter			
				_mainProxy.SetValueSilent(_childProxies.Select(p => p.Value).ToArray());
				_expression.Apply();
			}			
		}

		void CreateBindingProxies(BindableObject target)
		{			
			_mainProxy = new MultiBindingProxy(this);
			_childProxies = new List<MultiBindingProxy>();

			if (this.Bindings.Count == 0)
				return;

			foreach (var binding in _bindings)
			{
				MultiBindingProxy proxy = new MultiBindingProxy(this);

				// Bind proxy's BindingContext to that of the 
				// target.
				proxy.SetBinding(
					BindableObject.BindingContextProperty,
					new Binding(nameof(target.BindingContext), mode: this.Mode, source: target));

				// Bind proxy's Value property using the child binding
				var proxyBinding = binding.Clone();
				if (proxyBinding.Mode == BindingMode.Default)
					proxyBinding.Mode = this.Mode;
				proxy.SetBinding(MultiBindingProxy.ValueProperty, proxyBinding);
				_childProxies.Add(proxy);
			}
		}

		void ApplyTargetValueUpdate()
		{
			var types = _childProxies
				.Select(p => p.Value?.GetType() ?? typeof(object))
				.ToArray();
			var convertedValues = this.Converter?.ConvertBack(
				_mainProxy.Value,
				types,
				this.ConverterParameter,
				CultureInfo.CurrentUICulture);

			if (convertedValues == null)
				// Conversion error or no converter; cannot update sources
				return;

			int count = Math.Min(convertedValues.Length, this._childProxies.Count);
			for (int i = 0; i < count; i++)
				this._childProxies[i].SetValueSilent(convertedValues[i]);
		}
	}
}
