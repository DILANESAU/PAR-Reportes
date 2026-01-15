using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;

using SkiaSharp;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using WPF_PAR.Core;
using WPF_PAR.MVVM.Models;
using WPF_PAR.Services;
using WPF_PAR.Services.Interfaces;

namespace WPF_PAR.MVVM.ViewModels
{
    public class ClientesViewModel : ObservableObject
    {
        private readonly ClientesService _clientesService;
        private readonly ChartService _chartService;
        private readonly IDialogService _dialogService;
        public FilterService Filters { get; }

        // --- DATOS PRINCIPALES ---
        private List<ClienteAnalisisModel> _todosLosDatos; // Copia maestra para filtrar sin ir a SQL
        private ObservableCollection<ClienteAnalisisModel> _listaClientes;
        public ObservableCollection<ClienteAnalisisModel> ListaClientes
        {
            get => _listaClientes;
            set { _listaClientes = value; OnPropertyChanged(); }
        }

        // --- CLIENTE SELECCIONADO ---
        private ClienteAnalisisModel _clienteSeleccionado;
        public ClienteAnalisisModel ClienteSeleccionado
        {
            get => _clienteSeleccionado;
            set
            {
                _clienteSeleccionado = value;
                OnPropertyChanged();
                // Al seleccionar, cargamos KPIs, Productos y actualizamos la gráfica
                if ( value != null ) CargarDetalleCliente();
            }
        }

        // --- CONTROL DE VISTAS (TABLA PIVOTE) ---
        public ObservableCollection<string> ModosVista { get; set; } = new ObservableCollection<string>();

        private string _modoSeleccionado;
        public string ModoSeleccionado
        {
            get => _modoSeleccionado;
            set
            {
                _modoSeleccionado = value;
                OnPropertyChanged();
                CambiarVista(); // <--- ESTO ACTUALIZA TABLA Y GRÁFICA
            }
        }
        public string TituloColumnaActual
        {
            get
            {
                // Si el año seleccionado es el actual (ej. 2026)
                if ( AnioSeleccionado == DateTime.Now.Year )
                {
                    // Obtenemos el nombre del mes actual (ej. "ENE", "FEB")
                    string mesActual = DateTime.Now.ToString("MMM", CultureInfo.CurrentCulture).ToUpper().Replace(".", "");
                    return $"ACUM. {mesActual} {AnioSeleccionado}"; // Ej: "ACUM. ENE 2026"
                }

                // Si es un año pasado, mostramos "TOTAL"
                return $"TOTAL {AnioSeleccionado}";
            }
        }
        public string TituloColumnaAnterior
        {
            get
            {
                // Si el año seleccionado es el actual, el comparativo también debe indicar que es acumulado
                if ( AnioSeleccionado == DateTime.Now.Year )
                {
                    string mesActual = DateTime.Now.ToString("MMM", CultureInfo.CurrentCulture).ToUpper().Replace(".", "");
                    return $"ACUM. {mesActual} {AnioSeleccionado - 1}"; // Ej: "ACUM. ENE 2025"
                }

                return $"TOTAL {AnioSeleccionado - 1}";
            }
        }
        public int AnioSeleccionado
        {
            get => _anioSeleccionado;
            set
            {
                _anioSeleccionado = value;
                OnPropertyChanged();

                // Notificar cambio en TODOS los títulos
                OnPropertyChanged(nameof(TituloColumnaActual));
                OnPropertyChanged(nameof(TituloColumnaAnterior));
                OnPropertyChanged(nameof(TituloSem1));
                OnPropertyChanged(nameof(TituloSem2));
                OnPropertyChanged(nameof(TituloTri1));
                OnPropertyChanged(nameof(TituloTri2));
                OnPropertyChanged(nameof(TituloTri3));
                OnPropertyChanged(nameof(TituloTri4));

                ActualizarModosDisponibles(); // Tu lógica de bloqueo
                if ( !_isLoading ) CargarDatosIniciales();
            }
        }

        private void ActualizarModosDisponibles()
        {
            // Guardamos lo que estaba seleccionado para intentar mantenerlo
            string seleccionPrevia = ModoSeleccionado;

            ModosVista.Clear();
            ModosVista.Add("Anual"); // Anual siempre disponible (muestra YTD)

            bool esAnoActual = ( AnioSeleccionado == DateTime.Now.Year );
            int mesActual = DateTime.Now.Month;

            if ( !esAnoActual )
            {
                // Si es año pasado, mostramos todo
                ModosVista.Add("Semestral");
                ModosVista.Add("Trimestral");
            }
            else
            {
                // SI ES AÑO ACTUAL, APLICAMOS RESTRICCIONES

                // Semestral: Solo si ya pasó Junio (Mes > 6)
                // Opcional: Si quieres permitir ver el "Semestre en curso", cambia a >= 1
                if ( mesActual > 6 )
                {
                    ModosVista.Add("Semestral");
                }

                // Trimestral: Solo si ya pasó Marzo (Mes > 3)
                if ( mesActual > 3 )
                {
                    ModosVista.Add("Trimestral");
                }
            }

            // Restaurar selección o forzar Anual si la opción desapareció
            if ( ModosVista.Contains(seleccionPrevia) )
            {
                ModoSeleccionado = seleccionPrevia;
            }
            else
            {
                ModoSeleccionado = "Anual";
            }
        }

        // --- VISIBILIDAD DE COLUMNAS (Para el XAML) ---
        private bool _verAnual; public bool VerAnual { get => _verAnual; set { _verAnual = value; OnPropertyChanged(); } }
        private bool _verSemestral; public bool VerSemestral { get => _verSemestral; set { _verSemestral = value; OnPropertyChanged(); } }
        private bool _verTrimestral; public bool VerTrimestral { get => _verTrimestral; set { _verTrimestral = value; OnPropertyChanged(); } }

        // --- FILTROS ---
        public List<int> AñosDisponibles { get; set; }
        private int _anioSeleccionado;
        private string _textoBusqueda;
        public string TextoBusqueda
        {
            get => _textoBusqueda;
            set
            {
                _textoBusqueda = value;
                OnPropertyChanged();
                AplicarFiltroVisual();
            }
        }

        // --- DETALLES (KPIs y Productos) ---
        private KpiClienteModel _kpisCliente;
        public KpiClienteModel KpisCliente { get => _kpisCliente; set { _kpisCliente = value; OnPropertyChanged(); } }

        public ObservableCollection<ProductoAnalisisModel> ProductosEnDeclive { get; set; } = new ObservableCollection<ProductoAnalisisModel>();
        public ObservableCollection<ProductoAnalisisModel> ProductosEnAumento { get; set; } = new ObservableCollection<ProductoAnalisisModel>();


        // --- GRÁFICA ---
        public ISeries[] SeriesGrafica { get; set; }
        public Axis[] EjeXGrafica { get; set; }
        public Axis[] EjeYGrafica { get; set; }

        public string TituloSem1 => $"SEM 1 {AnioSeleccionado}";
        public string TituloSem2 => $"SEM 2 {AnioSeleccionado}";

        public string TituloTri1 => $"TRI 1 {AnioSeleccionado}";
        public string TituloTri2 => $"TRI 2 {AnioSeleccionado}";
        public string TituloTri3 => $"TRI 3 {AnioSeleccionado}";
        public string TituloTri4 => $"TRI 4 {AnioSeleccionado}";

        private bool _isLoading;
        public bool IsLoading { get => _isLoading; set { _isLoading = value; OnPropertyChanged(); } }

        public RelayCommand ActualizarCommand { get; set; }
        public ClientesViewModel(
            ClientesService clientesService,
            ChartService chartService,
            FilterService filterService,
            IDialogService dialogService)
        {
            _clientesService = clientesService;
            _chartService = chartService;
            Filters = filterService;
            _dialogService = dialogService;

            ListaClientes = new ObservableCollection<ClienteAnalisisModel>();

            int actual = DateTime.Now.Year;
            AñosDisponibles = new List<int> { actual, actual - 1, actual - 2 };
            _anioSeleccionado = actual;
            ActualizarModosDisponibles();
            _modoSeleccionado = "Anual";

            ActualizarCommand = new RelayCommand(o => CargarDatosIniciales());
            Filters.OnFiltrosCambiados += CargarDatosIniciales;
        }
        public async void CargarDatosIniciales()
        {
            IsLoading = true;
            try
            {
                var datos = await _clientesService.ObtenerDatosBase(AnioSeleccionado, Filters.SucursalId);

                _todosLosDatos = datos;
                ListaClientes = new ObservableCollection<ClienteAnalisisModel>(datos);

                CambiarVista();

                ClienteSeleccionado = null;
                SeriesGrafica = null;
            }
            catch ( Exception ex )
            {
                _dialogService.ShowMessage("Error", "Error al cargar datos: " + ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CambiarVista()
        {
            VerAnual = false; VerSemestral = false; VerTrimestral = false;

            switch ( ModoSeleccionado )
            {
                case "Anual": VerAnual = true; break;
                case "Semestral": VerSemestral = true; break;
                case "Trimestral": VerTrimestral = true; break;
            }

            if ( ClienteSeleccionado != null )
            {
                ActualizarGraficaLateral();
            }
        }
        private async void CargarDetalleCliente()
        {
            if ( ClienteSeleccionado == null ) return;
            IsLoading = true;

            try
            {
                ActualizarGraficaLateral();

                KpisCliente = await _clientesService.ObtenerKpisCliente(
                    ClienteSeleccionado.Cliente, AnioSeleccionado, Filters.SucursalId);

                var productos = await _clientesService.ObtenerVariacionProductos(
                    ClienteSeleccionado.Cliente, AnioSeleccionado, Filters.SucursalId);

                ProductosEnDeclive = new ObservableCollection<ProductoAnalisisModel>(
                    productos.Where(x => x.Diferencia < 0).OrderBy(x => x.Diferencia).Take(5));

                ProductosEnAumento = new ObservableCollection<ProductoAnalisisModel>(
                    productos.Where(x => x.Diferencia > 0).OrderByDescending(x => x.Diferencia).Take(5));

                OnPropertyChanged(nameof(ProductosEnDeclive));
                OnPropertyChanged(nameof(ProductosEnAumento));
            }
            catch ( Exception ex )
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }
        private void ActualizarGraficaLateral()
        {
            if ( ClienteSeleccionado == null ) return;

            List<decimal> valores = new List<decimal>();
            string[] etiquetas = null;
            var c = ClienteSeleccionado;

            // LÓGICA CORREGIDA: Agrupamos los datos según el modo de vista
            switch ( ModoSeleccionado )
            {
                case "Anual":
                    // Muestra el detalle de los 12 meses (Curva suave)
                    valores.AddRange(c.VentasMensualesActual);
                    etiquetas = new[] { "ENE", "FEB", "MAR", "ABR", "MAY", "JUN", "JUL", "AGO", "SEP", "OCT", "NOV", "DIC" };
                    break;

                case "Semestral":
                    // Sumamos los meses para crear 2 puntos grandes (S1 y S2)
                    var s1 = c.VentasMensualesActual.Take(6).Sum();
                    var s2 = c.VentasMensualesActual.Skip(6).Take(6).Sum();

                    valores.Add(s1);
                    valores.Add(s2);
                    etiquetas = new[] { "SEM 1", "SEM 2" };
                    break;

                case "Trimestral":
                    // Sumamos bloques de 3 meses para crear 4 puntos (T1, T2, T3, T4)
                    var t1 = c.VentasMensualesActual.Take(3).Sum();
                    var t2 = c.VentasMensualesActual.Skip(3).Take(3).Sum();
                    var t3 = c.VentasMensualesActual.Skip(6).Take(3).Sum();
                    var t4 = c.VentasMensualesActual.Skip(9).Take(3).Sum();

                    valores.Add(t1); valores.Add(t2); valores.Add(t3); valores.Add(t4);
                    etiquetas = new[] { "TRI 1", "TRI 2", "TRI 3", "TRI 4" };
                    break;
            }

            // --- Configuración Visual (Escala y Estilo) ---
            decimal maxVal = valores.Any() ? valores.Max() : 0;
            double techo = ( double ) ( maxVal * 1.15m );

            EjeYGrafica = new Axis[]
            {
        new Axis
        {
            MinLimit = 0,
            MaxLimit = techo > 0 ? techo : 1,
            Labeler = v => v >= 1000000 ? $"{v/1000000:N1}M" : $"{v/1000:N0}K",
            TextSize = 10,
            ShowSeparatorLines = true
        }
            };

            SeriesGrafica = new ISeries[]
            {
        new LineSeries<decimal>
        {
            Values = valores,
            Fill = new SolidColorPaint(SKColors.DodgerBlue.WithAlpha(30)),
            Stroke = new SolidColorPaint(SKColors.DodgerBlue) { StrokeThickness = 4 },
            GeometrySize = 10, // Puntos un poco más grandes para que se noten en Sem/Tri
            GeometryStroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 3 },
            
            // OJO: LineSmoothness en 0 para Trimestral/Semestral se ve mejor (líneas rectas)
            // para que se note el cambio brusco entre periodos.
            LineSmoothness = ModoSeleccionado == "Anual" ? 1 : 0,

            DataLabelsPaint = new SolidColorPaint(SKColors.Black),
            DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top,
            DataLabelsFormatter = p => p.Model > 0 ? (p.Model >= 1000000 ? $"{p.Model/1000000:N1}M" : $"{p.Model/1000:N0}K") : ""
        }
            };

            EjeXGrafica = new Axis[]
            {
        new Axis { Labels = etiquetas, LabelsRotation = 0, TextSize = 11 }
            };

            OnPropertyChanged(nameof(SeriesGrafica));
            OnPropertyChanged(nameof(EjeXGrafica));
            OnPropertyChanged(nameof(EjeYGrafica));
        }
        private void AplicarFiltroVisual()
        {
            if ( _todosLosDatos == null ) return;

            if ( string.IsNullOrWhiteSpace(TextoBusqueda) )
            {
                ListaClientes = new ObservableCollection<ClienteAnalisisModel>(_todosLosDatos);
            }
            else
            {
                string q = TextoBusqueda.ToUpper();
                var filtrado = _todosLosDatos
                    .Where(x => x.Nombre.ToUpper().Contains(q) || x.Cliente.Contains(q))
                    .ToList();
                ListaClientes = new ObservableCollection<ClienteAnalisisModel>(filtrado);
            }
        }
    }
}