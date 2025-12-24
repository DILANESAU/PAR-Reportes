using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;

using SkiaSharp;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Data;

using WPF_PAR.Core;
using WPF_PAR.MVVM.Models;
using WPF_PAR.Services;
using WPF_PAR.Services.Interfaces;

namespace WPF_PAR.MVVM.ViewModels
{
    public class FamiliaViewModel : ObservableObject
    {
        private readonly ReportesService _reportesService;
        private readonly CatalogoService _catalogoService;
        private readonly ChartService _chartService; 
        private readonly FamiliaLogicService _familiaLogic; 
        private readonly IDialogService _dialogService;
        private readonly ISnackbarService _snackbarService;
        public FilterService Filters { get; }

        public ObservableCollection<FamiliaResumenModel> TarjetasFamilias { get; set; }
        public ObservableCollection<FamiliaResumenModel> TarjetasArquitectonica { get; set; }
        public ObservableCollection<FamiliaResumenModel> TarjetasEspecializada { get; set; }

        private ObservableCollection<VentaReporteModel> _detalleVentas;
        public ObservableCollection<VentaReporteModel> DetalleVentas
        {
            get => _detalleVentas;
            set { _detalleVentas = value; OnPropertyChanged(); }
        }

        private ObservableCollection<LineaResumenModel> _resumenLineas;
        public ObservableCollection<LineaResumenModel> ResumenLineas
        {
            get => _resumenLineas;
            set { _resumenLineas = value; OnPropertyChanged(); }
        }

        private ISeries[] _seriesDetalle;
        public ISeries[] SeriesDetalle { get => _seriesDetalle; set { _seriesDetalle = value; OnPropertyChanged(); } }

        private ISeries[] _seriesTendencia;
        public ISeries[] SeriesTendencia { get => _seriesTendencia; set { _seriesTendencia = value; OnPropertyChanged(); } }
        private Axis[] _ejeXTendencia;
        public Axis[] EjeXTendencia { get => _ejeXTendencia; set { _ejeXTendencia = value; OnPropertyChanged(); } }
        private Axis[] _ejeYTendencia;
        public Axis[] EjeYTendencia { get => _ejeYTendencia; set { _ejeYTendencia = value; OnPropertyChanged(); } }

        private ISeries[] _seriesComportamientoLineas;
        public ISeries[] SeriesComportamientoLineas { get => _seriesComportamientoLineas; set { _seriesComportamientoLineas = value; OnPropertyChanged(); } }
        private Axis[] _ejeXMensual;
        public Axis[] EjeXMensual { get => _ejeXMensual; set { _ejeXMensual = value; OnPropertyChanged(); } }

        private List<VentaReporteModel> _ventasProcesadas;
        private List<VentaReporteModel> _datosAnualesCache;
        private List<VentaReporteModel> _datosFamiliaActual;

        private decimal _granTotalVenta;
        public decimal GranTotalVenta { get => _granTotalVenta; set { _granTotalVenta = value; OnPropertyChanged(); } }

        private double _granTotalLitros;
        public double GranTotalLitros { get => _granTotalLitros; set { _granTotalLitros = value; OnPropertyChanged(); } }

        private bool _verPorLitros;
        public bool VerPorLitros
        {
            get => _verPorLitros;
            set
            {
                _verPorLitros = value;
                OnPropertyChanged();
                ActualizarGraficosPorSubLinea();
            }
        }

        private bool _isLoading;
        public bool IsLoading { get => _isLoading; set { _isLoading = value; OnPropertyChanged(); } }

        private bool _verResumen = true;
        public bool VerResumen
        {
            get => _verResumen;
            set { _verResumen = value; OnPropertyChanged(); OnPropertyChanged(nameof(VerDetalle)); }
        }
        public bool VerDetalle => !VerResumen;

        private string _tituloDetalle;
        public string TituloDetalle { get => _tituloDetalle; set { _tituloDetalle = value; OnPropertyChanged(); } }

        private string _textoBusqueda;
        public string TextoBusqueda
        {
            get => _textoBusqueda;
            set { _textoBusqueda = value; OnPropertyChanged(); FiltrarTabla(); }
        }

        public ObservableCollection<string> SubLineasDisponibles { get; set; } = new ObservableCollection<string>();
        private string _subLineaSeleccionada;
        public string SubLineaSeleccionada
        {
            get => _subLineaSeleccionada;
            set
            {
                _subLineaSeleccionada = value;
                OnPropertyChanged();
                if ( !string.IsNullOrEmpty(value) ) ActualizarGraficosPorSubLinea();
            }
        }
        private string _lineaActual = "Todas";

        public RelayCommand ActualizarCommand { get; set; }
        public RelayCommand VerDetalleCommand { get; set; }
        public RelayCommand RegresarCommand { get; set; }
        public RelayCommand ExportarExcelCommand { get; set; }
        public RelayCommand OrdenarVentaCommand { get; set; }
        public RelayCommand OrdenarNombreCommand { get; set; }
        public RelayCommand CambiarPeriodoGraficoCommand { get; set; }

        public FamiliaViewModel(
            IDialogService dialogService,
            ISnackbarService snackbarService,
            BusinessLogicService businessLogic,
            FilterService filterService,
            ChartService chartService,           
            FamiliaLogicService familiaLogic)   
        {
            _dialogService = dialogService;
            _snackbarService = snackbarService;
            Filters = filterService;
            _chartService = chartService;
            _familiaLogic = familiaLogic;

            _reportesService = new ReportesService();
            _catalogoService = new CatalogoService(businessLogic);

            TarjetasFamilias = new ObservableCollection<FamiliaResumenModel>();
            TarjetasArquitectonica = new ObservableCollection<FamiliaResumenModel>();
            TarjetasEspecializada = new ObservableCollection<FamiliaResumenModel>();
            DetalleVentas = new ObservableCollection<VentaReporteModel>();
            ResumenLineas = new ObservableCollection<LineaResumenModel>();

            _ventasProcesadas = new List<VentaReporteModel>();
            _datosAnualesCache = new List<VentaReporteModel>();

            SeriesDetalle = new ISeries[0];
            SeriesTendencia = new ISeries[0];
            SeriesComportamientoLineas = new ISeries[0];

            EjeXTendencia = new Axis[] { new Axis { IsVisible = false, LabelsPaint = new SolidColorPaint(SKColors.Black) } };
            EjeYTendencia = new Axis[] { new Axis { IsVisible = true, LabelsPaint = new SolidColorPaint(SKColors.Black), TextSize = 12 } };
            EjeXMensual = new Axis[] { new Axis { LabelsPaint = new SolidColorPaint(SKColors.Gray) } };

            ActualizarCommand = new RelayCommand(o => EjecutarReporte());
            OrdenarVentaCommand = new RelayCommand(o => AplicarOrden("VENTA"));
            OrdenarNombreCommand = new RelayCommand(o => AplicarOrden("NOMBRE"));
            RegresarCommand = new RelayCommand(o => VerResumen = true);
            ExportarExcelCommand = new RelayCommand(o => GenerarReporteExcel());

            VerDetalleCommand = new RelayCommand(param =>
            {
                if ( param is string familia ) CargarDetalle(familia);
            });

            CambiarPeriodoGraficoCommand = new RelayCommand(param =>
            {
                if ( param is string periodo ) GenerarGraficoPorPeriodo(periodo);
            });

            var tarjetasVacias = _familiaLogic.ObtenerTarjetasVacias("Todas");
            TarjetasFamilias = new ObservableCollection<FamiliaResumenModel>(tarjetasVacias);

            EjecutarReporte();
        }
        public void CargarPorLinea(string linea)
        {
            _lineaActual = linea;

            // Si ya tenemos datos cargados, solo refrescamos la visualización
            if ( _ventasProcesadas != null && _ventasProcesadas.Any() )
            {
                GenerarResumenVisual();
            }
            else
            {
                var vacias = _familiaLogic.ObtenerTarjetasVacias(_lineaActual);
                TarjetasFamilias = new ObservableCollection<FamiliaResumenModel>(vacias);

            }

            VerResumen = true;
        }
        private async void EjecutarReporte()
        {
            IsLoading = true;
            try
            {
                var ventasRaw = await _reportesService.ObtenerVentasBrutasRango(Filters.SucursalId, Filters.FechaInicio, Filters.FechaFin);
                _ventasProcesadas = ventasRaw;

                foreach ( var venta in _ventasProcesadas )
                {
                    var info = _catalogoService.ObtenerInfo(venta.Articulo);
                    venta.Familia = info.FamiliaSimple;
                    venta.Linea = info.Linea;
                    venta.Descripcion = info.Descripcion;
                    venta.LitrosUnitarios = info.Litros;
                }

                GenerarResumenVisual();

                IsLoading = false;

                _datosAnualesCache = await _reportesService.ObtenerHistoricoAnualPorArticulo(
                    DateTime.Now.Year.ToString(),
                    Filters.SucursalId.ToString()
                );

                foreach ( var item in _datosAnualesCache )
                {
                    var info = _catalogoService.ObtenerInfo(item.Articulo);
                    item.Linea = info.Linea;
                    item.Familia = info.FamiliaSimple;
                }

                if ( VerDetalle ) GenerarGraficoPorPeriodo("ANUAL");

            }
            catch ( Exception ex )
            {
                IsLoading = false;
                _dialogService.ShowMessage("Error", $"Error al cargar reporte: {ex.Message}");
            }
        }

        private void GenerarResumenVisual()
        {
            var (arqui, espe) = _familiaLogic.CalcularResumenGlobal(_ventasProcesadas);

            TarjetasArquitectonica = new ObservableCollection<FamiliaResumenModel>(arqui);
            TarjetasEspecializada = new ObservableCollection<FamiliaResumenModel>(espe);

            GranTotalVenta = _ventasProcesadas.Sum(x => x.TotalVenta);
            GranTotalLitros = _ventasProcesadas.Sum(x => x.LitrosTotales);

            var listaParaMostrar = new List<FamiliaResumenModel>();

            if ( _lineaActual == "Arquitectonica" )
            {
                listaParaMostrar.AddRange(arqui);
            }
            else if ( _lineaActual == "Especializada" )
            {
                listaParaMostrar.AddRange(espe);
            }
            else 
            {
                listaParaMostrar.AddRange(arqui);
                listaParaMostrar.AddRange(espe);
            }

            TarjetasFamilias = new ObservableCollection<FamiliaResumenModel>(listaParaMostrar);
        }

        private void CargarDetalle(string familia)
        {
            TituloDetalle = familia;
            _datosFamiliaActual = _ventasProcesadas.Where(x => x.Familia == familia).ToList();

            SubLineasDisponibles.Clear();
            SubLineasDisponibles.Add("TODAS");

            var lineas = _datosFamiliaActual
                            .Select(x => x.Linea)
                            .Distinct()
                            .OrderBy(x => x)
                            .ToList();

            foreach ( var l in lineas ) SubLineasDisponibles.Add(l);

            SubLineaSeleccionada = "TODAS";

            GenerarGraficoPorPeriodo("ANUAL");

            VerResumen = false;
        }

        private void ActualizarGraficosPorSubLinea()
        {
            if ( _datosFamiliaActual == null ) return;

            string filtro = SubLineaSeleccionada;
            var datosFiltrados = ( filtro == "TODAS" || string.IsNullOrEmpty(filtro) )
                ? _datosFamiliaActual.ToList()
                : _datosFamiliaActual.Where(x => ( x.Linea ?? "" ).Equals(filtro, StringComparison.OrdinalIgnoreCase)).ToList();

            DetalleVentas = new ObservableCollection<VentaReporteModel>(
                datosFiltrados.OrderByDescending(x => x.TotalVenta)
            );

            var gruposLinea = datosFiltrados
                .GroupBy(x => x.Linea)
                .Select(g => new LineaResumenModel
                {
                    NombreLinea = g.Key,
                    VentaTotal = g.Sum(x => x.TotalVenta),
                    LitrosTotales = g.Sum(x => x.LitrosTotales),
                    ProductoTop = g.GroupBy(p => p.Descripcion)
                                   .OrderByDescending(gp => gp.Sum(v => v.LitrosTotales))
                                   .FirstOrDefault()?.Key ?? "Sin datos"
                })
                .OrderByDescending(x => x.VentaTotal)
                .ToList();

            ResumenLineas = new ObservableCollection<LineaResumenModel>(gruposLinea);

            SeriesDetalle = _chartService.GenerarPieChart(gruposLinea.Take(5).ToList());

            var resultadoTop = _chartService.GenerarTopProductos(datosFiltrados, VerPorLitros);
            SeriesTendencia = resultadoTop.Series;
            EjeXTendencia = resultadoTop.EjesX;
            EjeYTendencia = resultadoTop.EjesY;
        }

        private void GenerarGraficoPorPeriodo(string periodo)
        {
            if ( _datosAnualesCache == null ) return;

            var datosFamiliaAnuales = _datosAnualesCache
                .Where(x => x.Familia == TituloDetalle)
                .ToList();

            var resultado = _chartService.GenerarTendenciaLineas(datosFamiliaAnuales, periodo);

            SeriesComportamientoLineas = resultado.Series;
            EjeXMensual = resultado.EjesX;
        }
        private void AplicarOrden(string criterio)
        {
            var listaOrdenada = _familiaLogic.OrdenarTarjetas(TarjetasFamilias, criterio);
            TarjetasFamilias = new ObservableCollection<FamiliaResumenModel>(listaOrdenada);
        }

        private void GenerarReporteExcel()
        {
            if ( DetalleVentas == null || DetalleVentas.Count == 0 )
            {
                _dialogService.ShowMessage("No hay datos para exportar.", "Aviso");
                return;
            }

            string nombreArchivo = $"Reporte_{Filters.SucursalId}_{TituloDetalle?.Replace(":", "") ?? "General"}.csv";
            string ruta = _dialogService.ShowSaveFileDialog("Archivo CSV (*.csv)|*.csv", nombreArchivo);

            if ( !string.IsNullOrEmpty(ruta) )
            {
                try
                {
                    string contenido = _familiaLogic.GenerarContenidoCSV(DetalleVentas);
                    File.WriteAllText(ruta, contenido, Encoding.UTF8);
                    _snackbarService.Show("✅ Reporte exportado correctamente");
                }
                catch ( Exception ex )
                {
                    _dialogService.ShowError($"Error al exportar: {ex.Message}", "Error");
                }
            }
        }

        private void FiltrarTabla()
        {
            if ( DetalleVentas == null ) return;
            ICollectionView view = CollectionViewSource.GetDefaultView(DetalleVentas);
            if ( string.IsNullOrWhiteSpace(TextoBusqueda) )
            {
                view.Filter = null;
            }
            else
            {
                view.Filter = (obj) =>
                {
                    var v = obj as VentaReporteModel;
                    if ( v == null ) return false;
                    string t = TextoBusqueda.ToUpper();
                    return ( v.Cliente?.ToUpper().Contains(t) ?? false ) ||
                           ( v.Descripcion?.ToUpper().Contains(t) ?? false ) ||
                           ( v.Linea?.ToUpper().Contains(t) ?? false );
                };
            }
        }
    }
}