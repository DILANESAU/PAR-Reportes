using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;

using Microsoft.Win32;

using SkiaSharp;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Data;

using WPF_PAR.Core;
using WPF_PAR.MVVM.Models;
using WPF_PAR.Services;
using WPF_PAR.Services.Interfaces;

namespace WPF_PAR.MVVM.ViewModels
{
    public class FamiliaViewModel : ObservableObject
    {
        // Servicios
        private readonly ReportesService _reportesService;
        private readonly CatalogoService _catalogoService;
        private readonly SucursalesService _sucursalesService;
        private readonly IDialogService _dialogService;
        private readonly ISnackbarService _snackbarService;
        private readonly BusinessLogicService _businessLogic;

        // Propiedades Principales
        public ObservableCollection<FamiliaResumenModel> TarjetasFamilias { get; set; }
        public ObservableCollection<VentaReporteModel> DetalleVentas { get; set; }
        private List<VentaReporteModel> _ventasProcesadas;

        // Propiedades de Gráficos para el Detalle
        private ISeries[] _seriesDetalle;
        public ISeries[] SeriesDetalle
        {
            get => _seriesDetalle;
            set { _seriesDetalle = value; OnPropertyChanged(); }
        }

        private ObservableCollection<LineaResumenModel> _resumenLineas;
        public ObservableCollection<LineaResumenModel> ResumenLineas
        {
            get => _resumenLineas;
            set { _resumenLineas = value; OnPropertyChanged(); }
        }

        // Filtros y Estados (Igual que antes)
        public Dictionary<int, string> ListaSucursales { get; set; }
        private int _sucursalSeleccionadaId;
        public int SucursalSeleccionadaId { get => _sucursalSeleccionadaId; set { _sucursalSeleccionadaId = value; OnPropertyChanged(); } }

        public ObservableCollection<string> ListaAnios { get; set; }
        private string _anioSeleccionado;
        public string AnioSeleccionado { get => _anioSeleccionado; set { _anioSeleccionado = value; OnPropertyChanged(); } }

        public Dictionary<int, string> ListaMeses { get; set; }
        private int _mesSeleccionado;
        public int MesSeleccionado { get => _mesSeleccionado; set { _mesSeleccionado = value; OnPropertyChanged(); } }

        private bool _isLoading;
        public bool IsLoading { get => _isLoading; set { _isLoading = value; OnPropertyChanged(); } }

        private bool _verResumen = true;
        public bool VerResumen
        {
            get => _verResumen;
            set
            {
                _verResumen = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(VerDetalle));
            }
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

        // Comandos
        public RelayCommand ActualizarCommand { get; set; }
        public RelayCommand VerDetalleCommand { get; set; }
        public RelayCommand RegresarCommand { get; set; }
        public RelayCommand ExportarExcelCommand { get; set; }

        private string _lineaActual = "Todas"; // Para filtrar Arquitectónica/Especializada

        public FamiliaViewModel(IDialogService dialogService, ISnackbarService snackbarService, BusinessLogicService businessLogic)
        {
            _dialogService = dialogService;
            _snackbarService = snackbarService;
            _businessLogic = businessLogic;

            _reportesService = new ReportesService();
            _catalogoService = new CatalogoService(businessLogic);
            _sucursalesService = new SucursalesService();

            TarjetasFamilias = new ObservableCollection<FamiliaResumenModel>();
            DetalleVentas = new ObservableCollection<VentaReporteModel>();
            ResumenLineas = new ObservableCollection<LineaResumenModel>();
            _ventasProcesadas = new List<VentaReporteModel>();

            InicializarTarjetasVacias();
            CargarFiltrosIniciales();

            ActualizarCommand = new RelayCommand(o => EjecutarReporte());
            VerDetalleCommand = new RelayCommand(param => { if ( param is string familia ) CargarDetalle(familia); });
            RegresarCommand = new RelayCommand(o => VerResumen = true);
            ExportarExcelCommand = new RelayCommand(o => GenerarReporteExcel());
        }
        public void CargarPorLinea(string linea)
        {
            _lineaActual = linea;
            if ( _ventasProcesadas.Any() ) GenerarResumenVisual();
            else EjecutarReporte();
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
        private void CargarFiltrosIniciales()
        {
            ListaAnios = new ObservableCollection<string> { "2023", "2024", "2025" };
            AnioSeleccionado = DateTime.Now.Year.ToString();

            ListaMeses = new Dictionary<int, string>
            {
                {1, "Enero"}, {2, "Febrero"}, {3, "Marzo"}, {4, "Abril"},
                {5, "Mayo"}, {6, "Junio"}, {7, "Julio"}, {8, "Agosto"},
                {9, "Septiembre"}, {10, "Octubre"}, {11, "Noviembre"}, {12, "Diciembre"}
            };
            MesSeleccionado = DateTime.Now.Month;

            // 1. CARGAR TODAS DEL CSV
            var todasLasSucursales = _sucursalesService.CargarSucursales();

            // 2. FILTRAR SEGÚN PERMISOS DEL USUARIO
            if ( Session.UsuarioActual.SucursalesPermitidas == null || Session.UsuarioActual.SucursalesPermitidas.Count == 0 )
            {
                // Si es null o vacía, asumimos que es ADMIN (Ve todas)
                // Ojo: Si quieres que un usuario sin permisos no vea nada, cambia lógica aquí.
                ListaSucursales = todasLasSucursales;
            }
            else
            {
                // FILTRO MAGICO: Solo las que están en su lista de permisos
                ListaSucursales = todasLasSucursales
                    .Where(x => Session.UsuarioActual.SucursalesPermitidas.Contains(x.Key))
                    .ToDictionary(x => x.Key, x => x.Value);
            }

            // 3. SELECCIONAR LA SUCURSAL POR DEFECTO (Validando que tenga permiso)
            int sucursalGuardada = Properties.Settings.Default.SucursalDefaultId;

            // A. Si tiene permiso para su favorita, la ponemos
            if ( ListaSucursales.ContainsKey(sucursalGuardada) )
            {
                SucursalSeleccionadaId = sucursalGuardada;
            }
            // B. Si no (o es la primera vez), seleccionamos la primera de SU lista permitida
            else if ( ListaSucursales.Count > 0 )
            {
                SucursalSeleccionadaId = ListaSucursales.Keys.First();
            }
        }
        private async void EjecutarReporte()
        {
            IsLoading = true;
            try
            {
                var ventasRaw = await _reportesService.ObtenerVentasBrutas(AnioSeleccionado, SucursalSeleccionadaId.ToString(), MesSeleccionado);

                foreach ( var venta in ventasRaw )
                {
                    var info = _catalogoService.ObtenerInfo(venta.Articulo);

                    venta.Familia = info.FamiliaSimple;
                    venta.LitrosUnitarios = info.Litros;

                    // IMPORTANTE: Asignamos la Linea para poder agrupar en el detalle
                    venta.Linea = string.IsNullOrEmpty(info.Linea) ? "Otras" : info.Linea;

                    // CORRECCIÓN DESCRIPCIÓN:
                    if ( !string.IsNullOrEmpty(info.Descripcion) )
                        venta.Descripcion = info.Descripcion;
                    else
                        venta.Descripcion = "(Sin descripción disponible)";
                }

                _ventasProcesadas = ventasRaw;
                GenerarResumenVisual();
                VerResumen = true;
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
            if ( _ventasProcesadas.Count == 0 )
            {
                return;
            }
            var familiasOrdenadas = ObtenerFamiliasBase();
            var listaFinal = new List<FamiliaResumenModel>();
            var grupos = _ventasProcesadas.GroupBy(x => x.Familia).ToList();
            var familiasBase = ObtenerFamiliasBase();

            foreach ( var familia in familiasBase )
            {
                var grupo = grupos.FirstOrDefault(g => g.Key == familia);
                string color = _businessLogic.ObtenerColorFamilia(familia);

                if ( grupo != null )
                {
                    // Calcular tops
                    var topCliente = grupo.GroupBy(x => x.Cliente).OrderByDescending(g => g.Sum(v => v.TotalVenta)).FirstOrDefault();
                    var topProd = grupo.GroupBy(x => x.Descripcion).OrderByDescending(g => g.Sum(v => v.LitrosTotales)).FirstOrDefault();

                    TarjetasFamilias.Add(new FamiliaResumenModel
                    {
                        NombreFamilia = familia,
                        VentaTotal = grupo.Sum(x => x.TotalVenta),
                        LitrosTotales = grupo.Sum(x => x.LitrosTotales),
                        ColorFondo = color,
                        ColorTexto = ColorHelper.ObtenerColorTextto(color),
                        MejorCliente = topCliente?.Key ?? "---",
                        ProductoEstrella = topProd != null ? $"{topProd.Key}" : "---"
                    });
                }
                else
                {
                    // Tarjeta vacía
                    TarjetasFamilias.Add(new FamiliaResumenModel { NombreFamilia = familia, ColorFondo = color, ColorTexto = ColorHelper.ObtenerColorTextto(color), VentaTotal = 0, LitrosTotales = 0, MejorCliente = "---", ProductoEstrella = "---" });
                }
            }
        }
        private void CargarDetalle(string familia)
        {
            TituloDetalle = $"{familia}";

            // 1. Filtrar los datos crudos para la tabla
            var filtrado = _ventasProcesadas
                           .Where(x => x.Familia == familia)
                           .OrderByDescending(x => x.TotalVenta)
                           .ToList();

            DetalleVentas = new ObservableCollection<VentaReporteModel>(filtrado);

            // 2. Calcular Resumen por LÍNEA (El "Dashboard" interno)
            CalcularResumenPorLineas(filtrado);

            // 3. Cambiar vista
            VerResumen = false;
        }
        private void CalcularResumenPorLineas(List<VentaReporteModel> ventasFamilia)
        {
            // 1. Agrupar por LÍNEA (Ej: "Esmalte Secado Rapido")
            var gruposLinea = ventasFamilia
                .GroupBy(x => x.Linea)
                .Select(g => new LineaResumenModel
                {
                    NombreLinea = g.Key,
                    VentaTotal = g.Sum(x => x.TotalVenta),
                    LitrosTotales = g.Sum(x => x.LitrosTotales),
                    // Calculamos el producto más vendido de esa línea
                    ProductoTop = g.GroupBy(p => p.Descripcion)
                                   .OrderByDescending(gp => gp.Sum(v => v.LitrosTotales))
                                   .FirstOrDefault()?.Key ?? "Sin datos"
                })
                .OrderByDescending(x => x.VentaTotal)
                .ToList();

            ResumenLineas = new ObservableCollection<LineaResumenModel>(gruposLinea);

            // 2. Generar Gráfico (Top 5 Líneas para no saturar)
            var top5 = gruposLinea.Take(5).ToList();

            SeriesDetalle = top5.Select(x => new PieSeries<decimal>
            {
                Values = new decimal[] { x.VentaTotal },
                Name = x.NombreLinea,
                DataLabelsPaint = new SolidColorPaint(SKColors.White),
                DataLabelsSize = 10,
                DataLabelsPosition = ( LiveChartsCore.Measure.PolarLabelsPosition )  LiveChartsCore.Measure.DataLabelsPosition.Middle ,

                // --- CORRECCIÓN AQUÍ ---
                // Usamos p.Model porque el "Modelo" es el decimal mismo.
                DataLabelsFormatter = p => $"{p.Model:C0}"
                // -----------------------

            }).ToArray();

            OnPropertyChanged(nameof(SeriesDetalle));
        }
        private void GenerarReporteExcel()
        {
            if ( DetalleVentas == null || DetalleVentas.Count == 0 )
            {
                // REFACTORIZADO
                _dialogService.ShowMessage("No hay datos para exportar.", "Aviso");
                return;
            }

            string nombreArchivo = $"Reporte_{SucursalSeleccionadaId}_{AnioSeleccionado}_{MesSeleccionado}_{TituloDetalle.Replace(":", "")}.csv";

            string rutaGuardado = _dialogService.ShowSaveFileDialog("Archivo CSV (*.csv)|*.csv", nombreArchivo);

            if ( !string.IsNullOrEmpty(rutaGuardado) )
            {
                try
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("Fecha,Sucursal,Folio,Clave,Producto,Familia,Cliente,Cantidad,Litros Totales,Total Venta");

                    foreach ( var v in DetalleVentas )
                    {
                        string cliente = v.Cliente?.Replace(",", " ") ?? "";
                        string familia = v.Familia?.Replace(",", " ") ?? "";
                        string desc = v.Descripcion?.Replace(",", " ") ?? "";

                        string linea = $"{v.FechaEmision:dd/MM/yyyy},{v.Sucursal},{v.MovID},{v.Articulo},{desc},{familia},{cliente},{v.Cantidad},{v.LitrosTotales},{v.TotalVenta}";
                        sb.AppendLine(linea);
                    }

                    File.WriteAllText(rutaGuardado, sb.ToString(), Encoding.UTF8);

                    _snackbarService.Show("✅ Reporte exportado correctamente a Excel");
                }
                catch ( Exception )
                {
                    _snackbarService.Show("Hubo un error en el guardado del archivo");
                }
            }
        }
        private List<string> ObtenerFamiliasBase()
        {
            // (Tu implementación existente que usa ConfiguracionLineas)
            if ( _lineaActual == "Arquitectonica" ) return ConfiguracionLineas.Arquitectonica;
            if ( _lineaActual == "Especializada" ) return ConfiguracionLineas.Especializada;
            return ConfiguracionLineas.ObtenerTodas();
        }
        private void FiltrarTabla()
        {
            if ( DetalleVentas == null ) return;
            ICollectionView view = CollectionViewSource.GetDefaultView(DetalleVentas);
            if ( string.IsNullOrWhiteSpace(TextoBusqueda) ) view.Filter = null;
            else view.Filter = (obj) => {
                var v = obj as VentaReporteModel;
                if ( v == null ) return false;
                string t = TextoBusqueda.ToUpper();
                return ( v.Cliente?.ToUpper().Contains(t) ?? false ) ||
                       ( v.Descripcion?.ToUpper().Contains(t) ?? false ) ||
                       ( v.Linea?.ToUpper().Contains(t) ?? false ); // Agregamos búsqueda por Línea
            };
        }
    }
}
