using MaterialDesignThemes.Wpf;

using System;
using System.Collections.Generic;

using WPF_PAR.Core;
using WPF_PAR.Services;

namespace WPF_PAR.MVVM.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        public FilterService GlobalFilters { get; }
        public Dictionary<int, string> ListaSucursales { get; set; }

        // --- COMANDOS DE NAVEGACIÓN ---
        public RelayCommand DashboardViewCommand { get; set; }
        public RelayCommand FamiliaViewCommand { get; set; }
        public RelayCommand ClientesViewCommand { get; set; }
        public RelayCommand SettingsViewCommand { get; set; }

        public RelayCommand NavegarLineaCommand { get; set; }
        public RelayCommand ToggleMenuCommand { get; set; }

        // --- VIEWMODELS HIJOS ---
        public DashboardViewModel DashboardVM { get; }
        public FamiliaViewModel FamiliaVM { get; }
        public ClientesViewModel ClientesVM { get; }
        public SettingsViewModel SettingsVM { get; }
        // --- MENSAJES EMERGENTES ---
        private SnackbarMessageQueue _messageQueue;
        public SnackbarMessageQueue MessageQueue
        {
            get => _messageQueue;
            set { _messageQueue = value; OnPropertyChanged(); }
        }

        // --- ESTADO DE LA VISTA ---
        private object _currentView;
        public object CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged();
                // Ocultar barra de filtros si estamos en Configuración
                AreFiltersVisible = !( value is SettingsViewModel );
            }
        }

        private bool _isMenuOpen = true; // Para colapsar/expandir el menú lateral
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
        public MainViewModel(
            FilterService filterService,
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

            // Obtenemos lista para el Combo Global (si se usa en MainView)
            ListaSucursales = filterService.ListaSucursales;
            MessageQueue = new SnackbarMessageQueue(TimeSpan.FromSeconds(3));

            // -----------------------------------------------------------
            // CONFIGURACIÓN DE COMANDOS (AQUÍ ESTÁ EL CAMBIO)
            // -----------------------------------------------------------

            // 1. Dashboard
            DashboardViewCommand = new RelayCommand(o => CurrentView = DashboardVM);

            // 2. Familias (Ya lo tenías bien: carga al entrar)
            FamiliaViewCommand = new RelayCommand(o =>
            {
                CurrentView = FamiliaVM;
                FamiliaVM.CargarDatosIniciales();
            });

            ClientesViewCommand = new RelayCommand(o =>
            {
                CurrentView = ClientesVM;
                ClientesVM.CargarDatosIniciales();
            });

            // 4. Configuración
            SettingsViewCommand = new RelayCommand(o => CurrentView = SettingsVM);

            // 5. Navegación Específica (Desde menú lateral tipo "Arquitectónica")
            NavegarLineaCommand = new RelayCommand(parametro =>
            {
                if ( parametro is string linea )
                {
                    CurrentView = FamiliaVM;
                    FamiliaVM.CargarPorLinea(linea);

                    // Opcional: Cerrar menú en móviles o pantallas chicas
                    // IsMenuOpen = false; 
                }
            });

            ToggleMenuCommand = new RelayCommand(o => IsMenuOpen = !IsMenuOpen);

            // VISTA INICIAL
            CurrentView = DashboardVM;
        }
    }
}