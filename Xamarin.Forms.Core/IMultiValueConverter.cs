using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Xamarin.Forms
{
	public interface IMultiValueConverter
	{
		object Convert(object[] values, Type targetType, object parameter, CultureInfo culture);
		object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture);
	}
}
