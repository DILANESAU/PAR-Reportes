using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Forms;

using WPF_PAR.Core;
using WPF_PAR.Services;

namespace WPF_PAR.MVVM.ViewModels
{
    public class SettingsViewModel : ObservableObject
    {
        private readonly SucursalesService _sucursalesService;
        public Dictionary <int, string> MisSucursales { get; set; }
        public int MiSucursalDefault { get; set; }
        private readonly ThemeService _themeService;
        private bool _isDarkMode;
        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                _isDarkMode = value;
                _themeService.SetThemeMode(value);
            }
        }
        public ObservableCollection<ColorItem> ColoresDisponibles { get; set; }
        public RelayCommand CambiarColorCommand { get; set; }
        public SettingsViewModel()
        {
            _themeService = new ThemeService();
            _sucursalesService = new SucursalesService();

            IsDarkMode = Properties.Settings.Default.IsDarkMode;

            ColoresDisponibles = new ObservableCollection<ColorItem>
            {
                new () { Nombre = "Purple", CodigoHex = "#9C27B0" },
                new () { Nombre = "Indigo", CodigoHex = "#3F51B5" },
                new () { Nombre = "Blue", CodigoHex = "#2196F3" },
                new () { Nombre = "Teal", CodigoHex = "#009688" },
                new () { Nombre = "Green", CodigoHex = "#4CAF50" },
                new () { Nombre = "Amber", CodigoHex = "#FFC107" },
                new () { Nombre = "DeepOrange", CodigoHex = "#FF5722" },
                new () { Nombre = "Red", CodigoHex = "#F44336" },
                new () { Nombre = "BlueGrey", CodigoHex = "#607D8B" }
            };

            CambiarColorCommand = new RelayCommand(colorHex =>
            {
                if ( colorHex is string codigo )
                    _themeService.SetPrimaryColor(codigo);
            });
            CargarConfiguracionSucursales();
        }
        private void CargarConfiguracionSucursales()
        {
            // 1. Obtener todas las sucursales del CSV
            var todas = _sucursalesService.CargarSucursales();

            // 2. Filtrar solo las que el usuario actual puede ver
            // (Usamos la misma lógica que en el Dashboard)
            if ( Session.UsuarioActual.SucursalesPermitidas == null )
            {
                MisSucursales = todas; // Admin ve todas
            }
            else
            {
                MisSucursales = todas
                    .Where(s => Session.UsuarioActual.SucursalesPermitidas.Contains(s.Key))
                    .ToDictionary(k => k.Key, v => v.Value);
            }

            // 3. Cargar el valor guardado
            int guardada = Properties.Settings.Default.SucursalDefaultId;

            // Verificamos si la guardada sigue siendo válida para este usuario
            if ( MisSucursales.ContainsKey(guardada) )
            {
                MiSucursalDefault = guardada;
            }
            else if ( MisSucursales.Count > 0 )
            {
                MiSucursalDefault = MisSucursales.Keys.First();
            }
        }

        private void GuardarTodo()
        {
            // ... (Guardar SQL y Temas) ...

            // Guardar Sucursal Default
            Properties.Settings.Default.SucursalDefaultId = MiSucursalDefault;

            Properties.Settings.Default.Save();
            MessageBox.Show("Configuración guardada correctamente.", "Éxito");
        }
        public class ColorItem
        {
            public string Nombre { get; set; }
            public string CodigoHex { get; set; }
        }
    }
}
