using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Data;

using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;

using SkiaSharp;

using WPF_PAR.Core;
using WPF_PAR.MVVM.Models;
using WPF_PAR.Services;
using WPF_PAR.Services.Interfaces;

namespace WPF_PAR.MVVM.ViewModels
{
    public class FamiliaViewModel : ObservableObject
    {
        // --- SERVICIOS ---
        private readonly ReportesService _reportesService;
        private readonly CatalogoService _catalogoService;
        private readonly IDialogService _dialogService;
        private readonly ISnackbarService _snackbarService;
        private readonly BusinessLogicService _businessLogic;
        public FilterService _filters { get; }

        // --- COLECCIONES PRINCIPALES ---
        private ObservableCollection<FamiliaResumenModel> _tarjetasFamilias;
        public ObservableCollection<FamiliaResumenModel> TarjetasFamilias
        {
            get => _tarjetasFamilias;
            set { _tarjetasFamilias = value; OnPropertyChanged(); }
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

        // --- DATOS EN MEMORIA ---
        private List<VentaReporteModel> _ventasProcesadas;
        private List<VentaReporteModel> _datosAnualesCache; // Para tendencia

        // --- GRÁFICOS DETALLE ---
        private ISeries[] _seriesDetalle;
        public ISeries[] SeriesDetalle { get => _seriesDetalle; set { _seriesDetalle = value; OnPropertyChanged(); } }

        public ISeries[] SeriesTendencia { get; set; }
        public Axis[] EjeXTendencia { get; set; }
        public Axis[] EjeYTendencia { get; set; }

        // --- ESTADO UI ---
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

        private string _lineaActual = "Todas";

        // --- COMANDOS ---
        public RelayCommand ActualizarCommand { get; set; }
        public RelayCommand VerDetalleCommand { get; set; }
        public RelayCommand RegresarCommand { get; set; }
        public RelayCommand ExportarExcelCommand { get; set; }


        // --- CONSTRUCTOR ---
        public FamiliaViewModel(IDialogService dialogService, ISnackbarService snackbarService, BusinessLogicService businessLogic, FilterService filterService)
        {
            _dialogService = dialogService;
            _snackbarService = snackbarService;
            _businessLogic = businessLogic;
            _filters = filterService;

            _reportesService = new ReportesService();
            _catalogoService = new CatalogoService(businessLogic);

            TarjetasFamilias = new ObservableCollection<FamiliaResumenModel>();
            DetalleVentas = new ObservableCollection<VentaReporteModel>();
            ResumenLineas = new ObservableCollection<LineaResumenModel>();

            _ventasProcesadas = new List<VentaReporteModel>();
            _datosAnualesCache = new List<VentaReporteModel>();

            InicializarTarjetasVacias();

            // SUSCRIPCIÓN AL FILTRO GLOBAL
            _filters.OnFiltrosCambiados += () =>
            {
                if ( !VerResumen ) VerResumen = true; // Volver al inicio al cambiar filtros
                EjecutarReporte();
            };

            ActualizarCommand = new RelayCommand(o => EjecutarReporte());

            VerDetalleCommand = new RelayCommand(param =>
            {
                if ( param is string familia ) CargarDetalle(familia);
            });

            RegresarCommand = new RelayCommand(o => VerResumen = true);
            ExportarExcelCommand = new RelayCommand(o => GenerarReporteExcel());

            // Carga inicial
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
                // 1. Obtener Datos del Periodo (Usando filtros globales)
                var ventasRaw = await _reportesService.ObtenerVentasBrutasRango(
                    _filters.SucursalId,
                    _filters.FechaInicio,
                    _filters.FechaFin
                );

                // 2. Enriquecer datos con Catálogo CSV
                foreach ( var venta in ventasRaw )
                {
                    var info = _catalogoService.ObtenerInfo(venta.Articulo);

                    venta.Familia = info.FamiliaSimple;
                    venta.LitrosUnitarios = info.Litros;
                    venta.Linea = string.IsNullOrEmpty(info.Linea) ? "Otras" : info.Linea;

                    if ( !string.IsNullOrEmpty(info.Descripcion) )
                        venta.Descripcion = info.Descripcion;
                    else
                        venta.Descripcion = "(Sin descripción disponible)";
                }

                _ventasProcesadas = ventasRaw;
                GenerarResumenVisual();

                // 3. Cargar Histórico Anual (Para tendencias) - Segundo plano
                _datosAnualesCache = await _reportesService.ObtenerHistoricoAnualPorArticulo(
                    _filters.FechaFin.Year.ToString(),
                    _filters.SucursalId.ToString()
                );

                // Mapear familias al histórico también
                foreach ( var item in _datosAnualesCache )
                {
                    var info = _catalogoService.ObtenerInfo(item.Articulo);
                    item.Familia = info.FamiliaSimple;
                }
            }
            catch ( Exception ex )
            {
                _dialogService.ShowError($"Error al generar reporte: {ex.Message}", "Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void GenerarResumenVisual()
        {
            TarjetasFamilias.Clear();
            if ( _ventasProcesadas.Count == 0 ) return;

            var grupos = _ventasProcesadas.GroupBy(x => x.Familia).ToList();
            var familiasMostrar = ObtenerFamiliasBase();

            foreach ( var nombreFamilia in familiasMostrar )
            {
                var grupoDatos = grupos.FirstOrDefault(g => g.Key == nombreFamilia);
                if ( grupoDatos != null )
                {
                    TarjetasFamilias.Add(CrearTarjetaConDatos(grupoDatos));
                }
                else
                {
                    string color = _businessLogic.ObtenerColorFamilia(nombreFamilia);
                    TarjetasFamilias.Add(new FamiliaResumenModel
                    {
                        NombreFamilia = nombreFamilia,
                        VentaTotal = 0,
                        LitrosTotales = 0,
                        MejorCliente = "---",
                        ProductoEstrella = "---",
                        ColorFondo = color,
                        ColorTexto = ColorHelper.ObtenerColorTextto(color)
                    });
                }
            }
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
            TituloDetalle = $"{familia}";

            var filtrado = _ventasProcesadas
                           .Where(x => x.Familia == familia)
                           .OrderByDescending(x => x.TotalVenta)
                           .ToList();

            DetalleVentas = new ObservableCollection<VentaReporteModel>(filtrado);

            CalcularResumenPorLineas(filtrado);
            CalcularTendenciaAnual(familia);

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

            string nombreArchivo = $"Reporte_{_filters.SucursalId}_{TituloDetalle.Replace(":", "")}.csv";
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
    }
}