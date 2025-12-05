using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Forms;
using WPF_PAR.Core;
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
                _isDarkMode = value;
                _themeService.SetThemeMode(value);
            }
        }
        public ObservableCollection<ColorItem> ColoresDisponibles { get; set; }
        public RelayCommand CambiarColorCommand { get; set; }
        public SettingsViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;

            _themeService = new ThemeService();
            _sucursalesService = new SucursalesService();

            IsDarkMode = Properties.Settings.Default.IsDarkMode;

            ColoresDisponibles = new ObservableCollection<ColorItem>
            {
                new () { Nombre = "Purple", CodigoHex = "#9C27B0" },
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
    }
}
