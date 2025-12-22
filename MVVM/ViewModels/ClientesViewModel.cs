using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using WPF_PAR.Core;
using WPF_PAR.MVVM.Models;
using WPF_PAR.Services;
using WPF_PAR.Services.Interfaces;

namespace WPF_PAR.MVVM.ViewModels
{
    public class ClientesViewModel : ObservableObject
    {
        private readonly ClientesService _clientesService;
        private readonly IDialogService _dialogService;

        // Propiedad Filters Pública para el XAML
        public FilterService Filters { get; private set; }

        public List<int> AñosDisponibles { get; set; }

        private int _anioSeleccionado;
        public int AnioSeleccionado
        {
            get => _anioSeleccionado;
            set
            {
                if ( _anioSeleccionado != value )
                {
                    _anioSeleccionado = value;
                    OnPropertyChanged();
                    // Recargar automáticamente al cambiar el año
                    CargarDatos();
                }
            }
        }

        public ObservableCollection<ClienteRankingModel> ListaClientes { get; set; }

        // --- KPIs ---
        private int _clientesEnRiesgo;
        public int ClientesEnRiesgo
        {
            get => _clientesEnRiesgo;
            set { _clientesEnRiesgo = value; OnPropertyChanged(); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public RelayCommand ActualizarCommand { get; set; }

        public ClientesViewModel(IDialogService dialogService, FilterService filterService)
        {
            _clientesService = new ClientesService();
            _dialogService = dialogService;

            // 1. VALIDACIÓN EN CONSTRUCTOR
            Filters = filterService ?? throw new ArgumentNullException(nameof(filterService));

            int actual = DateTime.Now.Year;
            AñosDisponibles = new List<int> { actual, actual - 1, actual - 2, actual - 3, actual - 4 };
            AnioSeleccionado = actual;

            ListaClientes = new ObservableCollection<ClienteRankingModel>();
            ActualizarCommand = new RelayCommand(o => CargarDatos());

            // Suscripción segura al evento del filtro (si existe)
            Filters.OnFiltrosCambiados += CargarDatos;

            CargarDatos();
        }

        public async void CargarDatos()
        {
            if ( IsLoading ) return;

            IsLoading = true;
            try
            {
                // 2. VALIDACIÓN DE DEPENDENCIA
                if ( Filters == null ) return;

                var datos = await _clientesService.ObtenerReporteAnualClientes(
                    Filters.SucursalId,
                    AnioSeleccionado
                );

                ListaClientes.Clear();

                // 3. LA SOLUCIÓN AL ERROR DE 2DA VUELTA
                // Si el servicio devuelve null (por error de conexión o lo que sea),
                // esto evita que la App se cierre.
                if ( datos != null )
                {
                    foreach ( var c in datos ) ListaClientes.Add(c);

                    // Solo calculamos si hay datos y si la propiedad existe
                    // (Asumiendo que quieres contar los que bajaron ventas)
                    if ( datos.Any() )
                    {
                        // Ejemplo: Clientes que compraron menos que el promedio o 0 este mes
                        // Ajusta esta lógica según tu modelo real
                        ClientesEnRiesgo = datos.Count(c => c.Diciembre == 0);
                    }
                }
            }
            catch ( Exception ex )
            {
                _dialogService.ShowError("Error al cargar datos: " + ex.Message, "Error");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}