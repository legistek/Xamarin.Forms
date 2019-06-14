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
		BindableProperty _targetProperty;
		bool _isApplying;

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

			if (fromTarget && _isApplying)
				return;

			_expression.Apply(fromTarget);
			ApplyBindingProxyValues(fromTarget ? _mainProxy : null);			
		}

		internal override void Apply(
			object context, 
			BindableObject bindObj, 
			BindableProperty targetProperty, 
			bool fromBindingContextChanged = false)
		{
			base.Apply(context, bindObj, targetProperty, fromBindingContextChanged);

			if (IsApplied && fromBindingContextChanged)
			{
				bool childContextChanged = false;
				foreach (var proxy in _childProxies)
				{
					if (!object.ReferenceEquals(proxy.BindingContext, context))
					{
						childContextChanged = true;
						proxy.SuspendValueChangeNotification = true;
						proxy.BindingContext = context;
						proxy.SuspendValueChangeNotification = false;
					}
				}
				if ( childContextChanged )
					ApplyBindingProxyValues(null, true);
				return;
			}

			

			_targetProperty = targetProperty;

			CreateBindingProxies(bindObj, context);

			if (_expression == null)
				_expression = new BindingExpression(this, nameof(MultiBindingProxy.Value));
			
			_isApplying = true;
			try
			{
				ApplyBindingProxyValues(_mainProxy, reapplyExpression: false);
				_expression.Apply(_mainProxy, bindObj, targetProperty);
			}
			finally
			{
				_isApplying = false;
			}
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

		internal bool ApplyBindingProxyValues(MultiBindingProxy trigger, bool reapplyExpression = true)
		{			
			BindingMode mode = this.GetRealizedMode(_targetProperty);
			bool convertBackFailed = false;
			if (trigger == _mainProxy && 
				(mode == BindingMode.TwoWay || mode == BindingMode.OneWayToSource))
			{
				// triggered because the target property was updated
				convertBackFailed = !ApplyTargetValueUpdate();				
			}

			if (mode != BindingMode.OneWayToSource && !convertBackFailed)
			{			
				// Re-evaluate the entire MultiBinding to get the new target value.
				// Even if this was triggered by a target update, it's
				// possible that re-evaluation could result in a different
				// value for the target than what was set explicitly.
				object newTargetValue = this.FallbackValue ?? _targetProperty.DefaultValue;
				if (this.Converter != null)
				{
					object convertedValue = this.Converter.Convert(
							_childProxies.Select(p => p.Value).ToArray(),
							_targetProperty.ReturnType,
							this.ConverterParameter,
							CultureInfo.CurrentUICulture);
					if (convertedValue != Binding.UnsetValue)
						newTargetValue = convertedValue;
				}

				_mainProxy.SetValueSilent(newTargetValue);
			}

			if (reapplyExpression)
			{
				bool wasApplying = _isApplying;
				_isApplying = true;
				_expression.Apply();
				_isApplying = wasApplying;
			}
			return true;
		}

		void CreateBindingProxies(BindableObject target, object context)
		{			
			_mainProxy = new MultiBindingProxy(this);
			_childProxies = new List<MultiBindingProxy>();

			if (this.Bindings.Count == 0)
				return;

			var mode = this.GetRealizedMode(_targetProperty);

			foreach (var binding in _bindings)
			{
				MultiBindingProxy proxy = new MultiBindingProxy(this);
				proxy.BindingContext = context;

				// Bind each proxy's BindingContext to that of the 
				// target.
				//proxy.SetBinding(
				//	BindableObject.BindingContextProperty,
				//	new Binding(nameof(target.BindingContext), mode: BindingMode.OneWay, source: target));

				// Bind proxy's Value property using the child binding settings
				var proxyBinding = binding.Clone();

				// OneWayToSource, OneTime, or OneWay mode on the MultiBinding effectively
				// override the childrens' modes 
				if (mode == BindingMode.OneWayToSource ||
					 mode == BindingMode.OneTime ||
					 mode == BindingMode.OneWay ||
					 proxyBinding.Mode == BindingMode.Default)
					proxyBinding.Mode = mode;

				proxy.SetBinding(MultiBindingProxy.ValueProperty, proxyBinding);
				_childProxies.Add(proxy);
			}
		}

		bool ApplyTargetValueUpdate()
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
			{
				// https://docs.microsoft.com/en-us/dotnet/api/system.windows.data.imultivalueconverter.convertback?view=netframework-4.8
				// Return null to indicate that the converter cannot perform the 
				// conversion or that it does not support conversion in this direction.		
				if (this.GetRealizedMode(_targetProperty) == BindingMode.OneWayToSource)
					// Ensures that a failed ConvertBack doesn't 
					// affect the source values
					this._childProxies.ForEach(p => p.SetValueSilent(Binding.DoNothing));
				return false;
			}

			int count = Math.Min(convertedValues.Length, this._childProxies.Count);
			for (int i = 0; i < count; i++)
			{
				// https://docs.microsoft.com/en-us/dotnet/api/system.windows.data.imultivalueconverter.convertback?view=netframework-4.8
				// Return DoNothing at position i to indicate that no value is to 
				// be set on the source binding at index i.
				// Return DependencyProperty.UnsetValue at position i to indicate that 
				// the converter is unable to provide a value for the source binding at 
				// index i, and that no value is to be set on it.
				if (convertedValues[i] == Binding.DoNothing ||
					convertedValues[i] == Binding.UnsetValue)
					continue;

				var childMode = this.Bindings[i].GetRealizedMode(_targetProperty);
				if (childMode != BindingMode.TwoWay && childMode != BindingMode.OneWayToSource)
					continue;
				this._childProxies[i].SetValueSilent(convertedValues[i]);
			}
			return true;
		}
	}
}
