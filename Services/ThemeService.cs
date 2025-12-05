using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace WPF_PAR.Services
{
    class ThemeService
    {
        private readonly PaletteHelper _paletteHelper = new();

        public void SetThemeMode(bool isDark)
        {
            Theme theme = _paletteHelper.GetTheme();

            theme.SetBaseTheme(isDark ? BaseTheme.Dark : BaseTheme.Light);

            _paletteHelper.SetTheme(theme);

            // Guardar preferencia
            Properties.Settings.Default.IsDarkMode = isDark;
            Properties.Settings.Default.Save();
        }
        // Cambiar Color Primario
        public void SetPrimaryColor(string colorName)
        {
            Color color = System.Windows.Media.Colors.Purple; // Default

            try
            {
                // Convertir string Hex o Nombre a Color
                color = ( Color ) ColorConverter.ConvertFromString(colorName);
            }
            catch { }

            // CAMBIO: Usamos 'Theme' aquí también
            Theme theme = _paletteHelper.GetTheme();

            // En v5, SetPrimaryColor acepta directamente el color
            theme.SetPrimaryColor(color);

            double luminancia = ( color.R * 0.299 + color.G * 0.587 + color.B * 0.114 );

            Color colorTexto = luminancia < 130 ? Colors.White : Colors.Black;

            theme.PrimaryMid = new MaterialDesignColors.ColorPair(color, colorTexto);

            _paletteHelper.SetTheme(theme);

            // Guardar preferencia
            Properties.Settings.Default.PrimayColor = colorName;
            Properties.Settings.Default.Save();
        }

        // Cargar lo guardado al iniciar la app
        public void LoadSavedTheme()
        {
            // Cargar Modo
            bool isDark = Properties.Settings.Default.IsDarkMode;
            SetThemeMode(isDark);

            // Cargar Color
            string color = Properties.Settings.Default.PrimayColor;
            if ( !string.IsNullOrEmpty(color) )
            {
                SetPrimaryColor(color);
            }
        }
    }
}
