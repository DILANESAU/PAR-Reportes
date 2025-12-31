using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;

namespace WPF_PAR.Core.Converters
{
    // 1. Convierte TRUE (Es Futuro) a Gris, FALSE (Ya pasó) a Azul
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ( value is bool esFuturo && esFuturo )
            {
                return Brushes.LightGray; // Color para periodos futuros
            }
            // Color para periodos actuales/pasados (Azul Material Design)
            return new SolidColorBrush(( Color ) ColorConverter.ConvertFromString("#2196F3"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // 2. Convierte TRUE (Es Futuro) a Opaco (0.3), FALSE a Visible (1.0)
    public class BoolToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ( value is bool esFuturo && esFuturo )
            {
                return 0.3; // Muy transparente si es futuro
            }
            return 1.0; // Totalmente visible si ya pasó
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
