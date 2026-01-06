using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;

namespace WPF_PAR.Core.Converters
{
    //si es true lo manda gris y si es falso azul
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ( value is bool esFuturo && esFuturo )
            {
                return Brushes.LightGray; 
            }

            //Material desing para los periodos y otras cosas
            return new SolidColorBrush(( Color ) ColorConverter.ConvertFromString("#2196F3"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    //Si es true lo vuelve opaco pero si es false pss se ve normal
    public class BoolToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ( value is bool esFuturo && esFuturo )
            {
                return 0.3;
            }
            return 1.0; 
        }
        //regresa todo a la normalidad en caso de bugs visuales (sin uso por ahora)
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        //chingadera pa convertir de minusculas o capitalizado a mayusculas
        public class ToUpperConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if ( value is string texto )
                {
                    return texto.ToUpper();
                }
                return value;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }
}
