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
using System.Windows.Media;

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
        private readonly IDialogService _dialogService;
        private readonly ISnackbarService _snackbarService;
        private readonly BusinessLogicService _businessLogic;
        public FilterService Filters { get; }

        private ObservableCollection<FamiliaResumenModel> _tarjetasFamilias;
        public ObservableCollection<FamiliaResumenModel> TarjetasFamilias
        {
            get => _tarjetasFamilias;
            set { _tarjetasFamilias = value; OnPropertyChanged(); }
        }
        private ObservableCollection<DatoRanking> _topClientesFamilia;
        public ObservableCollection<DatoRanking> TopClientesFamilia
        {
            get => _topClientesFamilia;
            set { _topClientesFamilia = value; OnPropertyChanged(); }
        }
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

        private List<VentaReporteModel> _ventasProcesadas;
        private List<VentaReporteModel> _datosAnualesCache; 

        private ISeries[] _seriesDetalle;
        public ISeries[] SeriesDetalle { get => _seriesDetalle; set { _seriesDetalle = value; OnPropertyChanged(); } }

        public ISeries[] SeriesTendencia { get; set; }
        public Axis[] EjeXTendencia { get; set; }
        public Axis[] EjeYTendencia { get; set; }
        public ObservableCollection<FamiliaResumenModel> TarjetasArquitectonica { get; set; }
        public ObservableCollection<FamiliaResumenModel> TarjetasEspecializada { get; set; }

        private decimal _granTotalVenta;
        public decimal GranTotalVenta
        {
            get => _granTotalVenta;
            set { _granTotalVenta = value; OnPropertyChanged(); }
        }

        private double _granTotalLitros;
        public double GranTotalLitros
        {
            get => _granTotalLitros;
            set { _granTotalLitros = value; OnPropertyChanged(); }
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
        private List<VentaReporteModel> _datosFamiliaActual;

        public RelayCommand ActualizarCommand { get; set; }
        public RelayCommand VerDetalleCommand { get; set; }
        public RelayCommand RegresarCommand { get; set; }
        public RelayCommand ExportarExcelCommand { get; set; }
        public RelayCommand OrdenarVentaCommand { get; set; }
        public RelayCommand OrdenarNombreCommand { get; set; }


        public FamiliaViewModel(IDialogService dialogService, ISnackbarService snackbarService, BusinessLogicService businessLogic, FilterService filterService)
        {
            _dialogService = dialogService;
            _snackbarService = snackbarService;
            _businessLogic = businessLogic;
            Filters = filterService;

            _reportesService = new ReportesService();
            _catalogoService = new CatalogoService(businessLogic);

            TarjetasFamilias = new ObservableCollection<FamiliaResumenModel>();
            DetalleVentas = new ObservableCollection<VentaReporteModel>();
            ResumenLineas = new ObservableCollection<LineaResumenModel>();
            TarjetasArquitectonica = new ObservableCollection<FamiliaResumenModel>();
            TarjetasEspecializada = new ObservableCollection<FamiliaResumenModel>();
            OrdenarVentaCommand = new RelayCommand(o => AplicarOrden("VENTA"));
            OrdenarNombreCommand = new RelayCommand(o => AplicarOrden("NOMBRE"));

            _ventasProcesadas = new List<VentaReporteModel>();
            _datosAnualesCache = new List<VentaReporteModel>();

            InicializarTarjetasVacias();
           

            ActualizarCommand = new RelayCommand(o => EjecutarReporte());

            VerDetalleCommand = new RelayCommand(param =>
            {
                if ( param is string familia ) CargarDetalle(familia);
            });

            RegresarCommand = new RelayCommand(o => VerResumen = true);
            ExportarExcelCommand = new RelayCommand(o => GenerarReporteExcel());

            EjecutarReporte();
        }

        private void InicializarTarjetasVacias()
        {
            TarjetasFamilias.Clear();
            var familiasOrdenadas = ObtenerFamiliasBase();

            foreach ( var nombre in familiasOrdenadas )
            {
                string colorFondo = _businessLogic.ObtenerColorFamilia(nombre);
                TarjetasFamilias.Add(new FamiliaResumenModel
                {
                    NombreFamilia = nombre,
                    VentaTotal = 0,
                    LitrosTotales = 0,
                    MejorCliente = "---",
                    ProductoEstrella = "---",
                    ColorFondo = colorFondo,
                    ColorTexto = ColorHelper.ObtenerColorTextto(colorFondo)
                });
            }
        }

        public void CargarPorLinea(string linea)
        {
            _lineaActual = linea;
            if ( _ventasProcesadas != null && _ventasProcesadas.Count > 0 )
                GenerarResumenVisual();
            else
                EjecutarReporte();
        }
        private async void EjecutarReporte()
        {
            IsLoading = true;
            try
            {
                // 1. Obtener Datos Principales
                var ventasRaw = await _reportesService.ObtenerVentasBrutasRango(
                    Filters.SucursalId,
                    Filters.FechaInicio,
                    Filters.FechaFin
                );

                _ventasProcesadas = ventasRaw;

                // 2. Procesar Familias y Líneas
                foreach ( var venta in _ventasProcesadas )
                {
                    var info = _catalogoService.ObtenerInfo(venta.Articulo);
                    venta.Familia = info.FamiliaSimple;
                    venta.Linea = info.Linea; // Aseguramos que la Línea esté llena
                    venta.Descripcion = info.Descripcion; // Y la descripción limpia
                    venta.LitrosUnitarios = info.Litros;
                }

                GenerarResumenVisual(); // Muestra las tarjetas de resumen

                IsLoading = false;

                // 3. CARGAR HISTÓRICO ANUAL (Para tendencias)
                // Esto pasa en segundo plano después de quitar el loading principal
                _datosAnualesCache = await _reportesService.ObtenerHistoricoAnualPorArticulo(
                    Filters.FechaFin.Year.ToString(),
                    Filters.SucursalId.ToString()
                );

                // IMPORTANTE: Mapear Familias y LÍNEAS en el histórico también
                foreach ( var item in _datosAnualesCache )
                {
                    var info = _catalogoService.ObtenerInfo(item.Articulo);
                    item.Familia = info.FamiliaSimple;
                    item.Linea = info.Linea; // <--- ESTO FALTABA para que filtre por línea
                }

                // Si el usuario ya entró al detalle mientras cargaba, actualizamos la gráfica ahora
                if ( VerDetalle )
                {
                    ActualizarGraficosPorSubLinea();
                }

            }
            catch ( Exception ex )
            {
                IsLoading = false;
                _dialogService.ShowMessage("Error", $"Error al cargar reporte: {ex.Message}");
            }
        }
        private void AplicarOrden(string criterio)
        {
            if ( TarjetasFamilias == null || TarjetasFamilias.Count == 0 ) return;

            var lista = TarjetasFamilias.ToList();

            if ( criterio == "VENTA" )
            {
                lista = lista.OrderByDescending(x => x.VentaTotal).ToList();
            }
            else
            {
                lista = lista.OrderBy(x => x.NombreFamilia).ToList();
            }

            TarjetasFamilias = new ObservableCollection<FamiliaResumenModel>(lista);
        }
        private void GenerarResumenVisual()
        {
            TarjetasArquitectonica.Clear();
            TarjetasEspecializada.Clear();

            if ( _ventasProcesadas.Count == 0 )
            {
                GranTotalVenta = 0;
                GranTotalLitros = 0;
                return;
            }

            // Calcular Totales Globales
            GranTotalVenta = _ventasProcesadas.Sum(x => x.TotalVenta);
            GranTotalLitros = _ventasProcesadas.Sum(x => x.LitrosTotales);

            var grupos = _ventasProcesadas.GroupBy(x => x.Familia).ToList();

            // Llenar Arquitectónica
            foreach ( var nombre in ConfiguracionLineas.Arquitectonica )
            {
                var grupo = grupos.FirstOrDefault(g => g.Key == nombre);
                var tarjeta = grupo != null ? CrearTarjetaConDatos(grupo) : CrearTarjetaVacia(nombre);
                TarjetasArquitectonica.Add(tarjeta);
            }

            // Llenar Especializada
            foreach ( var nombre in ConfiguracionLineas.Especializada )
            {
                var grupo = grupos.FirstOrDefault(g => g.Key == nombre);
                var tarjeta = grupo != null ? CrearTarjetaConDatos(grupo) : CrearTarjetaVacia(nombre);
                TarjetasEspecializada.Add(tarjeta);
            }
        }
        private FamiliaResumenModel CrearTarjetaVacia(string nombre)
        {
            string color = _businessLogic.ObtenerColorFamilia(nombre);
            return new FamiliaResumenModel
            {
                NombreFamilia = nombre,
                VentaTotal = 0,
                LitrosTotales = 0,
                MejorCliente = "---",
                ProductoEstrella = "---",
                ColorFondo = color,
                ColorTexto = ColorHelper.ObtenerColorTextto(color)
            };
        }
        private FamiliaResumenModel CrearTarjetaConDatos(IGrouping<string, VentaReporteModel> grupo)
        {
            var topCliente = grupo.GroupBy(g => g.Cliente)
                                  .OrderByDescending(z => z.Sum(v => v.TotalVenta))
                                  .FirstOrDefault();

            var topProducto = grupo.GroupBy(g => g.Descripcion)
                                   .Select(g => new { Nombre = g.Key, Litros = g.Sum(x => x.LitrosTotales) })
                                   .OrderByDescending(x => x.Litros)
                                   .FirstOrDefault();

            string textoProd = topProducto != null ? $"{topProducto.Nombre}" : "N/A";
            string colorFondo = _businessLogic.ObtenerColorFamilia(grupo.Key);

            return new FamiliaResumenModel
            {
                NombreFamilia = grupo.Key,
                VentaTotal = grupo.Sum(x => x.TotalVenta),
                LitrosTotales = grupo.Sum(x => x.LitrosTotales),
                MejorCliente = topCliente?.Key ?? "Desconocido",
                ProductoEstrella = textoProd,
                ColorFondo = colorFondo,
                ColorTexto = ColorHelper.ObtenerColorTextto(colorFondo)
            };
        }
        private void CargarDetalle(string familia)
        {
            TituloDetalle = familia;

            // 1. Guardar los datos crudos de esta familia
            _datosFamiliaActual = _ventasProcesadas.Where(x => x.Familia == familia).ToList();

            // 2. Llenar el selector de Sub-Líneas (Ecopar, Polipar, etc.)
            SubLineasDisponibles.Clear();
            SubLineasDisponibles.Add("TODAS"); // Opción por defecto

            var lineasEncontradas = _datosFamiliaActual
                                    .Select(x => x.Linea)
                                    .Distinct()
                                    .OrderBy(x => x)
                                    .ToList();

            foreach ( var l in lineasEncontradas ) SubLineasDisponibles.Add(l);

            // 3. Seleccionar "TODAS" por defecto (esto dispara ActualizarGraficosPorSubLinea)
            SubLineaSeleccionada = "TODAS";

            VerResumen = false;
        }
        private void CalcularResumenPorLineas(List<VentaReporteModel> ventasFamilia)
        {
            var gruposLinea = ventasFamilia
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

            var top5 = gruposLinea.Take(5).ToList();
            SeriesDetalle = top5.Select(x => new PieSeries<decimal>
            {
                Values = new decimal[] { x.VentaTotal },
                Name = x.NombreLinea,
                DataLabelsPaint = new SolidColorPaint(SKColors.White),
                DataLabelsSize = 10,
                DataLabelsPosition = ( LiveChartsCore.Measure.PolarLabelsPosition )  LiveChartsCore.Measure.DataLabelsPosition.Middle ,
                DataLabelsFormatter = p => $"{p.Model:C0}",
                ToolTipLabelFormatter = (point) => $"{point.Context.Series.Name}: {point.Model:C2}"
            }).ToArray();
        }
        public SolidColorPaint TooltipTextPaint { get; set; } = new SolidColorPaint
        {
            Color = SKColors.Black,
            SKTypeface = SKTypeface.FromFamilyName("Arial")
        };
        private void CalcularTendenciaAnual(string familia)
        {
            var ventasFamiliaAnual = _datosAnualesCache
                .Where(x => x.Familia == familia)
                .GroupBy(x => x.FechaEmision.Month)
                .Select(g => new { Mes = g.Key, Total = g.Sum(v => v.TotalVenta) })
                .ToList();

            var valores = new decimal[12];
            foreach ( var v in ventasFamiliaAnual ) valores[v.Mes - 1] = v.Total;

            SeriesTendencia = new ISeries[]
            {
                new LineSeries<decimal>
                {
                    Name = "Tendencia",
                    Values = valores,
                    Stroke = new SolidColorPaint(SKColors.Orange) { StrokeThickness = 3 },
                    GeometryFill = new SolidColorPaint(SKColors.Orange),
                    GeometrySize = 8,
                    Fill = null,
                    XToolTipLabelFormatter = (p) => $"{p.Model:C0}"
                }
            };

            EjeXTendencia = new Axis[] { new Axis { Labels = new[] { "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" } } };
            EjeYTendencia = new Axis[] { new Axis { Labeler = v => v.ToString("C0"), TextSize = 12 } };

            OnPropertyChanged(nameof(SeriesTendencia));
            OnPropertyChanged(nameof(EjeXTendencia));
            OnPropertyChanged(nameof(EjeYTendencia));
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
        private string LimpiarParaCsv(string texto)
        {
            if ( string.IsNullOrEmpty(texto) ) return "";
            return $"\"{texto.Replace("\"", "\"\"")}\"";
        }
        private void GenerarReporteExcel()
        {
            if ( DetalleVentas == null || DetalleVentas.Count == 0 )
            {
                _dialogService.ShowMessage("No hay datos para exportar.", "Aviso");
                return;
            }

            string nombreArchivo = $"Reporte_{Filters.SucursalId}_{TituloDetalle.Replace(":", "")}.csv";
            string rutaGuardado = _dialogService.ShowSaveFileDialog("Archivo CSV (*.csv)|*.csv", nombreArchivo);

            if ( !string.IsNullOrEmpty(rutaGuardado) )
            {
                try
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("Fecha,Sucursal,Folio,Clave,Producto,Familia,Cliente,Cantidad,Litros Totales,Total Venta");

                    foreach ( var v in DetalleVentas )
                    {
                        string fila = $"{v.FechaEmision:dd/MM/yyyy},{v.Sucursal},{v.MovID},{v.Articulo},{LimpiarParaCsv(v.Descripcion)},{LimpiarParaCsv(v.Familia)},{LimpiarParaCsv(v.Cliente)},{v.Cantidad},{v.LitrosTotales},{v.TotalVenta}";
                        sb.AppendLine(fila);
                    }

                    File.WriteAllText(rutaGuardado, sb.ToString(), Encoding.UTF8);
                    _snackbarService.Show("✅ Reporte exportado correctamente");
                }
                catch ( Exception ex )
                {
                    _dialogService.ShowError($"Error al exportar: {ex.Message}", "Error");
                }
            }
        }
        private List<string> ObtenerFamiliasBase()
        {
            if ( _lineaActual == "Arquitectonica" ) return ConfiguracionLineas.Arquitectonica;
            if ( _lineaActual == "Especializada" ) return ConfiguracionLineas.Especializada;
            return ConfiguracionLineas.ObtenerTodas();
        }
        // MÉTODO PARA ACTUALIZAR GRÁFICOS (Actualizado para filtrar Tendencias)
        private void ActualizarGraficosPorSubLinea()
        {
            if ( _datosFamiliaActual == null ) return;

            var datosFiltrados = _datosFamiliaActual;

            // Filtro principal de datos (Tabla y Pastel)
            if ( SubLineaSeleccionada != "TODAS" && !string.IsNullOrEmpty(SubLineaSeleccionada) )
            {
                datosFiltrados = _datosFamiliaActual.Where(x => x.Linea == SubLineaSeleccionada).ToList();
            }

            // 1. Actualizar Tabla
            DetalleVentas = new ObservableCollection<VentaReporteModel>(
                datosFiltrados.OrderByDescending(x => x.TotalVenta)
            );

            // 2. Actualizar Gráfico Pastel y Ranking
            CalcularResumenPorLineas(datosFiltrados);

            // 3. Actualizar Gráfico de Tendencia (Mensual)
            // Pasamos el filtro seleccionado para que la línea también cambie
            CalcularTendenciaAnual(TituloDetalle, SubLineaSeleccionada);
        }
        private void CalcularTendenciaAnual(string familia, string subLineaFiltro)
        {
            if ( _datosAnualesCache == null || !_datosAnualesCache.Any() )
            {
                // Si aún no carga, limpiar gráfico
                SeriesTendencia = Array.Empty<ISeries>();
                return;
            }

            // Filtramos el histórico global por la familia actual
            IEnumerable<VentaReporteModel> source = _datosAnualesCache.Where(x => x.Familia == familia);

            // Y si hay una sub-línea seleccionada (ej. Ecopar), filtramos también por ella
            if ( subLineaFiltro != "TODAS" && !string.IsNullOrEmpty(subLineaFiltro) )
            {
                source = source.Where(x => x.Linea == subLineaFiltro);
            }

            var ventasFamiliaAnual = source
                .GroupBy(x => x.FechaEmision.Month)
                .Select(g => new { Mes = g.Key, Total = g.Sum(v => v.TotalVenta) })
                .ToList();

            // ... (El resto de la lógica de llenado del gráfico sigue igual) ...
            // Solo asegúrate de usar 'ventasFamiliaAnual' que ya está filtrado

            // (Ejemplo rápido de rellenado)
            var valores = new ObservableCollection<double>();
            for ( int i = 1; i <= 12; i++ )
            {
                var mesDatos = ventasFamiliaAnual.FirstOrDefault(x => x.Mes == i);
                valores.Add(mesDatos != null ? ( double ) mesDatos.Total : 0);
            }

            SeriesTendencia = new ISeries[]
                {
                    new LineSeries<double>
                        {
                            Values = valores,
                            Name = "Venta Mensual",
                            Stroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 3 },
                            Fill = null,
                            GeometrySize = 10
                        }
                };

            // Configurar Ejes X e Y...
            EjeXTendencia = new Axis[] { new Axis { Labels = new[] { "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" } } };
            EjeYTendencia = new Axis[] { new Axis { Labeler = value => value.ToString("C0") } };
        }
        public class DatoRanking
        {
            public string Nombre { get; set; }
            public decimal Valor { get; set; }
            public double Porcentaje { get; set; }
        }
    }
}