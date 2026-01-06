using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.ComponentModel;
using WPF_PAR.Core;
using WPF_PAR.MVVM.Models;
using WPF_PAR.Services;
using WPF_PAR.Services.Interfaces;

namespace WPF_PAR.MVVM.ViewModels
{
    public class ClientesViewModel : ObservableObject
    {
        private readonly ReportesService _reportesService;
        private readonly ClientesService _clientesService; // Usaremos ClientesService, no LogicService
        private readonly IDialogService _dialogService;

        // ---------------------------------------------------------
        // PROPIEDADES
        // ---------------------------------------------------------

        // Esta es la colección que usa tu DataGrid en el XAML
        private ObservableCollection<ClienteRankingModel> _listaClientes;
        public ObservableCollection<ClienteRankingModel> ListaClientes
        {
            get => _listaClientes;
            set { _listaClientes = value; OnPropertyChanged(); }
        }

        private List<ClienteRankingModel> _datosCache; // Cache para filtrar sin ir a SQL

        private int _totalClientesActivos;
        public int TotalClientesActivos
        {
            get => _totalClientesActivos;
            set { _totalClientesActivos = value; OnPropertyChanged(); }
        }

        public FilterService Filters { get; }

        public List<int> AñosDisponibles { get; set; } // Faltaba esta propiedad

        private int _anioSeleccionado;
        public int AnioSeleccionado
        {
            get => _anioSeleccionado;
            set
            {
                _anioSeleccionado = value;
                OnPropertyChanged();
                // Al cambiar el año, recargamos datos
                if ( !IsLoading ) CargarDatos();
            }
        }

        // Propiedad para el buscador
        private string _textoBusqueda;
        public string TextoBusqueda
        {
            get => _textoBusqueda;
            set
            {
                _textoBusqueda = value;
                OnPropertyChanged();
                AplicarFiltroVisual(); // Filtra la tabla
            }
        }

        private bool _isLoading;
        public bool IsLoading { get => _isLoading; set { _isLoading = value; OnPropertyChanged(); } }

        // ---------------------------------------------------------
        // COMANDOS
        // ---------------------------------------------------------
        public RelayCommand ActualizarCommand { get; set; }

        // ---------------------------------------------------------
        // CONSTRUCTOR
        // ---------------------------------------------------------
        public ClientesViewModel(
            ReportesService reportesService,
            ClientesService clientesService, // Inyectamos ClientesService
            FilterService filterService,
            IDialogService dialogService)
        {
            _reportesService = reportesService;
            _clientesService = clientesService;
            Filters = filterService;
            _dialogService = dialogService;

            ListaClientes = new ObservableCollection<ClienteRankingModel>();
            _datosCache = new List<ClienteRankingModel>();

            int actual = DateTime.Now.Year;
            AñosDisponibles = new List<int> { actual, actual - 1, actual - 2, actual - 3, actual - 4 };
            _anioSeleccionado = actual; // Asignamos directamente al campo para no disparar recarga aun

            ActualizarCommand = new RelayCommand(o => CargarDatos());

            Filters.OnFiltrosCambiados += CargarDatos;
            // Carga inicial
            CargarDatos();
        }

        public async void CargarDatos()
        {
            IsLoading = true;
            try
            {
                // Usamos el servicio de clientes para obtener el reporte anual
                // Nota: Asegúrate de que Filters.SucursalId tenga valor
                var datos = await _clientesService.ObtenerReporteAnualClientes(Filters.SucursalId, AnioSeleccionado);

                _datosCache = datos;
                ListaClientes = new ObservableCollection<ClienteRankingModel>(datos);

                TotalClientesActivos = datos.Count;
            }
            catch ( Exception ex )
            {
                _dialogService.ShowMessage("Error", "No se pudieron cargar los clientes: " + ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void AplicarFiltroVisual()
        {
            if ( _datosCache == null ) return;

            IEnumerable<ClienteRankingModel> filtrado = _datosCache;

            if ( !string.IsNullOrWhiteSpace(TextoBusqueda) )
            {
                string q = TextoBusqueda.ToUpper();
                filtrado = filtrado.Where(x => x.Nombre.ToUpper().Contains(q) || x.ClaveCliente.Contains(q));
            }

            ListaClientes = new ObservableCollection<ClienteRankingModel>(filtrado);
            TotalClientesActivos = ListaClientes.Count;
        }
    }
}