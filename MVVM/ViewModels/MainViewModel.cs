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
        // --- 1. SERVICIO DE FILTROS GLOBAL (El Jefe) ---
        // Esta propiedad se enlaza a la barra superior de MainWindow.xaml
        public FilterService GlobalFilters { get; set; }

        // Lista de sucursales para el ComboBox global
        public Dictionary<int, string> ListaSucursales { get; set; }


        // --- 2. COMANDOS DE NAVEGACIÓN ---
        public RelayCommand DashboardViewCommand { get; set; }
        public RelayCommand FamiliaViewCommand { get; set; }
        public RelayCommand ClientesViewCommand { get; set; } // Nuevo módulo
        public RelayCommand SettingsViewCommand { get; set; }

        public RelayCommand NavegarLineaCommand { get; set; }
        public RelayCommand ToggleMenuCommand { get; set; }


        // --- 3. VIEWMODELS HIJOS ---
        public DashboardViewModel DashboardVM { get; set; }
        public FamiliaViewModel FamiliaVM { get; set; }
        public ClientesViewModel ClientesVM { get; set; } // Nuevo módulo
        public SettingsViewModel SettingsVM { get; set; }


        // --- 4. ESTADO DE LA VISTA ---
        private object _currentView;
        public object CurrentView
        {
            get => _currentView;
            set { _currentView = value; OnPropertyChanged(); }
        }

        private bool _isMenuOpen = true;
        public bool IsMenuOpen
        {
            get => _isMenuOpen;
            set { _isMenuOpen = value; OnPropertyChanged(); }
        }


        // --- CONSTRUCTOR ---
        public MainViewModel()
        {
            // A. CREAR SERVICIOS BASE (Infraestructura)
            IDialogService dialogService = new DialogService();
            ISnackbarService snackbarService = new SnackbarService();
            BusinessLogicService businessLogic = new BusinessLogicService();
            SucursalesService sucursalesService = new SucursalesService();


            // B. INICIALIZAR EL SISTEMA DE FILTROS
            GlobalFilters = new FilterService();

            // Cargar sucursales para el combo global (respetando permisos si existieran)
            // Aquí podrías filtrar usando Session.UsuarioActual.SucursalesPermitidas si quisieras
            ListaSucursales = sucursalesService.CargarSucursales();


            // C. INYECTAR DEPENDENCIAS A LOS HIJOS (Aquí ocurre la magia)
            // Todos reciben la MISMA instancia de 'GlobalFilters'

            DashboardVM = new DashboardViewModel(dialogService, GlobalFilters);

            FamiliaVM = new FamiliaViewModel(dialogService, snackbarService, businessLogic, GlobalFilters);

            ClientesVM = new ClientesViewModel(dialogService, GlobalFilters);

            SettingsVM = new SettingsViewModel(dialogService);


            // D. CONFIGURAR COMANDOS
            DashboardViewCommand = new RelayCommand(o => CurrentView = DashboardVM);

            FamiliaViewCommand = new RelayCommand(o =>
            {
                CurrentView = FamiliaVM;
                // Opcional: Resetear a "Todas" al entrar directo
                FamiliaVM.CargarPorLinea("Todas");
            });

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