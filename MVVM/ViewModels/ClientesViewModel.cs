using System.Collections.ObjectModel;
using WPF_PAR.Core;
using WPF_PAR.MVVM.Models;
using WPF_PAR.Services;
using WPF_PAR.Services.Interfaces;

namespace WPF_PAR.MVVM.ViewModels
{
    public class ClientesViewModel : ObservableObject
    {
        private readonly ClientesService _clientesService;
        private readonly ClientesLogicService _logicService;
        private readonly IDialogService _dialogService;
        private ObservableCollection<SubLineaPerformanceModel> _listaClientes;
        public ObservableCollection<SubLineaPerformanceModel> ListaClientes
        {
            get => _listaClientes;
            set { _listaClientes = value; OnPropertyChanged(); }
        }

        private List<ClienteRankingModel> _datosCache;

        private int _totalClientesActivos;
        public int TotalClientesActivos
        {
            get => _totalClientesActivos;
            set { _totalClientesActivos = value; OnPropertyChanged(); }
        }
        private decimal _granTotalVenta;
        public decimal GranTotalVenta { get => _granTotalVenta; set { _granTotalVenta = value; OnPropertyChanged(); } }
        public FilterService Filters { get; }

        private string _textoBusqueda;
        public string TextoBusqueda
        {
            get => _textoBusqueda;
            set
            {
                _textoBusqueda = value;
                OnPropertyChanged();
                _ = AplicarFiltrosLocalesAsync();
            }
        }

        private bool _isLoading;
        public bool IsLoading { get => _isLoading; set { _isLoading = value; OnPropertyChanged(); } }

        private string _periodoActual = "ANUAL";
        public RelayCommand ActualizarCommand { get; set; }
        public RelayCommand CambiarPeriodoCommand { get; set; }
        public ClientesViewModel(
            ClientesService clientesService,
            ClientesLogicService logicService,
            FilterService filterService,
            IDialogService dialogService)
        {
            _clientesService = clientesService;
            _logicService = logicService;
            Filters = filterService;
            _dialogService = dialogService;

            ListaClientes = new ObservableCollection<SubLineaPerformanceModel>();
            _datosCache = new List<ClienteRankingModel>();

            ActualizarCommand = new RelayCommand(o => CargarDatos());
            CambiarPeriodoCommand = new RelayCommand(p =>
            {
                if ( p is string periodo ) GenerateTarjetas(periodo);
            });
            CargarDatos();
        }

        private async void CargarDatos()
        {
            if ( IsLoading ) return;
            IsLoading = true;
            try
            {
                int anio = Filters.FechaInicio.Year;

                var rawData = await _clientesService.ObtenerReporteAnualClientes(Filters.SucursalId, anio);

                _datosCache = rawData;

                GranTotalVenta = _datosCache.Sum(x => x.TotalAnual);
                TotalClientesActivos = _datosCache.Count;

                await GenerateTarjetasAsync("ANUAL");
            }
            catch ( Exception ex )
            {
                _dialogService.ShowError("Error al cargar clientes", ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }
        private async void GenerateTarjetas(string periodo)
        {
            await GenerateTarjetasAsync(periodo);
        }

        private async Task GenerateTarjetasAsync(string periodo)
        {
            _periodoActual = periodo;
            await AplicarFiltrosLocalesAsync();
        }
        private async Task AplicarFiltrosLocalesAsync()
        {
            if ( _datosCache == null || !_datosCache.Any() ) return;

            IsLoading = true;
            string query = TextoBusqueda?.ToUpper();
            string periodo = _periodoActual;
            var datosBase = _datosCache; 

            var resultadoTarjetas = await Task.Run(() =>
            {
                IEnumerable<ClienteRankingModel> filtrados = datosBase;
                if ( !string.IsNullOrWhiteSpace(query) )
                {
                    filtrados = datosBase.Where(x =>
                        ( x.Nombre != null && x.Nombre.ToUpper().Contains(query) ) ||
                        ( x.ClaveCliente != null && x.ClaveCliente.Contains(query) )
                    );
                }

                var listaFiltrada = filtrados.ToList();
                string modoCalculo = periodo == "ANUAL" ? "TRIMESTRAL" : periodo;
                var tarjetasGeneradas = CalcularDesgloseLocal(listaFiltrada, modoCalculo);

                return new { Tarjetas = tarjetasGeneradas, Total = listaFiltrada.Count };
            });

            // VOLVER A UI
            ListaClientes = new ObservableCollection<SubLineaPerformanceModel>(resultadoTarjetas.Tarjetas);
            TotalClientesActivos = resultadoTarjetas.Total;

            IsLoading = false;
        }
        private List<SubLineaPerformanceModel> CalcularDesgloseLocal(List<ClienteRankingModel> datos, string modo)
        {
            var resultado = new List<SubLineaPerformanceModel>();

            if ( modo == "TRIMESTRAL" )
            {
                resultado.Add(CrearTarjeta("Q1 (Ene-Mar)", datos, d => d.Enero + d.Febrero + d.Marzo));
                resultado.Add(CrearTarjeta("Q2 (Abr-Jun)", datos, d => d.Abril + d.Mayo + d.Junio));
                resultado.Add(CrearTarjeta("Q3 (Jul-Sep)", datos, d => d.Julio + d.Agosto + d.Septiembre));
                resultado.Add(CrearTarjeta("Q4 (Oct-Dic)", datos, d => d.Octubre + d.Noviembre + d.Diciembre));
            }
            else if ( modo == "SEMESTRAL" )
            {
                resultado.Add(CrearTarjeta("Semestre 1", datos, d => d.Enero + d.Febrero + d.Marzo + d.Abril + d.Mayo + d.Junio));
                resultado.Add(CrearTarjeta("Semestre 2", datos, d => d.Julio + d.Agosto + d.Septiembre + d.Octubre + d.Noviembre + d.Diciembre));
            }

            return resultado;
        }
        private SubLineaPerformanceModel CrearTarjeta(string titulo, List<ClienteRankingModel> datos, Func<ClienteRankingModel, decimal> selectorVenta)
        {
            decimal totalPeriodo = datos.Sum(selectorVenta);
            var topCliente = datos.OrderByDescending(selectorVenta).FirstOrDefault();

            return new SubLineaPerformanceModel
            {
                Nombre = titulo,
                VentaTotal = totalPeriodo, 
                LitrosTotales = 0,

                Crecimiento = 0,
                EsPositivo = true,
                TopProductoNombre = topCliente != null ? topCliente.Nombre : "N/A",
                TopProductoVenta = topCliente != null ? selectorVenta(topCliente) : 0,

                Bloques = new List<PeriodoBloque>()
            };
        }
    }
}