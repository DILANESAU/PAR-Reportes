using Microsoft.Win32;

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

namespace WPF_PAR.MVVM.ViewModels
{
    public class FamiliaViewModel : ObservableObject
    {
        private readonly ReportesService _reportesService;
        private readonly CatalogoService _catalogoService;
        private readonly SucursalesService _sucursalesService;
        public ObservableCollection<FamiliaResumenModel> TarjetasFamilias { get; set; }
        public ObservableCollection<VentaReporteModel> DetalleVentas { get; set; }
        private List<VentaReporteModel> _ventasProcesadas;
        public Dictionary<int, string> ListaSucursales { get; set; }
        private int _sucursalSeleccionadaId;
        public int SucursalSeleccionadaId
        {
            get => _sucursalSeleccionadaId;
            set { _sucursalSeleccionadaId = value; OnPropertyChanged(); }
        }
        private string _textoBusqueda;
        public string TextoBusqueda
        {
            get => _textoBusqueda;
            set
            {
                _textoBusqueda = value;
                OnPropertyChanged();
                FiltrarTabla();
            }
        }
        public ObservableCollection<string> ListaAnios { get; set; }
        private string _anioSeleccionado;
        public string AnioSeleccionado
        {
            get => _anioSeleccionado;
            set { _anioSeleccionado = value; OnPropertyChanged(); }
        }
        public Dictionary<int, string> ListaMeses { get; set; }
        private int _mesSeleccionado;
        public int MesSeleccionado
        {
            get => _mesSeleccionado;
            set { _mesSeleccionado = value; OnPropertyChanged(); }
        }
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }
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
        public string TituloDetalle
        {
            get => _tituloDetalle;
            set { _tituloDetalle = value; OnPropertyChanged(); }
        }
        private string _lineaActual = "Todas";
        public void CargarPorLinea(string linea)
        {
            _lineaActual = linea;
            if (_ventasProcesadas != null && _ventasProcesadas.Count > 0)
            {
                GenerarResumenVisual();

            }
            else 
                EjecutarReporte();
        }
        public RelayCommand ActualizarCommand { get; set; }
        public RelayCommand VerDetalleCommand { get; set; } 
        public RelayCommand RegresarCommand { get; set; }  
        public RelayCommand ExportarExcelCommand { get; set; } 
        public FamiliaViewModel()
        {
            _reportesService = new ReportesService();
            _catalogoService = new CatalogoService();
            _sucursalesService = new SucursalesService();

            TarjetasFamilias = new ObservableCollection<FamiliaResumenModel>();
            DetalleVentas = new ObservableCollection<VentaReporteModel>();
            _ventasProcesadas = new List<VentaReporteModel>();

            InicializarTarjetasVacias();
            CargarFiltrosIniciales();

            ActualizarCommand = new RelayCommand(o => EjecutarReporte());

            VerDetalleCommand = new RelayCommand(param =>
            {
                if ( param is string familia ) CargarDetalle(familia);
            });

            RegresarCommand = new RelayCommand(o => VerResumen = true);

            ExportarExcelCommand = new RelayCommand(o => GenerarReporteExcel());
        }
        private void InicializarTarjetasVacias()
        {
            TarjetasFamilias.Clear();
            var familiasOrdenadas = ObtenerFamiliasBase();

            foreach ( var nombre in familiasOrdenadas )
            {
                string colorFondo = ObtenerColorFamilia(nombre);

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
                var ventasRaw = await _reportesService.ObtenerVentasBrutas(
                    AnioSeleccionado,
                    SucursalSeleccionadaId.ToString(),
                    MesSeleccionado
                );

                foreach ( var venta in ventasRaw )
                {
                    var info = _catalogoService.ObtenerInfo(venta.Articulo);

                    venta.Familia = info.FamiliaSimple;  
                    venta.LitrosUnitarios = info.Litros;

                    venta.Descripcion = string.IsNullOrEmpty(info.Descripcion) ? venta.Articulo : info.Descripcion;
                }

                _ventasProcesadas = ventasRaw;

                GenerarResumenVisual();

                VerResumen = true;
            }
            catch ( Exception ex )
            {
                MessageBox.Show($"Error al generar reporte: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                //InicializarTarjetasVacias();
                return;
            }
            var grupos = _ventasProcesadas.GroupBy(x => x.Familia).ToList();

            var familiasOrdenadas = ObtenerFamiliasBase();

            var listaFinal = new List<FamiliaResumenModel>();

            List<string> familiasA_Mostrar;

            if ( _lineaActual == "Arquitectonica" )
                familiasA_Mostrar = ConfiguracionLineas.Arquitectonica;
            else if ( _lineaActual == "Especializada" )
                familiasA_Mostrar = ConfiguracionLineas.Especializada;
            else
                familiasA_Mostrar = ObtenerFamiliasBase();

                foreach ( var nombreFamilia in familiasA_Mostrar )
                {
                    var grupoDatos = grupos.FirstOrDefault(g => g.Key == nombreFamilia);

                    if ( grupoDatos != null )
                    {
                        listaFinal.Add(CrearTarjetaConDatos(grupoDatos));
                    }
                    else
                    {
                        string color = ObtenerColorFamilia(nombreFamilia);
                        listaFinal.Add(new FamiliaResumenModel
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

            foreach ( var grupo in grupos )
            {
                if ( !familiasOrdenadas.Contains(grupo.Key) )
                {
                    listaFinal.Add(CrearTarjetaConDatos(grupo));
                }
            }

            TarjetasFamilias.Clear();
            foreach ( var item in listaFinal ) TarjetasFamilias.Add(item);
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

            string textoProd = topProducto != null ? $"{topProducto.Nombre} ({topProducto.Litros:N0} Lts)" : "N/A";
            string colorFondo = ObtenerColorFamilia(grupo.Key);

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
            TituloDetalle = $"Detalle: {familia}";

            var filtrado = _ventasProcesadas
                           .Where(x => x.Familia == familia)
                           .OrderByDescending(x => x.TotalVenta);

            DetalleVentas = new ObservableCollection<VentaReporteModel>(filtrado);
            OnPropertyChanged(nameof(DetalleVentas));

            VerResumen = false;
        }
        private void GenerarReporteExcel()
        {
            if ( DetalleVentas == null || DetalleVentas.Count == 0 )
            {
                MessageBox.Show("No hay datos para exportar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Archivo CSV (*.csv)|*.csv",
                FileName = $"Reporte_{SucursalSeleccionadaId}_{AnioSeleccionado}_{MesSeleccionado}_{TituloDetalle.Replace(":", "")}.csv"
            };

            if ( saveFileDialog.ShowDialog() == true )
            {
                try
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("Fecha,Sucursal,Folio,Clave,Producto,Familia,Cliente,Cantidad,Litros Totales,Total Venta");

                    foreach ( var v in DetalleVentas )
                    {
                        // Limpiar comas para no romper el CSV
                        string cliente = v.Cliente.Replace(",", " ");
                        string familia = v.Familia.Replace(",", " ");

                        string linea = $"{v.FechaEmision:dd/MM/yyyy},{v.Sucursal},{v.MovID},{v.Articulo},{v.Descripcion},{familia},{cliente},{v.Cantidad},{v.LitrosTotales},{v.TotalVenta}";
                        sb.AppendLine(linea);
                    }

                    File.WriteAllText(saveFileDialog.FileName, sb.ToString(), Encoding.UTF8);
                    var snackbar = (Application.Current.MainWindow as MainWindow)?.FindName("MainSnackbar") as MaterialDesignThemes.Wpf.Snackbar;
                    if ( snackbar != null )
                    {
                        snackbar.MessageQueue?.Enqueue("✅ Reporte exportado correctamente a Excel");
                    }
                }
                catch ( Exception )
                {
                    var snackbar = ( Application.Current.MainWindow as MainWindow )?.FindName("MainSnackbar") as MaterialDesignThemes.Wpf.Snackbar;
                    if ( snackbar != null )
                    {
                        snackbar.MessageQueue?.Enqueue("Hubo un error en el guardado del archivo");
                    }
                }
            }
        }
        private string ObtenerColorFamilia(string familia)
        {

            if ( familia.Contains("Vinílica") ) return "#D19755"; 
            if ( familia.Contains("Esmalte") ) return "#4D1311";  
            if ( familia.Contains("Imper") ) return "#5d9a9f";   
            if ( familia.Contains("Sellador") ) return "#1F7045";
            if ( familia.Contains("Tráfico") ) return "#F9A825"; 
            if ( familia.Contains("Industrial") ) return "#664072";
            if ( familia.Contains("Accesorios") ) return "#ddaea6";
            if ( familia.Contains("Solventes") ) return "#005c4b";
            if ( familia.Contains("Ferretería") ) return "#4c85f3";
            return "#616161";
        }
        private List<string> ObtenerFamiliasBase()
        {
            switch ( _lineaActual )
            {
                case "Arquitectonica":
                    return ConfiguracionLineas.Arquitectonica;
                case "Especializada":
                    return ConfiguracionLineas.Especializada;

                default:
                    return ConfiguracionLineas.ObtenerTodas();
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
                    var venta = obj as VentaReporteModel;
                    if ( venta == null ) return false;

                    string texto = TextoBusqueda.ToUpper();
                    return ( venta.Cliente != null && venta.Cliente.ToUpper().Contains(texto) ) ||
                           ( venta.MovID != null && venta.MovID.Contains(texto) ) ||
                           ( venta.Articulo != null && venta.Articulo.Contains(texto) ) ||
                           ( venta.Descripcion != null && venta.Descripcion.ToUpper().Contains(texto) );
                };
            }
        }
    }
}
