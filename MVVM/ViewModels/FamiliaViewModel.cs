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

using WPF_PAR.Core;
using WPF_PAR.MVVM.Models;
using WPF_PAR.Services;
using WPF_PAR.Services.Interfaces;

namespace WPF_PAR.MVVM.ViewModels
{
    public class FamiliaViewModel : ObservableObject
    {
        // SERVICIOS
        private readonly ReportesService _reportesService;
        private readonly CatalogoService _catalogoService;
        private readonly ChartService _chartService;
        private readonly FamiliaLogicService _familiaLogic;
        private readonly IDialogService _dialogService;
        private readonly INotificationService _notificationService;
        public FilterService Filters { get; }

        private bool _isInitialized = false;

        // COLECCIONES
        private ObservableCollection<FamiliaResumenModel> _tarjetasFamilias;
        public ObservableCollection<FamiliaResumenModel> TarjetasFamilias { get => _tarjetasFamilias; set { _tarjetasFamilias = value; OnPropertyChanged(); } }

        private ObservableCollection<FamiliaResumenModel> _tarjetasArquitectonica;
        public ObservableCollection<FamiliaResumenModel> TarjetasArquitectonica { get => _tarjetasArquitectonica; set { _tarjetasArquitectonica = value; OnPropertyChanged(); } }

        private ObservableCollection<FamiliaResumenModel> _tarjetasEspecializada;
        public ObservableCollection<FamiliaResumenModel> TarjetasEspecializada { get => _tarjetasEspecializada; set { _tarjetasEspecializada = value; OnPropertyChanged(); } }

        // Tabla Detallada
        private ObservableCollection<VentaReporteModel> _detalleVentas;
        public ObservableCollection<VentaReporteModel> DetalleVentas
        {
            get => _detalleVentas;
            set { _detalleVentas = value; OnPropertyChanged(); OnPropertyChanged(nameof(NoHayDatos)); }
        }
        public bool NoHayDatos => DetalleVentas == null || DetalleVentas.Count == 0;

        // Títulos
        private string _tituloGraficoPastel = "Distribución";
        public string TituloGraficoPastel { get => _tituloGraficoPastel; set { _tituloGraficoPastel = value; OnPropertyChanged(); } }

        private string _tituloGraficoBarras = "Ranking de Productos";
        public string TituloGraficoBarras { get => _tituloGraficoBarras; set { _tituloGraficoBarras = value; OnPropertyChanged(); } }

        // Lista Desglose
        private ObservableCollection<SubLineaPerformanceModel> _listaDesglose;
        public ObservableCollection<SubLineaPerformanceModel> ListaDesglose { get => _listaDesglose; set { _listaDesglose = value; OnPropertyChanged(); } }

        // --- GRAFICOS (Nombres actualizados para coincidir con el nuevo XAML) ---
        public ISeries[] SeriesPastelDinero { get; set; } // Antes SeriesDetalle
        public ISeries[] SeriesPastelLitros { get; set; } // Nuevo

        public ISeries[] SeriesBarrasTop { get; set; }    // Antes SeriesTendencia
        public Axis[] EjeXBarras { get; set; }            // Antes EjeXTendencia
        public Axis[] EjeYBarras { get; set; }            // Antes EjeYTendencia

        public ISeries[] SeriesComportamientoLineas { get; set; }
        public Axis[] EjeXMensual { get; set; }

        // PROPIEDAD PARA LA LEYENDA (Dark Mode)
        public SolidColorPaint LegendTextPaint { get; set; }

        // ESTADO
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
            set { _verPorLitros = value; OnPropertyChanged(); ActualizarGraficosPorSubLinea(); }
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
            set { _subLineaSeleccionada = value; OnPropertyChanged(); if ( !string.IsNullOrEmpty(value) ) ActualizarGraficosPorSubLinea(); }
        }
        private ObservableCollection<KeyValuePair<int, string>> _listaSucursales;
        public ObservableCollection<KeyValuePair<int, string>> ListaSucursales
        {
            get => _listaSucursales;
            set { _listaSucursales = value; OnPropertyChanged(); }
        }
        // COMANDOS
        public RelayCommand ActualizarCommand { get; set; }
        public RelayCommand VerDetalleCommand { get; set; }
        public RelayCommand RegresarCommand { get; set; }
        public RelayCommand ExportarExcelCommand { get; set; }
        public RelayCommand CambiarPeriodoGraficoCommand { get; set; }
        public RelayCommand OrdenarVentaCommand { get; set; }
        public RelayCommand OrdenarNombreCommand { get; set; }
        public RelayCommand ExportarGlobalCommand { get; set; }


        public FamiliaViewModel(
            IDialogService dialogService,
            BusinessLogicService businessLogic,
            FilterService filterService,
            ChartService chartService,
            FamiliaLogicService familiaLogic,
            INotificationService notificationService)
        {
            _dialogService = dialogService;
            Filters = filterService;
            _chartService = chartService;
            _familiaLogic = familiaLogic;
            _notificationService = notificationService;

            _reportesService = new ReportesService();
            _catalogoService = new CatalogoService(businessLogic);

            // Inicializar Colecciones
            TarjetasFamilias = new ObservableCollection<FamiliaResumenModel>();
            TarjetasArquitectonica = new ObservableCollection<FamiliaResumenModel>();
            TarjetasEspecializada = new ObservableCollection<FamiliaResumenModel>();
            DetalleVentas = new ObservableCollection<VentaReporteModel>();

            _ventasProcesadas = new List<VentaReporteModel>();
            _datosAnualesCache = new List<VentaReporteModel>();

            // Configurar Colores Iniciales (Dark Mode)
            ActualizarColoresGraficos();

            // Configurar Comandos
            ActualizarCommand = new RelayCommand(o => EjecutarReporte());
            OrdenarVentaCommand = new RelayCommand(o => AplicarOrden("VENTA"));
            OrdenarNombreCommand = new RelayCommand(o => AplicarOrden("NOMBRE"));
            RegresarCommand = new RelayCommand(o => VerResumen = true);
            ExportarExcelCommand = new RelayCommand(o => GenerarReporteExcel(false));
            ExportarGlobalCommand = new RelayCommand(o => GenerarReporteExcel(true));

            VerDetalleCommand = new RelayCommand(param => { if ( param is string familia ) CargarDetalle(familia); });
            CambiarPeriodoGraficoCommand = new RelayCommand(param => { if ( param is string periodo ) GenerarDesglosePorPeriodo(periodo); });

            Filters.OnFiltrosCambiados += EjecutarReporte;
        }

        private void ActualizarColoresGraficos()
        {
            bool isDark = false;
            try { isDark = Properties.Settings.Default.IsDarkMode; } catch { }

            var colorTexto = isDark ? SKColors.White : SKColors.Black;
            var colorSeparador = isDark ? SKColors.White.WithAlpha(30) : SKColors.Gray.WithAlpha(50);

            // Leyenda
            LegendTextPaint = new SolidColorPaint(colorTexto);

            // Ejes Barras Top (Antes Tendencia)
            EjeXBarras = new Axis[] { new Axis { IsVisible = false, LabelsPaint = new SolidColorPaint(colorTexto) } };
            EjeYBarras = new Axis[]
            {
                new Axis
                {
                    IsVisible = true,
                    LabelsPaint = new SolidColorPaint(colorTexto),
                    TextSize = 12,
                    SeparatorsPaint = new SolidColorPaint(colorSeparador)
                }
            };

            // Ejes Mensual
            EjeXMensual = new Axis[]
            {
                new Axis
                {
                    LabelsPaint = new SolidColorPaint(colorTexto),
                    SeparatorsPaint = null
                }
            };
        }

        public void CargarDatosIniciales()
        {
            if ( _isInitialized ) return;

            IsLoading = true;
            CargarPorLinea("Todas");
            EjecutarReporte();
            _isInitialized = true;
        }

        public void CargarPorLinea(string linea)
        {
            _lineaActual = linea;

            // Si ya tenemos datos procesados, solo filtramos visualmente
            if ( _ventasProcesadas != null && _ventasProcesadas.Any() )
            {
                GenerarResumenVisual();
                _notificationService.ShowSuccess($"Visualizando línea: {linea.ToUpper()}");
            }
            else
            {
                // Si no hay datos, mostramos vacío
                TarjetasFamilias = new ObservableCollection<FamiliaResumenModel>(_familiaLogic.ObtenerTarjetasVacias(_lineaActual));

                if ( _isInitialized )
                    _notificationService.ShowInfo($"No hay datos cargados para {linea}.");
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

                // Enriquecer datos con catálogo
                foreach ( var venta in _ventasProcesadas )
                {
                    var info = _catalogoService.ObtenerInfo(venta.Articulo);
                    venta.Familia = info.FamiliaSimple;
                    venta.Linea = info.Linea;
                    venta.Descripcion = info.Descripcion;
                    venta.LitrosUnitarios = info.Litros;
                }

                // Filtrar ferretería
                _ventasProcesadas = _ventasProcesadas
                    .Where(x => x.Familia != "FERRETERIA" && !x.Linea.Contains("FERRETERIA", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                GenerarResumenVisual();
                IsLoading = false;

                if ( _ventasProcesadas.Count == 0 )
                {
                    _notificationService.ShowInfo("La consulta no devolvió resultados para este periodo.");
                }

                // Cargar caché anual en segundo plano
                _datosAnualesCache = await _reportesService.ObtenerHistoricoAnualPorArticulo(
                    DateTime.Now.Year.ToString(), Filters.SucursalId.ToString());

                foreach ( var item in _datosAnualesCache )
                {
                    var info = _catalogoService.ObtenerInfo(item.Articulo);
                    item.Linea = info.Linea;
                    item.Familia = info.FamiliaSimple;
                }

                if ( VerDetalle ) GenerarDesglosePorPeriodo("ANUAL");
            }
            catch ( Exception ex )
            {
                IsLoading = false;
                await _notificationService.ShowErrorDialog($"Error al cargar datos:\n{ex.Message}");
            }
        }

        private void GenerarResumenVisual()
        {
            var (arqui, espe) = _familiaLogic.CalcularResumenGlobal(_ventasProcesadas);
            TarjetasArquitectonica = new ObservableCollection<FamiliaResumenModel>(arqui);
            TarjetasEspecializada = new ObservableCollection<FamiliaResumenModel>(espe);

            GranTotalVenta = _ventasProcesadas.Sum(x => x.TotalVenta);
            GranTotalLitros = _ventasProcesadas.Sum(x => x.LitrosTotales);

            IEnumerable<FamiliaResumenModel> resultado;
            if ( _lineaActual == "Arquitectonica" ) resultado = arqui;
            else if ( _lineaActual == "Especializada" ) resultado = espe;
            else resultado = arqui.Concat(espe);

            TarjetasFamilias = new ObservableCollection<FamiliaResumenModel>(resultado);
        }

        private void AplicarOrden(string criterio)
        {
            if ( TarjetasFamilias == null ) return;
            var lista = TarjetasFamilias.ToList();
            TarjetasFamilias = new ObservableCollection<FamiliaResumenModel>(_familiaLogic.OrdenarTarjetas(lista, criterio));
            _notificationService.ShowSuccess($"Ordenado por {criterio.ToLower()}.");
        }

        private void CargarDetalle(string familia)
        {
            TituloDetalle = familia;
            _datosFamiliaActual = _ventasProcesadas.Where(x => x.Familia == familia).ToList();

            SubLineasDisponibles.Clear();
            SubLineasDisponibles.Add("TODAS");

            var lineas = _datosFamiliaActual.Select(x => x.Linea).Distinct().OrderBy(x => x).ToList();
            foreach ( var l in lineas ) SubLineasDisponibles.Add(l);

            SubLineaSeleccionada = "TODAS";
            GenerarDesglosePorPeriodo("ANUAL");
            VerResumen = false;
        }

        private void ActualizarGraficosPorSubLinea()
        {
            if ( _datosFamiliaActual == null ) return;

            // Filtramos ferretería por si acaso
            var datosBase = _datosFamiliaActual
                .Where(x => !x.Linea.Contains("FERRETERIA", StringComparison.OrdinalIgnoreCase))
                .ToList();

            string filtro = SubLineaSeleccionada;
            bool esVistaGlobal = ( filtro == "TODAS" || string.IsNullOrEmpty(filtro) );

            var datosFiltrados = esVistaGlobal
                ? datosBase.ToList()
                : datosBase.Where(x => ( x.Linea ?? "" ).Equals(filtro, StringComparison.OrdinalIgnoreCase)).ToList();

            // Actualizar tabla inferior
            DetalleVentas = new ObservableCollection<VentaReporteModel>(
                datosFiltrados.OrderByDescending(x => x.TotalVenta));

            // =========================================================
            // 1. TÍTULOS DE LAS GRÁFICAS
            // =========================================================
            if ( esVistaGlobal )
            {
                TituloGraficoPastel = "Distribución por Línea ($)"; // Título Dinero
                TituloGraficoBarras = "Top 5 Productos Globales";   // Título Barras
            }
            else
            {
                TituloGraficoPastel = "Distribución por Producto ($)";
                TituloGraficoBarras = "Top 5 Clientes en " + filtro;
            }

            // =========================================================
            // 2. PREPARACIÓN DE DATOS (DINERO VS LITROS)
            // =========================================================

            // Definimos una estructura común para procesar ambas gráficas igual
            var resumenDatos = new List<(string Nombre, decimal Venta, double Litros)>();

            if ( esVistaGlobal )
            {
                // VISTA GLOBAL: Agrupamos por LÍNEA (Ej. Vinilicas, Esmaltes)
                resumenDatos = datosFiltrados
                    .GroupBy(x => x.Linea)
                    .Select(g => (
                        Nombre: g.Key,
                        Venta: g.Sum(x => x.TotalVenta),
                        Litros: ( double ) g.Sum(x => x.LitrosTotales)
                    ))
                    .ToList();
            }
            else
            {
                // VISTA FILTRADA: Agrupamos por PRODUCTO (Ej. Ecopar 19L)
                resumenDatos = datosFiltrados
                    .GroupBy(x => x.Descripcion)
                    .Select(g => (
                        Nombre: g.Key,
                        Venta: g.Sum(x => x.TotalVenta),
                        Litros: ( double ) g.Sum(x => x.LitrosTotales)
                    ))
                    .ToList();
            }

            // =========================================================
            // 3. CONSTRUCCIÓN DE SERIES (PASTELES)
            // =========================================================

            // --- PASTEL 1: DINERO ---
            var topDinero = resumenDatos.OrderByDescending(x => x.Venta).Take(5).ToList();

            SeriesPastelDinero = topDinero.Select(x => new PieSeries<double>
            {
                Values = new double[] { ( double ) x.Venta },
                // AQUI aplicamos la normalización para quitar el "PIN-" de las líneas
                Name = NormalizarNombreProducto(x.Nombre),
                DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Outer,
                DataLabelsFormatter = p => $"{p.Model:C0}",
                ToolTipLabelFormatter = p => $"{p.Context.Series.Name}: {p.Model:C0} ({p.StackedValue.Share:P1})"
            }).ToArray();

            // --- PASTEL 2: LITROS (Ahora sigue la misma lógica de agrupación) ---
            var topLitros = resumenDatos.OrderByDescending(x => x.Litros).Take(5).ToList();

            SeriesPastelLitros = topLitros.Select(x => new PieSeries<double>
            {
                Values = new double[] { x.Litros },
                Name = NormalizarNombreProducto(x.Nombre), // También normalizamos aquí
                DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Outer,
                DataLabelsFormatter = p => $"{p.Model:N0} L",
                ToolTipLabelFormatter = p => $"{p.Context.Series.Name}: {p.Model:N0} L ({p.StackedValue.Share:P1})"
            }).ToArray();


            // =========================================================
            // 4. GRÁFICA DE BARRAS (Inferior)
            // =========================================================
            if ( esVistaGlobal )
            {
                // Global: Top Productos
                var resultadoTop = _chartService.GenerarTopProductos(datosFiltrados, VerPorLitros);
                SeriesBarrasTop = resultadoTop.Series;
                EjeXBarras = resultadoTop.EjesX;
                EjeYBarras = resultadoTop.EjesY;
            }
            else
            {
                // Filtrado: Top Clientes (O Productos si prefieres, aquí dejé Clientes como tenías)
                var topClientes = datosFiltrados
                    .GroupBy(x => x.Cliente)
                    .Select(g => new VentaReporteModel
                    {
                        Descripcion = Truncar(g.Key, 25), // Nombre del cliente truncado
                        TotalVenta = g.Sum(x => x.TotalVenta),
                        LitrosUnitarios = g.Sum(x => x.LitrosTotales),
                        // Propiedades dummy necesarias para el servicio
                        Cantidad = 1,
                        PrecioUnitario = 0
                    })
                    .ToList();

                var resultadoTop = _chartService.GenerarTopProductos(topClientes, VerPorLitros);
                SeriesBarrasTop = resultadoTop.Series;
                EjeXBarras = resultadoTop.EjesX;
                EjeYBarras = resultadoTop.EjesY;
            }

            // =========================================================
            // NOTIFICAR CAMBIOS
            // =========================================================
            OnPropertyChanged(nameof(SeriesPastelDinero));
            OnPropertyChanged(nameof(SeriesPastelLitros));
            OnPropertyChanged(nameof(TituloGraficoPastel));

            OnPropertyChanged(nameof(SeriesBarrasTop));
            OnPropertyChanged(nameof(EjeXBarras));
            OnPropertyChanged(nameof(EjeYBarras));
            OnPropertyChanged(nameof(TituloGraficoBarras));
        }

        // Helper para truncar nombres largos de clientes
        private string Truncar(string t, int m) => t.Length > m ? t.Substring(0, m - 3) + "..." : t;

        private void GenerarDesglosePorPeriodo(string periodo)
        {
            if ( _datosAnualesCache == null || !_datosAnualesCache.Any() ) return;

            var datosFamiliaAnuales = _datosAnualesCache.Where(x => x.Familia == TituloDetalle).ToList();

            var resultadoGrafico = _chartService.GenerarTendenciaLineas(datosFamiliaAnuales, periodo);
            SeriesComportamientoLineas = resultadoGrafico.Series;
            EjeXMensual = resultadoGrafico.EjesX;
            OnPropertyChanged(nameof(SeriesComportamientoLineas));
            OnPropertyChanged(nameof(EjeXMensual));

            string modoCalculo = periodo == "ANUAL" ? "TRIMESTRAL" : periodo;
            var listaNueva = _familiaLogic.CalcularDesgloseClientes(datosFamiliaAnuales, modoCalculo);
            ListaDesglose = new ObservableCollection<SubLineaPerformanceModel>(listaNueva);
        }

        private async void GenerarReporteExcel(bool esGlobal)
        {
            var datos = esGlobal ? _ventasProcesadas : DetalleVentas?.ToList();
            if ( datos == null || datos.Count == 0 )
            {
                await _notificationService.ShowErrorDialog("No hay datos para exportar.");
                return;
            }

            string nombre = esGlobal ? $"Reporte_GLOBAL_{DateTime.Now:yyyyMMdd}" : $"Reporte_{TituloDetalle}_{DateTime.Now:yyyyMMdd}";
            string ruta = _dialogService.ShowSaveFileDialog("CSV|*.csv", nombre + ".csv");

            if ( !string.IsNullOrEmpty(ruta) )
            {
                IsLoading = true;
                try
                {
                    await Task.Run(() => File.WriteAllText(ruta, _familiaLogic.GenerarContenidoCSV(datos), Encoding.UTF8));
                    _notificationService.ShowSuccess("Archivo guardado exitosamente.");
                }
                catch ( Exception ex )
                {
                    await _notificationService.ShowErrorDialog($"Error al guardar: {ex.Message}");
                }
                finally { IsLoading = false; }
            }
        }

        private void FiltrarTabla()
        {
            if ( DetalleVentas == null ) return;
            var view = CollectionViewSource.GetDefaultView(DetalleVentas);
            if ( string.IsNullOrWhiteSpace(TextoBusqueda) ) view.Filter = null;
            else
            {
                string t = TextoBusqueda.ToUpper();
                view.Filter = o =>
                {
                    if ( o is VentaReporteModel v )
                        return ( v.Cliente?.ToUpper().Contains(t) ?? false ) ||
                               ( v.Descripcion?.ToUpper().Contains(t) ?? false ) ||
                               ( v.Linea?.ToUpper().Contains(t) ?? false );
                    return false;
                };
            }
        }

        // Método auxiliar para limpiar nombres
        private string NormalizarNombreProducto(string nombreOriginal)
        {
            if ( string.IsNullOrEmpty(nombreOriginal) ) return "";

            // 1. Quitar espacios extra y convertir a minúsculas para analizar
            string limpio = nombreOriginal.Trim();

            // 2. Regla: Si tiene guion (ej. "PIN-ECOPAR"), tomamos lo que está después del guion
            if ( limpio.Contains("-") )
            {
                var partes = limpio.Split('-');
                if ( partes.Length > 1 )
                {
                    limpio = partes[1]; // Tomas "ECOPAR"
                }
            }

            // 3. Regla: Convertir a "Título" (Primera mayúscula, resto minúscula)
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(limpio.ToLower());
        }
    }
}