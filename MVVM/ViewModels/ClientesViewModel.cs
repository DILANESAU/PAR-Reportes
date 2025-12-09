using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

using WPF_PAR.Core;
using WPF_PAR.MVVM.Models;
using WPF_PAR.Services;
using WPF_PAR.Services.Interfaces;

namespace WPF_PAR.MVVM.ViewModels
{
    public class ClientesViewModel : ObservableObject
    {
        private readonly ClientesService _clientesService;
        private readonly SucursalesService _sucursalesService;
        private readonly IDialogService _dialogService;
        private readonly FilterService _filters;

        public ObservableCollection<ClienteRankingModel> ListaClientes { get; set; }
        private List<ClienteRankingModel> _datosOriginales;

        // --- FILTROS PROFESIONALES ---
        public Dictionary<int, string> ListaSucursales { get; set; }

        private int _sucursalSeleccionadaId;
        public int SucursalSeleccionadaId
        {
            get => _sucursalSeleccionadaId;
            set { _sucursalSeleccionadaId = value; OnPropertyChanged(); }
        }

        private DateTime _fechaInicio;
        public DateTime FechaInicio
        {
            get => _fechaInicio;
            set { _fechaInicio = value; OnPropertyChanged(); }
        }

        private DateTime _fechaFin;
        public DateTime FechaFin
        {
            get => _fechaFin;
            set { _fechaFin = value; OnPropertyChanged(); }
        }
        // -----------------------------

        private bool _isLoading;
        public bool IsLoading { get => _isLoading; set { _isLoading = value; OnPropertyChanged(); } }

        public RelayCommand ActualizarCommand { get; set; }
        public RelayCommand OrdenarMejoresCommand { get; set; }
        public RelayCommand OrdenarPeoresCommand { get; set; }

        public ClientesViewModel(IDialogService dialogService , FilterService filters)
        {
            _clientesService = new ClientesService();
            _sucursalesService = new SucursalesService();
            _dialogService = dialogService;
            _filters = filters;



            ListaClientes = new ObservableCollection<ClienteRankingModel>();

            // Inicializar Filtros
            ConfigurarFiltros();

            ActualizarCommand = new RelayCommand(o => CargarDatos());
            OrdenarMejoresCommand = new RelayCommand(o => AplicarOrden("MEJORES"));
            OrdenarPeoresCommand = new RelayCommand(o => AplicarOrden("RIESGO"));

            // Carga inicial
            CargarDatos();

        }

        private void ConfigurarFiltros()
        {
            // Cargar Sucursales (Igual que en otros VMs)
            var todas = _sucursalesService.CargarSucursales();
            // Aquí podrías filtrar por permisos de usuario si quisieras
            ListaSucursales = todas;

            // Default: Sucursal guardada o la primera
            int guardada = Properties.Settings.Default.SucursalDefaultId;
            SucursalSeleccionadaId = ListaSucursales.ContainsKey(guardada) ? guardada : ListaSucursales.Keys.FirstOrDefault();

            // Default Fechas: Del día 1 del mes actual hasta hoy
            DateTime hoy = DateTime.Now;
            FechaInicio = new DateTime(hoy.Year, hoy.Month, 1);
            FechaFin = hoy;
        }

        public async void CargarDatos()
        {
            IsLoading = true;
            try
            {
                var datos = await _clientesService.ObtenerRankingClientes(SucursalSeleccionadaId, FechaInicio, FechaFin);
                _datosOriginales = datos;

                AplicarOrden("MEJORES");
            }
            catch ( Exception ex )
            {
                _dialogService.ShowError("Error al consultar clientes: " + ex.Message, "Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void AplicarOrden(string tipo)
        {
            if ( _datosOriginales == null ) return;
            ListaClientes.Clear();

            var ordenados = tipo == "RIESGO"
                ? _datosOriginales.OrderBy(c => c.Diferencia).ToList()
                : _datosOriginales.OrderByDescending(c => c.VentaActual).ToList();

            foreach ( var c in ordenados ) ListaClientes.Add(c);
        }
    }
}
