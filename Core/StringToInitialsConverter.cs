using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace WPF_PAR.Core.Converters
{
    public class StringToInitialsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ( value is string nombre && !string.IsNullOrWhiteSpace(nombre) )
            {
                // Divide el nombre por espacios y quita los vacíos
                var partes = nombre.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if ( partes.Length == 0 ) return string.Empty;

                // Toma la primera letra de la primera palabra
                string iniciales = partes[0][0].ToString();

                // Si hay más palabras, toma la primera letra de la segunda palabra también
                if ( partes.Length > 1 )
                {
                    iniciales += partes[1][0].ToString();
                }

                return iniciales.ToUpper();
            }

            return string.Empty; // O un signo de "?" si prefieres
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}