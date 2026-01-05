using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media; // Para Brushes

using WPF_PAR.Core;
using WPF_PAR.MVVM.Models;
using WPF_PAR.Services;
using WPF_PAR.Services.Interfaces;

namespace WPF_PAR.MVVM.ViewModels
{
    public class DashboardViewModel : ObservableObject
    {
        private readonly ReportesService _reportesService;
        private readonly CatalogoService _catalogoService;
        private readonly ChartService _chartService;
        private readonly IDialogService _dialogService;

        public FilterService Filters { get; }

        // ---------------------------------------------------------
        // 1. SELECTOR DE PERIODOS (MODO HÍBRIDO)
        // ---------------------------------------------------------
        public ObservableCollection<string> ListaPeriodos { get; set; }

        private string _periodoSeleccionado;
        public string PeriodoSeleccionado
        {
            get => _periodoSeleccionado;
            set
            {
                if ( _periodoSeleccionado != value )
                {
                    _periodoSeleccionado = value;
                    OnPropertyChanged();
                    // Al cambiar el combo, calculamos fechas y recargamos
                    AplicarFiltroPeriodo(value);
                }
            }
        }

        // ---------------------------------------------------------
        // 2. KPIs CON COMPARATIVA
        // ---------------------------------------------------------
        private decimal _granTotalVenta;
        public decimal GranTotalVenta { get => _granTotalVenta; set { _granTotalVenta = value; OnPropertyChanged(); OnPropertyChanged(nameof(TieneDatos)); } }

        private double _granTotalLitros;
        public double GranTotalLitros { get => _granTotalLitros; set { _granTotalLitros = value; OnPropertyChanged(); } }

        // Propiedades para el indicador de Crecimiento vs Año Anterior
        private decimal _variacionDinero;
        public decimal VariacionDinero { get => _variacionDinero; set { _variacionDinero = value; OnPropertyChanged(); } }

        private double _variacionPorcentaje;
        public double VariacionPorcentaje { get => _variacionPorcentaje; set { _variacionPorcentaje = value; OnPropertyChanged(); } }

        private bool _esCrecimientoPositivo;
        public bool EsCrecimientoPositivo { get => _esCrecimientoPositivo; set { _esCrecimientoPositivo = value; OnPropertyChanged(); } }

        public bool TieneDatos => GranTotalVenta > 0;

        // ---------------------------------------------------------
        // 3. DATOS VISUALES
        // ---------------------------------------------------------
        private ISeries[] _seriesFamilias;
        public ISeries[] SeriesFamilias { get => _seriesFamilias; set { _seriesFamilias = value; OnPropertyChanged(); } }

        private ObservableCollection<ClienteResumenItem> _topClientesList;
        public ObservableCollection<ClienteResumenItem> TopClientesList
        {
            get => _topClientesList;
            set { _topClientesList = value; OnPropertyChanged(); }
        }

        private bool _isLoading;
        public bool IsLoading { get => _isLoading; set { _isLoading = value; OnPropertyChanged(); } }

        public RelayCommand ActualizarCommand { get; set; }

        public DashboardViewModel(
            ReportesService reportesService,
            CatalogoService catalogoService,
            ChartService chartService,
            FilterService filterService,
            IDialogService dialogService)
        {
            _reportesService = reportesService;
            _catalogoService = catalogoService;
            _chartService = chartService;
            Filters = filterService;
            _dialogService = dialogService;

            // Inicializar Listas
            TopClientesList = new ObservableCollection<ClienteResumenItem>();
            SeriesFamilias = new ISeries[0];

            // Opciones del Modo Híbrido
            ListaPeriodos = new ObservableCollection<string>
            {
                "Hoy",
                "Ayer",
                "Esta Semana",
                "Este Mes",
                "Mes Anterior",
                "Año en Curso"
            };

            ActualizarCommand = new RelayCommand(o => CargarDatos());

            // Selección inicial (dispara la carga)
            PeriodoSeleccionado = "Este Mes";
        }

        private void AplicarFiltroPeriodo(string periodo)
        {
            DateTime hoy = DateTime.Today;

            switch ( periodo )
            {
                case "Hoy":
                    Filters.FechaInicio = hoy;
                    Filters.FechaFin = hoy;
                    break;
                case "Ayer":
                    Filters.FechaInicio = hoy.AddDays(-1);
                    Filters.FechaFin = hoy.AddDays(-1);
                    break;
                case "Esta Semana":
                    // Lunes de esta semana
                    int delta = DayOfWeek.Monday - hoy.DayOfWeek;
                    if ( delta > 0 ) delta -= 7;
                    Filters.FechaInicio = hoy.AddDays(delta);
                    Filters.FechaFin = hoy;
                    break;
                case "Este Mes":
                    Filters.FechaInicio = new DateTime(hoy.Year, hoy.Month, 1);
                    Filters.FechaFin = hoy;
                    break;
                case "Mes Anterior":
                    var mesAnterior = hoy.AddMonths(-1);
                    Filters.FechaInicio = new DateTime(mesAnterior.Year, mesAnterior.Month, 1);
                    Filters.FechaFin = new DateTime(mesAnterior.Year, mesAnterior.Month, DateTime.DaysInMonth(mesAnterior.Year, mesAnterior.Month));
                    break;
                case "Año en Curso":
                    Filters.FechaInicio = new DateTime(hoy.Year, 1, 1);
                    Filters.FechaFin = hoy;
                    break;
            }

            // Recargamos datos automáticamente
            CargarDatos();
        }

        public async void CargarDatos()
        {
            if ( IsLoading ) return;
            IsLoading = true;

            try
            {
                // FECHAS ACTUALES
                var inicio = Filters.FechaInicio;
                var fin = Filters.FechaFin;

                // FECHAS AÑO ANTERIOR (Para comparar peras con peras)
                var inicioPasado = inicio.AddYears(-1);
                var finPasado = fin.AddYears(-1);

                // --- 1. EJECUTAMOS DOS CONSULTAS EN PARALELO ---
                var taskActual = _reportesService.ObtenerVentasBrutasRango(Filters.SucursalId, inicio, fin);
                var taskPasado = _reportesService.ObtenerVentasBrutasRango(Filters.SucursalId, inicioPasado, finPasado);

                await Task.WhenAll(taskActual, taskPasado);

                var ventasActuales = taskActual.Result;
                var ventasPasadas = taskPasado.Result;

                // --- 2. PROCESAMIENTO ---
                var resultado = await Task.Run(() =>
                {
                    if ( ventasActuales == null || !ventasActuales.Any() )
                        return new DashboardResult { EsVacio = true };

                    // Enriquecer datos actuales
                    foreach ( var v in ventasActuales )
                    {
                        var info = _catalogoService.ObtenerInfo(v.Articulo);
                        v.Familia = info.FamiliaSimple;
                        v.LitrosUnitarios = info.Litros;
                    }

                    // Cálculos Actuales
                    var ventasFiltradas = ventasActuales
                        .Where(x => x.Familia != "FERRETERIA" && x.Familia != "VARIOS")
                        .ToList();

                    decimal totalDinero = ventasFiltradas.Sum(x => x.TotalVenta);
                    double totalLitros = ventasFiltradas.Sum(x => x.LitrosTotales);

                    // Cálculo Pasado (Solo necesitamos el total dinero para comparar)
                    decimal totalDineroPasado = 0;
                    if ( ventasPasadas != null )
                    {
                        totalDineroPasado = ventasPasadas
                            .Where(x => !_catalogoService.EsFerreteria(x.Articulo)) // Filtro rápido
                            .Sum(x => x.TotalVenta);
                    }

                    // Datos para Gráficos
                    var porFamilia = ventasFiltradas
                        .GroupBy(x => x.Familia)
                        .Select(g => new LineaResumenModel
                        { NombreLinea = g.Key, VentaTotal = g.Sum(x => x.TotalVenta) })
                        .OrderByDescending(x => x.VentaTotal).ToList();

                    var porCliente = ventasFiltradas
                        .GroupBy(x => x.Cliente)
                        .Select(g => new ClienteResumenItem
                        {
                            Nombre = string.IsNullOrEmpty(g.Key) ? "Público General" : g.Key,
                            Monto = g.Sum(x => x.TotalVenta),
                            Transacciones = g.Count()
                        })
                        .OrderByDescending(x => x.Monto).Take(10).ToList();

                    for ( int i = 0; i < porCliente.Count; i++ ) porCliente[i].Ranking = i + 1;

                    return new DashboardResult
                    {
                        EsVacio = false,
                        TotalVenta = totalDinero,
                        TotalVentaPasada = totalDineroPasado, // Dato histórico
                        TotalLitros = totalLitros,
                        DatosFamilia = porFamilia,
                        DatosClientes = porCliente
                    };
                });

                // --- 3. ACTUALIZACIÓN UI ---
                if ( resultado.EsVacio )
                {
                    GranTotalVenta = 0;
                    GranTotalLitros = 0;
                    VariacionDinero = 0;
                    VariacionPorcentaje = 0;
                    SeriesFamilias = new ISeries[0];
                    TopClientesList = new ObservableCollection<ClienteResumenItem>();
                }
                else
                {
                    GranTotalVenta = resultado.TotalVenta;
                    GranTotalLitros = resultado.TotalLitros;

                    // Lógica Comparativa (Growth Rate)
                    decimal diferencia = resultado.TotalVenta - resultado.TotalVentaPasada;
                    VariacionDinero = Math.Abs(diferencia);
                    EsCrecimientoPositivo = diferencia >= 0;

                    if ( resultado.TotalVentaPasada > 0 )
                        VariacionPorcentaje = ( double ) ( diferencia / resultado.TotalVentaPasada );
                    else
                        VariacionPorcentaje = 1; // 100% si antes era 0

                    SeriesFamilias = _chartService.GenerarPieChart(resultado.DatosFamilia);
                    TopClientesList = new ObservableCollection<ClienteResumenItem>(resultado.DatosClientes);
                }
            }
            catch ( Exception ex )
            {
                _dialogService.ShowError($"Error: {ex.Message}", "Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private class DashboardResult
        {
            public bool EsVacio { get; set; }
            public decimal TotalVenta { get; set; }
            public decimal TotalVentaPasada { get; set; }
            public double TotalLitros { get; set; }
            public List<LineaResumenModel> DatosFamilia { get; set; }
            public List<ClienteResumenItem> DatosClientes { get; set; }
        }
    }

    public class ClienteResumenItem

    {
        public int Ranking { get; set; }
        public string Nombre { get; set; }
        public decimal Monto { get; set; }
        public int Transacciones { get; set; }

    }
}