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
        // ---------------------------------------------------------
        // SERVICIOS
        // ---------------------------------------------------------
        private readonly ReportesService _reportesService;
        private readonly CatalogoService _catalogoService;
        private readonly ChartService _chartService;
        private readonly FamiliaLogicService _familiaLogic;
        private readonly IDialogService _dialogService;
        private readonly ISnackbarService _snackbarService;

        public FilterService Filters { get; }

        // ---------------------------------------------------------
        // COLECCIONES DE DATOS
        // ---------------------------------------------------------

        // Tarjetas Resumen (Vista Principal)
        public ObservableCollection<FamiliaResumenModel> TarjetasFamilias { get; set; }
        public ObservableCollection<FamiliaResumenModel> TarjetasArquitectonica { get; set; }
        public ObservableCollection<FamiliaResumenModel> TarjetasEspecializada { get; set; }

        // Tabla Detallada
        private ObservableCollection<VentaReporteModel> _detalleVentas;
        public ObservableCollection<VentaReporteModel> DetalleVentas
        {
            get => _detalleVentas;
            set { _detalleVentas = value; OnPropertyChanged(); }
        }
        // Títulos dinámicos para los gráficos
        private string _tituloGraficoPastel = "Distribución por Sub-Línea";
        public string TituloGraficoPastel { get => _tituloGraficoPastel; set { _tituloGraficoPastel = value; OnPropertyChanged(); } }

        private string _tituloGraficoBarras = "Top 5 Productos";
        public string TituloGraficoBarras { get => _tituloGraficoBarras; set { _tituloGraficoBarras = value; OnPropertyChanged(); } }

        // ¡NUEVO! Lista para las Tarjetas Comparativas (Q1, Q2, Semestre, etc.)
        private ObservableCollection<SubLineaPerformanceModel> _listaDesglose;
        public ObservableCollection<SubLineaPerformanceModel> ListaDesglose
        {
            get => _listaDesglose;
            set { _listaDesglose = value; OnPropertyChanged(); }
        }

        // ---------------------------------------------------------
        // PROPIEDADES GRÁFICAS (LiveCharts)
        // ---------------------------------------------------------

        // Gráfico Pastel (Distribución)
        private ISeries[] _seriesDetalle;
        public ISeries[] SeriesDetalle { get => _seriesDetalle; set { _seriesDetalle = value; OnPropertyChanged(); } }

        // Gráfico Barras (Ranking Top 5)
        private ISeries[] _seriesTendencia;
        public ISeries[] SeriesTendencia { get => _seriesTendencia; set { _seriesTendencia = value; OnPropertyChanged(); } }
        public Axis[] EjeXTendencia { get; set; }
        public Axis[] EjeYTendencia { get; set; }

        // Mantenemos estas propiedades por si acaso decides reusar el gráfico de líneas en otra pestaña,
        // aunque ahora la vista principal usará la ListaDesglose.
        public ISeries[] SeriesComportamientoLineas { get; set; }
        public Axis[] EjeXMensual { get; set; }

        // ---------------------------------------------------------
        // ESTADO Y FILTROS
        // ---------------------------------------------------------
        private List<VentaReporteModel> _ventasProcesadas;     // Datos del rango seleccionado
        private List<VentaReporteModel> _datosAnualesCache;    // Datos de todo el año (para tendencias)
        private List<VentaReporteModel> _datosFamiliaActual;   // Datos filtrados por la familia seleccionada

        private string _lineaActual = "Todas"; // Controla el menú lateral (Arquitectónica/Especializada)

        // Totales Globales
        private decimal _granTotalVenta;
        public decimal GranTotalVenta { get => _granTotalVenta; set { _granTotalVenta = value; OnPropertyChanged(); } }

        private double _granTotalLitros;
        public double GranTotalLitros { get => _granTotalLitros; set { _granTotalLitros = value; OnPropertyChanged(); } }

        // Switch Venta vs Litros
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

        // Navegación (Resumen vs Detalle)
        private bool _verResumen = true;
        public bool VerResumen
        {
            get => _verResumen;
            set { _verResumen = value; OnPropertyChanged(); OnPropertyChanged(nameof(VerDetalle)); }
        }
        public bool VerDetalle => !VerResumen;

        private string _tituloDetalle;
        public string TituloDetalle { get => _tituloDetalle; set { _tituloDetalle = value; OnPropertyChanged(); } }

        // Filtros de Tabla
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

        // ---------------------------------------------------------
        // COMANDOS
        // ---------------------------------------------------------
        public RelayCommand ActualizarCommand { get; set; }
        public RelayCommand VerDetalleCommand { get; set; }
        public RelayCommand RegresarCommand { get; set; }
        public RelayCommand ExportarExcelCommand { get; set; }
        public RelayCommand CambiarPeriodoGraficoCommand { get; set; } // Ahora actualiza las tarjetas comparativas

        // ---------------------------------------------------------
        // CONSTRUCTOR
        // ---------------------------------------------------------
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

            // Inicializar Colecciones
            TarjetasFamilias = new ObservableCollection<FamiliaResumenModel>();
            TarjetasArquitectonica = new ObservableCollection<FamiliaResumenModel>();
            TarjetasEspecializada = new ObservableCollection<FamiliaResumenModel>();
            DetalleVentas = new ObservableCollection<VentaReporteModel>();
            ListaDesglose = new ObservableCollection<SubLineaPerformanceModel>();

            _ventasProcesadas = new List<VentaReporteModel>();
            _datosAnualesCache = new List<VentaReporteModel>();

            // Inicializar Gráficos Vacíos (Evita Crash de LiveCharts)
            SeriesDetalle = new ISeries[0];
            SeriesTendencia = new ISeries[0];
            SeriesComportamientoLineas = new ISeries[0];

            EjeXTendencia = new Axis[] { new Axis { IsVisible = false, LabelsPaint = new SolidColorPaint(SKColors.Black) } };
            EjeYTendencia = new Axis[] { new Axis { IsVisible = true, LabelsPaint = new SolidColorPaint(SKColors.Black), TextSize = 12 } };
            EjeXMensual = new Axis[] { new Axis { LabelsPaint = new SolidColorPaint(SKColors.Gray) } };

            // Comandos
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

            // Carga inicial (Tarjetas vacías)
            CargarPorLinea("Todas");

            // Cargar datos reales
            EjecutarReporte();
        }

        // ---------------------------------------------------------
        // NAVEGACIÓN LATERAL (Arquitectónica / Especializada)
        // ---------------------------------------------------------
        public void CargarPorLinea(string linea)
        {
            _lineaActual = linea;

            if ( _ventasProcesadas != null && _ventasProcesadas.Any() )
            {
                // Si ya hay datos, solo refrescamos la UI
                GenerarResumenVisual();
            }
            else
            {
                // Si no hay datos, mostramos estructura vacía
                var vacias = _familiaLogic.ObtenerTarjetasVacias(_lineaActual);
                TarjetasFamilias = new ObservableCollection<FamiliaResumenModel>(vacias);
            }
            VerResumen = true;
        }

        // ---------------------------------------------------------
        // LÓGICA DE DATOS
        // ---------------------------------------------------------
        private async void EjecutarReporte()
        {
            IsLoading = true;
            try
            {
                // 1. Obtener Datos Rango
                var ventasRaw = await _reportesService.ObtenerVentasBrutasRango(Filters.SucursalId, Filters.FechaInicio, Filters.FechaFin);
                _ventasProcesadas = ventasRaw;

                // 2. Enriquecer
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

                // 3. Cargar Histórico Anual (Fondo)
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
                _ventasProcesadas = ventasRaw
                        .Where(x => x.Familia != "FERRETERIA" && !x.Linea.Contains("FERRETERIA", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                // Si ya estamos en detalle, refrescar comparativas
                if ( VerDetalle ) GenerarDesglosePorPeriodo("ANUAL");
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

            // Filtro visual según menú lateral
            IEnumerable<FamiliaResumenModel> resultado;
            if ( _lineaActual == "Arquitectonica" ) resultado = arqui;
            else if ( _lineaActual == "Especializada" ) resultado = espe;
            else resultado = arqui.Concat(espe);

            TarjetasFamilias = new ObservableCollection<FamiliaResumenModel>(resultado);
        }

        // ---------------------------------------------------------
        // VISTA DETALLE
        // ---------------------------------------------------------
        private void CargarDetalle(string familia)
        {
            TituloDetalle = familia;
            _datosFamiliaActual = _ventasProcesadas.Where(x => x.Familia == familia).ToList();

            // Llenar Combo Sub-Líneas
            SubLineasDisponibles.Clear();
            SubLineasDisponibles.Add("TODAS");
            var lineas = _datosFamiliaActual.Select(x => x.Linea).Distinct().OrderBy(x => x);
            foreach ( var l in lineas ) SubLineasDisponibles.Add(l);

            SubLineaSeleccionada = "TODAS"; // Esto dispara ActualizarGraficosPorSubLinea

            // Generar Tarjetas Comparativas (Por defecto Anual -> Muestra Trimestres)
            GenerarDesglosePorPeriodo("ANUAL");

            VerResumen = false;
        }

        private void ActualizarGraficosPorSubLinea()
        {
            if ( _datosFamiliaActual == null ) return;

            var datosBase = _datosFamiliaActual
            .Where(x => !x.Linea.Contains("FERRETERIA", StringComparison.OrdinalIgnoreCase))
            .ToList();
            // 1. Filtrar Datos
            string filtro = SubLineaSeleccionada;
            bool esVistaGlobal = ( filtro == "TODAS" || string.IsNullOrEmpty(filtro) );

            var datosFiltrados = esVistaGlobal
                ? datosBase.ToList()
                : datosBase.Where(x => ( x.Linea ?? "" ).Equals(filtro, StringComparison.OrdinalIgnoreCase)).ToList();

            // 2. Actualizar Tabla
            DetalleVentas = new ObservableCollection<VentaReporteModel>(
                datosFiltrados.OrderByDescending(x => x.TotalVenta)
            );

            // ---------------------------------------------------------
            // 3. LOGICA INTELIGENTE DE GRÁFICOS (DRILL-DOWN)
            // ---------------------------------------------------------

            if ( esVistaGlobal )
            {
                // === VISTA GLOBAL (TODAS) ===
                TituloGraficoPastel = "Distribución por Sub-Línea";
                TituloGraficoBarras = "Top 5 Productos Globales";

                // Pastel: Muestra Sub-Líneas (Como estaba antes)
                var gruposLinea = datosFiltrados
                    .GroupBy(x => x.Linea)
                    .Select(g => new LineaResumenModel
                    {
                        NombreLinea = g.Key,
                        VentaTotal = g.Sum(x => x.TotalVenta)
                    })
                    .OrderByDescending(x => x.VentaTotal)
                    .Take(5)
                    .ToList();

                SeriesDetalle = _chartService.GenerarPieChart(gruposLinea);

                // Barras: Muestra Productos
                var resultadoTop = _chartService.GenerarTopProductos(datosFiltrados, VerPorLitros);
                SeriesTendencia = resultadoTop.Series;
                EjeXTendencia = resultadoTop.EjesX;
                EjeYTendencia = resultadoTop.EjesY;
            }
            else
            {
                // === VISTA ESPECÍFICA (SUB-LÍNEA SELECCIONADA) ===
                TituloGraficoPastel = "Top Productos de esta Línea";
                TituloGraficoBarras = "Top Clientes de esta Línea";

                // Pastel: Muestra PRODUCTOS en lugar de líneas (Para que no salga 100% igual)
                var gruposProductos = datosFiltrados
                    .GroupBy(x => x.Descripcion)
                    .Select(g => new LineaResumenModel
                    {
                        NombreLinea = g.Key.Length > 15 ? g.Key.Substring(0, 12) + "..." : g.Key, // Acortar nombre
                        VentaTotal = g.Sum(x => x.TotalVenta)
                    })
                    .OrderByDescending(x => x.VentaTotal)
                    .Take(5) // Top 5 productos de esta línea
                    .ToList();

                SeriesDetalle = _chartService.GenerarPieChart(gruposProductos);

                // Barras: Muestra CLIENTES (Insight nuevo: ¿Quién compra esto?)
                // Truco: Reusamos el servicio GenerarTopProductos pero le pasamos Clientes en vez de Descripciones
                // Creamos una lista temporal "trucada"
                var datosClientes = datosFiltrados.Select(x => new VentaReporteModel
                {
                    Descripcion = x.Cliente, // Ponemos el Cliente en la propiedad Descripción
                    Cantidad = x.LitrosTotales,      // Cantidad = Litros Totales
                    LitrosUnitarios = 1
                }).ToList();

                var resultadoClientes = _chartService.GenerarTopProductos(datosClientes, VerPorLitros);
                SeriesTendencia = resultadoClientes.Series;
                EjeXTendencia = resultadoClientes.EjesX;
                EjeYTendencia = resultadoClientes.EjesY;
            }
        }

        // ---------------------------------------------------------
        // MÉTODO CLAVE: Generar Tarjetas Comparativas (Q1, Q2...)
        // ---------------------------------------------------------
        private void GenerarDesglosePorPeriodo(string periodo)
        {
            if ( _datosAnualesCache == null ) return;
            var datosBase = _datosFamiliaActual
                .Where(x => !x.Linea.Contains("FERRETERIA", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Filtramos el histórico global solo para la familia actual
            var datosFamiliaAnuales = _datosAnualesCache
                .Where(x => x.Familia == TituloDetalle)
                .ToList();

            // OPCIONAL: También generamos el gráfico de líneas por si decides dejarlo de fondo
            // (Si no lo usas en el XAML, no consume recursos visuales)
            var resultadoGrafico = _chartService.GenerarTendenciaLineas(datosFamiliaAnuales, periodo);
            SeriesComportamientoLineas = resultadoGrafico.Series;
            EjeXMensual = resultadoGrafico.EjesX;
            OnPropertyChanged(nameof(SeriesComportamientoLineas));
            OnPropertyChanged(nameof(EjeXMensual));

            // GENERAR TARJETAS DE RENDIMIENTO
            // Si el usuario selecciona "ANUAL", por defecto mostramos desglose TRIMESTRAL para que vea Q1-Q4
            string modoCalculo = periodo == "ANUAL" ? "TRIMESTRAL" : periodo;

            var listaNueva = _familiaLogic.CalcularDesgloseClientes(datosFamiliaAnuales, modoCalculo);

            ListaDesglose = new ObservableCollection<SubLineaPerformanceModel>(listaNueva);
        }

        // ---------------------------------------------------------
        // EXPORTACIÓN
        // ---------------------------------------------------------
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