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
        // SERVICIOS
        private readonly ReportesService _reportesService;
        private readonly CatalogoService _catalogoService;
        private readonly ChartService _chartService;
        private readonly FamiliaLogicService _familiaLogic;
        private readonly IDialogService _dialogService;
        private bool _isInitialized = false;
        private readonly INotificationService _notificationService;
        public FilterService Filters { get; }

        // COLECCIONES
        private ObservableCollection<FamiliaResumenModel> _tarjetasFamilias;
        public ObservableCollection<FamiliaResumenModel> TarjetasFamilias
        {
            get => _tarjetasFamilias;
            set { _tarjetasFamilias = value; OnPropertyChanged(); }
        }

        private ObservableCollection<FamiliaResumenModel> _tarjetasArquitectonica;
        public ObservableCollection<FamiliaResumenModel> TarjetasArquitectonica
        {
            get => _tarjetasArquitectonica;
            set { _tarjetasArquitectonica = value; OnPropertyChanged(); }
        }

        private ObservableCollection<FamiliaResumenModel> _tarjetasEspecializada;
        public ObservableCollection<FamiliaResumenModel> TarjetasEspecializada
        {
            get => _tarjetasEspecializada;
            set { _tarjetasEspecializada = value; OnPropertyChanged(); }
        }

        private ObservableCollection<LineaResumenModel> _resumenLineas;
        public ObservableCollection<LineaResumenModel> ResumenLineas
        {
            get => _resumenLineas;
            set { _resumenLineas = value; OnPropertyChanged(); }
        }

        // Tabla Detallada
        private ObservableCollection<VentaReporteModel> _detalleVentas;
        public ObservableCollection<VentaReporteModel> DetalleVentas
        {
            get => _detalleVentas;
            set { _detalleVentas = value; OnPropertyChanged(); OnPropertyChanged(nameof(NoHayDatos)); }
        }
        // Propiedad que devuelve TRUE si la lista es nula o tiene 0 elementos
        public bool NoHayDatos => DetalleVentas == null || DetalleVentas.Count == 0;

        // Títulos
        private string _tituloGraficoPastel = "Distribución";
        public string TituloGraficoPastel { get => _tituloGraficoPastel; set { _tituloGraficoPastel = value; OnPropertyChanged(); } }

        private string _tituloGraficoBarras = "Ranking de Productos";
        public string TituloGraficoBarras { get => _tituloGraficoBarras; set { _tituloGraficoBarras = value; OnPropertyChanged(); } }

        // Lista Desglose
        private ObservableCollection<SubLineaPerformanceModel> _listaDesglose;
        public ObservableCollection<SubLineaPerformanceModel> ListaDesglose
        {
            get => _listaDesglose;
            set { _listaDesglose = value; OnPropertyChanged(); }
        }

        // GRAFICOS
        private ISeries[] _seriesDetalle;
        public ISeries[] SeriesDetalle { get => _seriesDetalle; set { _seriesDetalle = value; OnPropertyChanged(); } }

        private ISeries[] _seriesTendencia;
        public ISeries[] SeriesTendencia { get => _seriesTendencia; set { _seriesTendencia = value; OnPropertyChanged(); } }

        private Axis[] _ejeXTendencia;
        public Axis[] EjeXTendencia { get => _ejeXTendencia; set { _ejeXTendencia = value; OnPropertyChanged(); } }

        private Axis[] _ejeYTendencia;
        public Axis[] EjeYTendencia { get => _ejeYTendencia; set { _ejeYTendencia = value; OnPropertyChanged(); } }

        public ISeries[] SeriesComportamientoLineas { get; set; }
        public Axis[] EjeXMensual { get; set; }

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
            ExportarExcelCommand = new RelayCommand(o => GenerarReporteExcel(false)); // False = Exportar Detalle actual

            // NUEVO COMANDO: True = Exportar Todo
            ExportarGlobalCommand = new RelayCommand(o => GenerarReporteExcel(true));

            VerDetalleCommand = new RelayCommand(param => { if ( param is string familia ) CargarDetalle(familia); });
            CambiarPeriodoGraficoCommand = new RelayCommand(param => { if ( param is string periodo ) GenerarDesglosePorPeriodo(periodo); });
            Filters.OnFiltrosCambiados += EjecutarReporte;
        }
        public void CargarDatosIniciales()
        {
            // Si ya tiene datos, no recargamos (opcional, por si quieres caché)
            if ( _isInitialized ) return;

            IsLoading = true;

            // Aquí sí va la lógica pesada
            CargarPorLinea("Todas");
            EjecutarReporte();

            _isInitialized = true;
        }
        public void CargarPorLinea(string linea)
        {
            _lineaActual = linea;

            if ( _ventasProcesadas != null && _ventasProcesadas.Any() )
            {
                GenerarResumenVisual();
                // Feedback sutil: "Mostrando datos de Arquitectónica"
                _notificationService.ShowSuccess($"Visualizando línea: {linea.ToUpper()}");
            }
            else
            {
                TarjetasFamilias = new ObservableCollection<FamiliaResumenModel>(_familiaLogic.ObtenerTarjetasVacias(_lineaActual));
                // Aviso importante
                _notificationService.ShowError($"No hay ventas registradas para la línea {linea} en este periodo.");
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
                    DateTime.Now.Year.ToString(), Filters.SucursalId.ToString());

                foreach ( var item in _datosAnualesCache )
                {
                    var info = _catalogoService.ObtenerInfo(item.Articulo);
                    item.Linea = info.Linea;
                    item.Familia = info.FamiliaSimple;
                }

                // Filtrar ferretería si es necesario
                _ventasProcesadas = ventasRaw
                        .Where(x => x.Familia != "FERRETERIA" && !x.Linea.Contains("FERRETERIA", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                if ( _ventasProcesadas.Count == 0 )
                {
                    _notificationService.ShowError("La consulta no devolvió resultados para este periodo.");
                }

                if ( VerDetalle ) GenerarDesglosePorPeriodo("ANUAL");

                var auditoria = ventasRaw
                    .GroupBy(x => x.Mov)
                    .Select(g => new
    {
        Tipo = g.Key,
        Total = g.Sum(x => x.TotalVenta)
    })
    .ToList();

            }
            catch ( Exception ex )
            {
                IsLoading = false;
                await _notificationService.ShowErrorDialog($"Error crítico al cargar los datos:\n{ex.Message}");
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
            // Aquí puedes reordenar TarjetasFamilias según el criterio
            var lista = TarjetasFamilias.ToList();
            TarjetasFamilias = new ObservableCollection<FamiliaResumenModel>(_familiaLogic.OrdenarTarjetas(lista, criterio));
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
            var datosBase = _datosFamiliaActual.Where(x => !x.Linea.Contains("FERRETERIA", StringComparison.OrdinalIgnoreCase)).ToList();

            string filtro = SubLineaSeleccionada;
            bool esVistaGlobal = ( filtro == "TODAS" || string.IsNullOrEmpty(filtro) );
            var datosFiltrados = esVistaGlobal ? datosBase.ToList() : datosBase.Where(x => ( x.Linea ?? "" ).Equals(filtro, StringComparison.OrdinalIgnoreCase)).ToList();

            // 1. Tabla de Detalle (Siempre muestra el desglose de ventas)
            DetalleVentas = new ObservableCollection<VentaReporteModel>(datosFiltrados.OrderByDescending(x => x.TotalVenta));

            // =========================================================
            // 2. GRÁFICO DE BARRAS (Tendencia)
            // =========================================================
            if ( esVistaGlobal )
            {
                // VISTA GLOBAL: Mostramos Ranking de PRODUCTOS
                TituloGraficoBarras = "Top 5 Productos Globales";

                // Generamos gráfico directo con los productos filtrados
                var resultadoTop = _chartService.GenerarTopProductos(datosFiltrados, VerPorLitros);
                SeriesTendencia = resultadoTop.Series;
                EjeXTendencia = resultadoTop.EjesX;
                EjeYTendencia = resultadoTop.EjesY;
            }
            else
            {
                // VISTA FILTRADA: Ranking de CLIENTES
                TituloGraficoBarras = "Top 5 Clientes en " + filtro;

                var topClientes = datosFiltrados
                    .GroupBy(x => x.Cliente)
                    .Select(g => new VentaReporteModel
                    {
                        Descripcion = Truncar(g.Key, 20),

                        // --- CORRECCIÓN AQUÍ ---
                        // El error era que asignábamos la suma a 'PrecioUnitario' esperando que calculara solo.
                        // Como ahora TotalVenta es independiente, debemos asignarlo directo.
                        TotalVenta = g.Sum(x => x.TotalVenta),

                        // Los litros sí funcionaban porque asignabas LitrosUnitarios
                        LitrosUnitarios = g.Sum(x => x.LitrosTotales),

                        // Valores dummy para que no truenen otros cálculos
                        Cantidad = 1,
                        PrecioUnitario = 0
                    })
                    .ToList();

                var resultadoTop = _chartService.GenerarTopProductos(topClientes, VerPorLitros);
                // ... resto del código igual
                SeriesTendencia = resultadoTop.Series;
                EjeXTendencia = resultadoTop.EjesX;
                EjeYTendencia = resultadoTop.EjesY;
            }

            // =========================================================
            // 3. GRÁFICO DE PASTEL (Distribución)
            // =========================================================
            // Función local para cortar nombres largos
            string Truncar(string texto, int max) =>
                texto.Length > max ? texto.Substring(0, max - 3) + "..." : texto;

            if ( esVistaGlobal )
            {
                TituloGraficoPastel = "Distribución por Sub-Línea";
                var gruposLinea = datosFiltrados.GroupBy(x => x.Linea)
                    .Select(g => new LineaResumenModel
                    {
                        NombreLinea = g.Key, // Cortamos nombre a 15 letras
                        VentaTotal = g.Sum(x => x.TotalVenta)
                    })
                    .OrderByDescending(x => x.VentaTotal)
                    .Take(5)
                    .ToList();
                SeriesDetalle = _chartService.GenerarPieChart(gruposLinea);
            }
            else
            {
                TituloGraficoPastel = "Share de Productos";
                var gruposProductos = datosFiltrados.GroupBy(x => x.Descripcion)
                    .Select(g => new LineaResumenModel
                    {
                        NombreLinea = g.Key, // Cortamos nombre a 15 letras
                        VentaTotal = g.Sum(x => x.TotalVenta)
                    })
                    .OrderByDescending(x => x.VentaTotal)
                    .Take(5)
                    .ToList();
                SeriesDetalle = _chartService.GenerarPieChart(gruposProductos);
            }
        }
        private void GenerarDesglosePorPeriodo(string periodo)
        {
            if ( _datosAnualesCache == null ) return;
            var datosFamiliaAnuales = _datosAnualesCache.Where(x => x.Familia == TituloDetalle).ToList();

            var resultadoGrafico = _chartService.GenerarTendenciaLineas(datosFamiliaAnuales, periodo);
            SeriesComportamientoLineas = resultadoGrafico.Series;
            EjeXMensual = resultadoGrafico.EjesX;
            OnPropertyChanged(nameof(SeriesComportamientoLineas));
            OnPropertyChanged(nameof(EjeXMensual));

            string modoCalculo = periodo == "ANUAL" ? "TRIMESTRAL" : periodo;
            // Aquí usamos el nuevo servicio unificado. Si FamiliaLogicService espera VentaReporteModel, todo OK.
            var listaNueva = _familiaLogic.CalcularDesgloseClientes(datosFamiliaAnuales, modoCalculo);
            ListaDesglose = new ObservableCollection<SubLineaPerformanceModel>(listaNueva);
        }
        // Cambia la firma de tu método actual 'GenerarReporteExcel' para aceptar un booleano
        private async void GenerarReporteExcel(bool esGlobal = false)
        {
            // DEFINIMOS QUÉ DATOS VAMOS A USAR
            List<VentaReporteModel> datosAExportar;
            string nombreBase;

            if ( esGlobal )
            {
                // CASO 1: EXPORTAR TODO (Botón Principal)
                datosAExportar = _ventasProcesadas;
                nombreBase = $"Reporte_GLOBAL_Suc{Filters.SucursalId}_{DateTime.Now:yyyyMMdd}";
            }
            else
            {
                // CASO 2: EXPORTAR DETALLE (Botón dentro de la familia)
                datosAExportar = DetalleVentas?.ToList();
                nombreBase = $"Reporte_{TituloDetalle?.Replace(" ", "")}_{DateTime.Now:yyyyMMdd}";
            }

            // VALIDACIÓN
            if ( datosAExportar == null || datosAExportar.Count == 0 )
            {
                await _notificationService.ShowErrorDialog("No hay datos para exportar en la selección actual.");
                return;
            }

            // EL RESTO ES IGUAL (Guardar archivo...)
            string ruta = _dialogService.ShowSaveFileDialog("Archivo CSV (*.csv)|*.csv", nombreBase + ".csv");

            if ( !string.IsNullOrEmpty(ruta) )
            {
                IsLoading = true;
                try
                {
                    await Task.Run(() =>
                    {
                        // Reutilizamos tu lógica existente que ya incluye Mov y Folio
                        string contenido = _familiaLogic.GenerarContenidoCSV(datosAExportar);
                        File.WriteAllText(ruta, contenido, Encoding.UTF8);
                    });

                    _notificationService.ShowSuccess($"Reporte {( esGlobal ? "GLOBAL" : "" )} guardado exitosamente.");
                }
                catch ( Exception ex )
                {
                    await _notificationService.ShowErrorDialog($"Error al exportar:\n{ex.Message}");
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }
        private void FiltrarTabla()
        {
            if ( DetalleVentas == null ) return;
            ICollectionView view = CollectionViewSource.GetDefaultView(DetalleVentas);
            if ( string.IsNullOrWhiteSpace(TextoBusqueda) ) view.Filter = null;
            else view.Filter = (obj) => { var v = obj as VentaReporteModel; if ( v == null ) return false; string t = TextoBusqueda.ToUpper(); return ( v.Cliente?.ToUpper().Contains(t) ?? false ) || ( v.Descripcion?.ToUpper().Contains(t) ?? false ) || ( v.Linea?.ToUpper().Contains(t) ?? false ); };
        }
    }
}