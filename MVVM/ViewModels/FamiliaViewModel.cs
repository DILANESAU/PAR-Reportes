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
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

using WPF_PAR.Core;
using WPF_PAR.MVVM.Models;
using WPF_PAR.Services;
using WPF_PAR.Services.Interfaces;

namespace WPF_PAR.MVVM.ViewModels
{
    public class FamiliaViewModel : ObservableObject
    {
        // Servicios inyectados
        private readonly ReportesService _reportesService;
        private readonly CatalogoService _catalogoService;
        private readonly ChartService _chartService;
        private readonly FamiliaLogicService _familiaLogic;
        private readonly IDialogService _dialogService;
        private readonly ISnackbarService _snackbarService;

        public FilterService Filters { get; }

        // Propiedades Visuales
        public ObservableCollection<FamiliaResumenModel> TarjetasFamilias { get; set; }
        public ObservableCollection<FamiliaResumenModel> TarjetasArquitectonica { get; set; }
        public ObservableCollection<FamiliaResumenModel> TarjetasEspecializada { get; set; }

        private ObservableCollection<VentaReporteModel> _detalleVentas;
        public ObservableCollection<VentaReporteModel> DetalleVentas
        {
            get => _detalleVentas;
            set { _detalleVentas = value; OnPropertyChanged(); }
        }

        private string _tituloGraficoPastel = "Distribución por Sub-Línea";
        public string TituloGraficoPastel { get => _tituloGraficoPastel; set { _tituloGraficoPastel = value; OnPropertyChanged(); } }

        private string _tituloGraficoBarras = "Top 5 Productos";
        public string TituloGraficoBarras { get => _tituloGraficoBarras; set { _tituloGraficoBarras = value; OnPropertyChanged(); } }

        private ObservableCollection<SubLineaPerformanceModel> _listaDesglose;
        public ObservableCollection<SubLineaPerformanceModel> ListaDesglose
        {
            get => _listaDesglose;
            set { _listaDesglose = value; OnPropertyChanged(); }
        }

        private ISeries[] _seriesDetalle;
        public ISeries[] SeriesDetalle { get => _seriesDetalle; set { _seriesDetalle = value; OnPropertyChanged(); } }

        private ISeries[] _seriesTendencia;
        public ISeries[] SeriesTendencia { get => _seriesTendencia; set { _seriesTendencia = value; OnPropertyChanged(); } }
        public Axis[] EjeXTendencia { get; set; }
        public Axis[] EjeYTendencia { get; set; }

        public ISeries[] SeriesComportamientoLineas { get; set; }
        public Axis[] EjeXMensual { get; set; }

        private List<VentaReporteModel> _ventasProcesadas;
        private List<VentaReporteModel> _datosAnualesCache;
        private List<VentaReporteModel> _datosFamiliaActual;

        private string _lineaActual = "Todas";

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

        public RelayCommand ActualizarCommand { get; set; }
        public RelayCommand VerDetalleCommand { get; set; }
        public RelayCommand RegresarCommand { get; set; }
        public RelayCommand ExportarExcelCommand { get; set; }
        public RelayCommand CambiarPeriodoGraficoCommand { get; set; }

        public FamiliaViewModel(
            IDialogService dialogService,
            ISnackbarService snackbarService,
            FilterService filterService,
            ChartService chartService,
            FamiliaLogicService familiaLogic,
            ReportesService reportesService,
            CatalogoService catalogoService)
        {
            _dialogService = dialogService;
            _snackbarService = snackbarService;
            Filters = filterService;
            _chartService = chartService;
            _familiaLogic = familiaLogic;
            _reportesService = reportesService;
            _catalogoService = catalogoService;

            // Inicializar Colecciones
            TarjetasFamilias = new ObservableCollection<FamiliaResumenModel>();
            TarjetasArquitectonica = new ObservableCollection<FamiliaResumenModel>();
            TarjetasEspecializada = new ObservableCollection<FamiliaResumenModel>();
            DetalleVentas = new ObservableCollection<VentaReporteModel>();
            ListaDesglose = new ObservableCollection<SubLineaPerformanceModel>();

            _ventasProcesadas = new List<VentaReporteModel>();
            _datosAnualesCache = new List<VentaReporteModel>();

            SeriesDetalle = new ISeries[0];
            SeriesTendencia = new ISeries[0];
            SeriesComportamientoLineas = new ISeries[0];

            EjeXTendencia = new Axis[] { new Axis { IsVisible = false, LabelsPaint = new SolidColorPaint(SKColors.Black) } };
            EjeYTendencia = new Axis[] { new Axis { IsVisible = true, LabelsPaint = new SolidColorPaint(SKColors.Black), TextSize = 12 } };
            EjeXMensual = new Axis[] { new Axis { LabelsPaint = new SolidColorPaint(SKColors.Gray) } };

            ActualizarCommand = new RelayCommand(o => EjecutarReporte());
            RegresarCommand = new RelayCommand(o => VerResumen = true);
            ExportarExcelCommand = new RelayCommand(o => GenerarReporteExcel());

            VerDetalleCommand = new RelayCommand(param =>
            {
                if ( param is string familia ) CargarDetalle(familia);
            });

            CambiarPeriodoGraficoCommand = new RelayCommand(param =>
            {
                if ( param is string periodo ) GenerarDesglosePorPeriodo(periodo);
            });

            CargarPorLinea("Todas");
            EjecutarReporte();
        }
        public void CargarPorLinea(string linea)
        {
            _lineaActual = linea;
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

                var resultadoProcesado = await Task.Run(() =>
                {
                    foreach ( var venta in ventasRaw )
                    {
                        // Nota: _catalogoService debe ser thread-safe (usualmente lo es si solo lee listas estáticas)
                        var info = _catalogoService.ObtenerInfo(venta.Articulo);
                        venta.Familia = info.FamiliaSimple;
                        venta.Linea = info.Linea;
                        venta.Descripcion = info.Descripcion;
                        venta.LitrosUnitarios = info.Litros;
                    }

                    // B. Calcular Totales y Tarjetas
                    var (arqui, espe) = _familiaLogic.CalcularResumenGlobal(ventasRaw);

                    var totalVenta = ventasRaw.Sum(x => x.TotalVenta);
                    var totalLitros = ventasRaw.Sum(x => x.LitrosTotales);

                    // C. Filtrar "Basura" (Ferretería, etc)
                    var ventasLimpias = ventasRaw
                        .Where(x => x.Familia != "FERRETERIA" && !x.Linea.Contains("FERRETERIA", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    // Devolvemos todo en un paquete
                    return new
                    {
                        Ventas = ventasLimpias,
                        Arqui = arqui,
                        Espe = espe,
                        TotalVenta = totalVenta,
                        TotalLitros = totalLitros
                    };
                });

                // PASO 3: Actualizar UI (Hilo Principal)
                _ventasProcesadas = resultadoProcesado.Ventas;

                TarjetasArquitectonica = new ObservableCollection<FamiliaResumenModel>(resultadoProcesado.Arqui);
                TarjetasEspecializada = new ObservableCollection<FamiliaResumenModel>(resultadoProcesado.Espe);

                GranTotalVenta = resultadoProcesado.TotalVenta;
                GranTotalLitros = resultadoProcesado.TotalLitros;

                // Actualizar la lista visible según el filtro seleccionado
                if ( _lineaActual == "Arquitectonica" ) TarjetasFamilias = new ObservableCollection<FamiliaResumenModel>(resultadoProcesado.Arqui);
                else if ( _lineaActual == "Especializada" ) TarjetasFamilias = new ObservableCollection<FamiliaResumenModel>(resultadoProcesado.Espe);
                else TarjetasFamilias = new ObservableCollection<FamiliaResumenModel>(resultadoProcesado.Arqui.Concat(resultadoProcesado.Espe));

                IsLoading = false;

                // PASO 4: Carga de fondo (Histórico) - Esto ya es async, no bloquea
                _datosAnualesCache = await _reportesService.ObtenerHistoricoAnualPorArticulo(
                    DateTime.Now.Year.ToString(),
                    Filters.SucursalId.ToString()
                );

                // Enriquecimiento del histórico en Task.Run para no bloquear si son muchos datos
                await Task.Run(() =>
                {
                    foreach ( var item in _datosAnualesCache )
                    {
                        var info = _catalogoService.ObtenerInfo(item.Articulo);
                        item.Linea = info.Linea;
                        item.Familia = info.FamiliaSimple;
                    }
                });

                if ( VerDetalle ) GenerarDesglosePorPeriodo("ANUAL");
            }
            catch ( Exception ex )
            {
                IsLoading = false;
                _dialogService.ShowMessage("Error", $"Error al cargar reporte: {ex.Message}");
            }
        }

        // Este método ya se integró en EjecutarReporte, pero lo mantenemos para refrescos manuales si existen
        private void GenerarResumenVisual()
        {
            // Ya está manejado en EjecutarReporte, pero si cambias de "Arquitectónica" a "Especializada"
            // usando botones, usarás CargarPorLinea que llama a esto.
            // Simplemente reutilizamos las listas ya calculadas en memoria.
            if ( _lineaActual == "Arquitectonica" ) TarjetasFamilias = new ObservableCollection<FamiliaResumenModel>(TarjetasArquitectonica);
            else if ( _lineaActual == "Especializada" ) TarjetasFamilias = new ObservableCollection<FamiliaResumenModel>(TarjetasEspecializada);
            else TarjetasFamilias = new ObservableCollection<FamiliaResumenModel>(TarjetasArquitectonica.Concat(TarjetasEspecializada));
        }

        // ... (CargarDetalle queda igual) ...
        private void CargarDetalle(string familia)
        {
            TituloDetalle = familia;
            _datosFamiliaActual = _ventasProcesadas.Where(x => x.Familia == familia).ToList();

            SubLineasDisponibles.Clear();
            SubLineasDisponibles.Add("TODAS");
            var lineas = _datosFamiliaActual.Select(x => x.Linea).Distinct().OrderBy(x => x);
            foreach ( var l in lineas ) SubLineasDisponibles.Add(l);

            SubLineaSeleccionada = "TODAS";
            GenerarDesglosePorPeriodo("ANUAL");
            VerResumen = false;
        }

        // --- CORRECCIÓN 3: Optimización de Gráficos ---
        private async void ActualizarGraficosPorSubLinea()
        {
            if ( _datosFamiliaActual == null ) return;

            // Guardamos estado UI antes de ir al hilo secundario
            string filtro = SubLineaSeleccionada;
            bool porLitros = VerPorLitros;
            var datosOrigen = _datosFamiliaActual; // Referencia segura a la lista

            var resultadoGraficos = await Task.Run(() =>
            {
                // Lógica de filtrado
                var datosBase = datosOrigen
                   .Where(x => !x.Linea.Contains("FERRETERIA", StringComparison.OrdinalIgnoreCase))
                   .ToList();

                bool esVistaGlobal = ( filtro == "TODAS" || string.IsNullOrEmpty(filtro) );

                var datosFiltrados = esVistaGlobal
                    ? datosBase.ToList()
                    : datosBase.Where(x => ( x.Linea ?? "" ).Equals(filtro, StringComparison.OrdinalIgnoreCase)).ToList();

                // Preparar datos para gráficos (ChartService devuelve objetos ligeros o configs)
                // OJO: LiveCharts crea objetos UI, pero suelen ser seguros de crear en background si no se adjuntan todavía
                // Si LiveCharts da error de threading, mueve solo la generación de Series fuera del Task.Run.
                // Generalmente LiveCharts v2 es amigable con esto.

                // Calculamos datos puros
                var gruposPie = datosFiltrados
                    .GroupBy(x => esVistaGlobal ? x.Linea : x.Descripcion)
                    .Select(g => new LineaResumenModel
                    {
                        NombreLinea = g.Key,
                        VentaTotal = g.Sum(x => x.TotalVenta)
                    })
                    .OrderByDescending(x => x.VentaTotal)
                    .Take(5)
                    .ToList();

                // Para las barras
                List<VentaReporteModel> datosBarras;
                if ( esVistaGlobal )
                {
                    datosBarras = datosFiltrados; // Se procesan dentro de GenerarTopProductos
                }
                else
                {
                    // Lógica especial de clientes
                    datosBarras = datosFiltrados.Select(x => new VentaReporteModel
                    {
                        Descripcion = x.Cliente,
                        Cantidad = x.Cantidad,
                        LitrosUnitarios = x.LitrosUnitarios,
                        PrecioUnitario = x.PrecioUnitario,
                        Descuento = x.Descuento,
                        TotalVenta = x.TotalVenta // Importante copiar esto
                    }).ToList();
                }

                return new
                {
                    EsGlobal = esVistaGlobal,
                    DatosTabla = datosFiltrados.OrderByDescending(x => x.TotalVenta).ToList(),
                    GruposPie = gruposPie,
                    DatosBarras = datosBarras
                };
            });

            // Volvemos al UI Thread para crear los Visuales
            // Esto es muy rápido, lo lento era filtrar y agrupar las listas
            TituloGraficoPastel = resultadoGraficos.EsGlobal ? "Distribución por Sub-Línea" : "Top Productos de esta Línea";
            TituloGraficoBarras = resultadoGraficos.EsGlobal ? "Top 5 Productos Globales" : "Top Clientes de esta Línea";

            DetalleVentas = new ObservableCollection<VentaReporteModel>(resultadoGraficos.DatosTabla);

            SeriesDetalle = _chartService.GenerarPieChart(resultadoGraficos.GruposPie);

            var resTop = _chartService.GenerarTopProductos(resultadoGraficos.DatosBarras, porLitros);
            SeriesTendencia = resTop.Series;
            EjeXTendencia = resTop.EjesX;
            EjeYTendencia = resTop.EjesY;
        }

        // ... (GenerarDesglosePorPeriodo y otros métodos auxiliares se quedan igual o se pueden optimizar similarmente) ...

        private void GenerarDesglosePorPeriodo(string periodo)
        {
            if ( _datosAnualesCache == null ) return;

            // Esto es rápido, pero si notaras lentitud, podrías envolverlo en Task.Run también
            var datosFamiliaAnuales = _datosAnualesCache
                .Where(x => x.Familia == TituloDetalle)
                .ToList();

            var resultadoGrafico = _chartService.GenerarTendenciaLineas(datosFamiliaAnuales, periodo);
            SeriesComportamientoLineas = resultadoGrafico.Series;
            EjeXMensual = resultadoGrafico.EjesX;
            OnPropertyChanged(nameof(SeriesComportamientoLineas));
            OnPropertyChanged(nameof(EjeXMensual));

            string modoCalculo = periodo == "ANUAL" ? "TRIMESTRAL" : periodo;
            var listaNueva = _familiaLogic.CalcularDesgloseClientes(datosFamiliaAnuales, modoCalculo);
            ListaDesglose = new ObservableCollection<SubLineaPerformanceModel>(listaNueva);
        }

        // ... (GenerarReporteExcel y FiltrarTabla sin cambios) ...
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