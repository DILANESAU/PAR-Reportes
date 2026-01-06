using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using WPF_PAR.Core;
using WPF_PAR.MVVM.Models;
using WPF_PAR.Services;
using WPF_PAR.Services.Interfaces;

namespace WPF_PAR.MVVM.ViewModels
{
    public class ClientesViewModel : ObservableObject
    {
        private readonly ReportesService _reportesService;
        private readonly ClientesLogicService _logicService; // Reusamos la lógica de cálculo
        private readonly IDialogService _dialogService;

        // ---------------------------------------------------------
        // PROPIEDADES
        // ---------------------------------------------------------

        // La lista principal de tarjetas (Q1, Q2...)
        private ObservableCollection<SubLineaPerformanceModel> _listaClientes;
        public ObservableCollection<SubLineaPerformanceModel> ListaClientes
        {
            get => _listaClientes;
            set { _listaClientes = value; OnPropertyChanged(); }
        }

        // Datos crudos en caché para no ir a SQL cada vez que cambias de Trimestre a Semestre
        private List<VentaReporteModel> _datosCache;

        // Contador de Clientes (La enumeración que pediste)
        private int _totalClientesActivos;
        public int TotalClientesActivos
        {
            get => _totalClientesActivos;
            set { _totalClientesActivos = value; OnPropertyChanged(); }
        }

        // Totales Monetarios
        private decimal _granTotalVenta;
        public decimal GranTotalVenta { get => _granTotalVenta; set { _granTotalVenta = value; OnPropertyChanged(); } }

        // Filtros
        public FilterService Filters { get; } // Tu servicio de filtros global (Sucursal, Fechas)

        private int _anioSeleccionado;
        public int AnioSeleccionado
        {
            get => _anioSeleccionado;
            set
            {
                _textoBusqueda = value;
                OnPropertyChanged();
                AplicarFiltrosLocales(); // Buscador en tiempo real
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
        public bool IsLoading { get => _isLoading; set { _isLoading = value; OnPropertyChanged(); } }

        // Periodo actual (para los botones)
        private string _periodoActual = "ANUAL";

        // ---------------------------------------------------------
        // COMANDOS
        // ---------------------------------------------------------
        public RelayCommand ActualizarCommand { get; set; }
        public RelayCommand CambiarPeriodoCommand { get; set; }

        // ---------------------------------------------------------
        // CONSTRUCTOR
        // ---------------------------------------------------------
        public ClientesViewModel(
            ReportesService reportesService,
            ClientesLogicService logicService,
            FilterService filterService,
            IDialogService dialogService)
        {
            _reportesService = reportesService;
            _logicService = logicService;
            Filters = filterService;
            _dialogService = dialogService;

            ListaClientes = new ObservableCollection<SubLineaPerformanceModel>();
            _datosCache = new List<VentaReporteModel>();

            int actual = DateTime.Now.Year;
            AñosDisponibles = new List<int> { actual, actual - 1, actual - 2, actual - 3, actual - 4 };
            AnioSeleccionado = actual;

            ListaClientes = new ObservableCollection<ClienteRankingModel>();
            ActualizarCommand = new RelayCommand(o => CargarDatos());
            CambiarPeriodoCommand = new RelayCommand(p =>
            {
                if ( p is string periodo ) GenerateTarjetas(periodo);
            });

            // Carga inicial
            CargarDatos();
        }

        public async void CargarDatos()
        {
            IsLoading = true;
            try
            {
                // 1. Obtenemos ventas de TODO el año actual para poder hacer comparativas (Ene-Dic)
                // Usamos el año de la fecha seleccionada en el filtro, o el actual.
                string anio = Filters.FechaInicio.Year.ToString();

                // Reutilizamos el método que trae datos por artículo, o creamos uno optimizado solo por cliente
                // Para este ejemplo, asumimos que obtienes el listado de ventas detallado
                var rawData = await _reportesService.ObtenerHistoricoAnualPorArticulo(anio, Filters.SucursalId.ToString());

                ListaClientes.Clear();

                // 2. Calcular KPIs Generales
                GranTotalVenta = _datosCache.Sum(x => x.TotalVenta); // O la propiedad que uses para sumar
                TotalClientesActivos = _datosCache.Select(x => x.Cliente).Distinct().Count();

                // 3. Generar Tarjetas con el periodo default
                GenerateTarjetas("ANUAL"); // Por defecto muestra Trimestres (Q1-Q4)
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

        private void GenerateTarjetas(string periodo)
        {
            _periodoActual = periodo;
            AplicarFiltrosLocales();
        }

        private void AplicarFiltrosLocales()
        {
            if ( _datosCache == null ) return;

            // 1. Filtramos los datos base por el Buscador (Texto)
            var datosFiltrados = _datosCache.AsEnumerable();

            if ( !string.IsNullOrWhiteSpace(TextoBusqueda) )
            {
                string q = TextoBusqueda.ToUpper();
                datosFiltrados = datosFiltrados.Where(x => x.Cliente != null && x.Cliente.ToUpper().Contains(q));
            }

            // 2. Usamos el LogicService para crear las tarjetas (Q1, Q2, etc)
            // Nota: Si periodo es "ANUAL", mandamos "TRIMESTRAL" para ver el desglose Q1-Q4
            string modoCalculo = _periodoActual == "ANUAL" ? "TRIMESTRAL" : _periodoActual;

            var listaTarjetas = _logicService.CalcularDesgloseClientes(datosFiltrados.ToList(), modoCalculo);

            ListaClientes = new ObservableCollection<SubLineaPerformanceModel>(listaTarjetas);

            // Actualizamos el contador visual según la búsqueda actual
            // (Opcional: si quieres que el contador baje cuando buscas "JUAN")
             TotalClientesActivos = ListaClientes.Count; 
        }
    }
}