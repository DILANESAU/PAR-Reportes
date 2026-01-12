using System;
using System.Collections.Generic;
using System.Linq;

using WPF_PAR.Core;
using WPF_PAR.Services;
using WPF_PAR.Services.Interfaces;

namespace WPF_PAR.MVVM.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        public FilterService GlobalFilters { get; }
        public Dictionary<int, string> ListaSucursales { get; set; }


        // --- 2. COMANDOS DE NAVEGACIÓN ---
        public RelayCommand DashboardViewCommand { get; set; }
        public RelayCommand FamiliaViewCommand { get; set; }
        public RelayCommand ClientesViewCommand { get; set; } 
        public RelayCommand SettingsViewCommand { get; set; }

        public RelayCommand NavegarLineaCommand { get; set; }
        public RelayCommand ToggleMenuCommand { get; set; }

        public DashboardViewModel DashboardVM { get; }
        public FamiliaViewModel FamiliaVM { get; }
        public ClientesViewModel ClientesVM { get; }
        public SettingsViewModel SettingsVM { get; }


        // --- 4. ESTADO DE LA VISTA ---
        private object _currentView;
        public object CurrentView
        {
            get => _currentView;
            set { _currentView = value; OnPropertyChanged(); AreFiltersVisible = !( value is SettingsViewModel ); }
        }

        private bool _isMenuOpen = true;
        public bool IsMenuOpen
        {
            get => _isMenuOpen;
            set { _isMenuOpen = value; OnPropertyChanged(); }
        }
        private bool _areFiltersVisible = true;
        public bool AreFiltersVisible
        {
            get => _areFiltersVisible;
            set { _areFiltersVisible = value; OnPropertyChanged(); }
        }

        // --- CONSTRUCTOR ---
        public MainViewModel(FilterService filterService,
        DashboardViewModel dashboardVM,
        FamiliaViewModel familiaVM,
        ClientesViewModel clientesVM,
        SettingsViewModel settingsVM)
        {
            GlobalFilters = filterService;
            DashboardVM = dashboardVM;
            FamiliaVM = familiaVM;
            ClientesVM = clientesVM;
            SettingsVM = settingsVM;
            ListaSucursales = filterService.ListaSucursales;

            // D. CONFIGURAR COMANDOS
            DashboardViewCommand = new RelayCommand(o => CurrentView = DashboardVM);
            FamiliaViewCommand = new RelayCommand(o => { CurrentView = FamiliaVM; FamiliaVM.CargarDatosIniciales(); });
            ClientesViewCommand = new RelayCommand(o => CurrentView = ClientesVM);
            SettingsViewCommand = new RelayCommand(o => CurrentView = SettingsVM);

            // Navegación específica (desde el menú lateral "Arquitectónica", etc.)
            NavegarLineaCommand = new RelayCommand(parametro =>
            {
                if ( parametro is string linea )
                {
                    CurrentView = FamiliaVM;
                    FamiliaVM.CargarPorLinea(linea);
                }
            });

            ToggleMenuCommand = new RelayCommand(o => IsMenuOpen = !IsMenuOpen);


            // E. VISTA INICIAL
            CurrentView = DashboardVM;
        }
    }
}