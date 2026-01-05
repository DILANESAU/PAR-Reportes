using MaterialDesignThemes.Wpf; // Necesario para PackIconKind (Dashboard)

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace WPF_PAR.Core.Converters
{
    // ==========================================
    // 1. CONVERTIDORES PARA DASHBOARD (NUEVOS)
    // ==========================================

    // Invierte visibilidad: True/Decimal>0 -> Collapsed (Ocultar)
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ( value is bool booleanValue )
            {
                return booleanValue ? Visibility.Collapsed : Visibility.Visible;
            }
            if ( value is decimal decimalValue )
            {
                return decimalValue > 0 ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Flecha Arriba/Abajo según crecimiento
    public class BoolToArrowIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ( value is bool esPositivo && esPositivo )
            {
                return PackIconKind.ArrowUpBold;
            }
            return PackIconKind.ArrowDownBold;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // ==========================================
    // 2. CONVERTIDORES PARA FAMILIAS/CLIENTES (VIEJOS)
    // ==========================================

    // Gris si es Futuro, Azul si es Pasado
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Si "EsFuturo" es true -> Gris
            if ( value is bool esFuturo && esFuturo )
            {
                return Brushes.LightGray;
            }
            // Si no -> Azul Material
            return new SolidColorBrush(( Color ) ColorConverter.ConvertFromString("#2196F3"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Transparente si es Futuro
    public class BoolToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ( value is bool esFuturo && esFuturo )
            {
                return 0.3; // 30% Opacidad
            }
            return 1.0; // 100% Opacidad
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}