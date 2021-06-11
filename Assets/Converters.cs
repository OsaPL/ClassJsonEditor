using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace ClassJsonEditor.Assets.Converters
{
    public class BoolToFontWeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (targetType == typeof(Avalonia.Media.FontWeight) && value is bool toConvert)
                return toConvert ? Avalonia.Media.FontWeight.Normal : Avalonia.Media.FontWeight.Bold;
            else
                throw new NotSupportedException();
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return BindingOperations.DoNothing;
        }
    }
}