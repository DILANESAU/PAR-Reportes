using MaterialDesignThemes.Wpf;

using System;
using System.Collections.Generic;

using WPF_PAR.Core;
using WPF_PAR.Services;
using WPF_PAR.Services.Interfaces;

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

        public SnackbarMessageQueue MessageQueue { get; }

        // --- CONSTRUCTOR ---
        public MainViewModel(
            FilterService filterService,
            DashboardViewModel dashboardVM,
            FamiliaViewModel familiaVM,
            ClientesViewModel clientesVM,
            SettingsViewModel settingsVM, INotificationService notificationService)
        {
            GlobalFilters = filterService;
            DashboardVM = dashboardVM;
            FamiliaVM = familiaVM;
            ClientesVM = clientesVM;
            SettingsVM = settingsVM;

            // Obtenemos lista para el Combo Global (si se usa en MainView)
            ListaSucursales = filterService.ListaSucursales;

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
                FamiliaVM.DetenerRenderizado();
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

            if ( notificationService is NotificationService servicioConcreto )
            {
                MessageQueue = servicioConcreto.MessageQueue;
            }
            // VISTA INICIAL
            CurrentView = DashboardVM;
        }
    }
}