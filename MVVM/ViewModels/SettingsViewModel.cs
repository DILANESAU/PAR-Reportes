using System.Collections.ObjectModel;

using WPF_PAR.Core;
using WPF_PAR.MVVM.Models;
using WPF_PAR.Services;
using WPF_PAR.Services.Interfaces;

namespace WPF_PAR.MVVM.ViewModels
{
    public class SettingsViewModel : ObservableObject
    {
        private readonly SucursalesService _sucursalesService;
        public Dictionary <int, string> MisSucursales { get; set; }
        private readonly IDialogService _dialogService;
        public int MiSucursalDefault { get; set; }
        private readonly ThemeService _themeService;
        private bool _isDarkMode;
        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if ( _isDarkMode != value )
                {
                    _isDarkMode = value;
                    OnPropertyChanged();

                    _themeService.SetThemeMode(_isDarkMode);
                }
            }
        }
        public ObservableCollection<OpcionColor> ColoresDisponibles { get; set; }
        public RelayCommand CambiarColorCommand { get; set; }
        public RelayCommand GuardarCommand { get; set; }
        public SettingsViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;

            _themeService = new ThemeService();
            _sucursalesService = new SucursalesService();

            try
            { _isDarkMode = Properties.Settings.Default.IsDarkMode; }
            catch
            { _isDarkMode = false;}

            GuardarCommand = new RelayCommand(o => GuardarTodo());

            ColoresDisponibles = new ObservableCollection<OpcionColor>
            {
                new OpcionColor { Nombre = "Morado (Default)", CodigoHex = "#673AB7" },
                new OpcionColor { Nombre = "Azul", CodigoHex = "#2196F3" },
                new OpcionColor { Nombre = "Verde", CodigoHex = "#4CAF50" },
                new OpcionColor { Nombre = "Naranja", CodigoHex = "#FF9800" },
                new OpcionColor { Nombre = "Rojo", CodigoHex = "#F44336" },
                new OpcionColor { Nombre = "Rosa", CodigoHex = "#E91E63" },
                new OpcionColor { Nombre = "Verde Azulado", CodigoHex = "#009688" },
                new OpcionColor { Nombre = "Gris Azulado", CodigoHex = "#607D8B" }
            };
            string colorGuardado = Properties.Settings.Default.PrimayColor;
            if ( string.IsNullOrEmpty(colorGuardado) ) colorGuardado = "#673AB7"; // Default

            MarcarColorSeleccionado(colorGuardado);

            CambiarColorCommand = new RelayCommand(param =>
            {
                if ( param is string hex )
                {
                    // Cambiar el tema visualmente
                    _themeService.SetPrimaryColor(hex);

                    // Actualizar la palomita en la UI
                    MarcarColorSeleccionado(hex);
                }
            });
            CargarConfiguracionSucursales();
        }
        private void CargarConfiguracionSucursales()
        {
            var todas = _sucursalesService.CargarSucursales();

            if ( Session.UsuarioActual.SucursalesPermitidas == null )
            {
                MisSucursales = todas;
            }
            else
            {
                MisSucursales = todas
                    .Where(s => Session.UsuarioActual.SucursalesPermitidas.Contains(s.Key))
                    .ToDictionary(k => k.Key, v => v.Value);
            }

            int guardada = Properties.Settings.Default.SucursalDefaultId;

            if ( MisSucursales.ContainsKey(guardada) )
            {
                MiSucursalDefault = guardada;
            }
            else if ( MisSucursales.Count > 0 )
            {
                MiSucursalDefault = MisSucursales.Keys.First();
            }
            OnPropertyChanged(nameof(MisSucursales));
            OnPropertyChanged(nameof(MiSucursalDefault));
        }

        private void GuardarTodo()
        {
            Properties.Settings.Default.SucursalDefaultId = MiSucursalDefault;

            Properties.Settings.Default.Save();
            _dialogService.ShowMessage("Configuración guardada correctamente.", "Éxito");
        }
        public class ColorItem
        {
            public string Nombre { get; set; }
            public string CodigoHex { get; set; }
        }
        private void MarcarColorSeleccionado(string hex)
        {
            foreach ( var color in ColoresDisponibles )
            {
                // Si el hex coincide, lo marcamos como True, si no, False
                // Usamos ToUpper() para evitar problemas de mayúsculas/minúsculas
                color.EsSeleccionado = color.CodigoHex.ToUpper() == hex.ToUpper();
            }
        }
    }
}
