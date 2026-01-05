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
            set
            {
                _currentView = value;
                OnPropertyChanged();
                AreFiltersVisible = !( value is SettingsViewModel );
            }
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

            // D. CONFIGURAR COMANDOS CON CIERRE DE MENÚ AUTOMÁTICO

            DashboardViewCommand = new RelayCommand(o =>
            {
                CurrentView = DashboardVM;
                IsMenuOpen = false; // <--- Cierra el menú al navegar
            });

            FamiliaViewCommand = new RelayCommand(o =>
            {
                CurrentView = FamiliaVM;
                FamiliaVM.CargarPorLinea("Todas");
                IsMenuOpen = false; // <--- Cierra el menú al navegar
            });

            ClientesViewCommand = new RelayCommand(o =>
            {
                CurrentView = ClientesVM;
                IsMenuOpen = false; // <--- Cierra el menú al navegar
            });

            SettingsViewCommand = new RelayCommand(o =>
            {
                CurrentView = SettingsVM;
                IsMenuOpen = false; // <--- Cierra el menú al navegar
            });

            // Navegación específica (desde sub-menús si los tuvieras)
            NavegarLineaCommand = new RelayCommand(parametro =>
            {
                if ( parametro is string linea )
                {
                    CurrentView = FamiliaVM;
                    FamiliaVM.CargarPorLinea(linea);
                    IsMenuOpen = false; // <--- También cierra el menú aquí
                }
            });

            ToggleMenuCommand = new RelayCommand(o => IsMenuOpen = !IsMenuOpen);

            // E. VISTA INICIAL
            CurrentView = DashboardVM;
        }
    }
}