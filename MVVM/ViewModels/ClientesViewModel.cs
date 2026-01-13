using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;

using SkiaSharp;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public List<string> ModosVista { get; } = new List<string> { "Anual", "Semestral", "Trimestral" };

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
        public string TituloColumnaActual => $"TOTAL {AnioSeleccionado}";
        public string TituloColumnaAnterior => $"TOTAL {AnioSeleccionado - 1}";
        public int AnioSeleccionado
        {
            get => _anioSeleccionado;
            set
            {
                _anioSeleccionado = value;
                OnPropertyChanged();

                // AGREGAR ESTAS DOS LÍNEAS PARA ACTUALIZAR LOS TÍTULOS
                OnPropertyChanged(nameof(TituloColumnaActual));
                OnPropertyChanged(nameof(TituloColumnaAnterior));

                if ( !_isLoading ) CargarDatosIniciales();
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
        public Axis[] EjeYGrafica { get; set; } // Nuevo para el escalado

        private bool _isLoading;
        public bool IsLoading { get => _isLoading; set { _isLoading = value; OnPropertyChanged(); } }

        public RelayCommand ActualizarCommand { get; set; }

        // =========================================================
        // CONSTRUCTOR
        // =========================================================
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

            // Configurar Años
            int actual = DateTime.Now.Year;
            AñosDisponibles = new List<int> { actual, actual - 1, actual - 2 };
            _anioSeleccionado = actual; // Ojo: Asegúrate que sea un año con datos (ej. 2025)

            // Configurar Modo Inicial
            _modoSeleccionado = "Semestral";

            ActualizarCommand = new RelayCommand(o => CargarDatosIniciales());
            Filters.OnFiltrosCambiados += CargarDatosIniciales;
        }

        // =========================================================
        // CARGA DE DATOS MAESTROS
        // =========================================================
        public async void CargarDatosIniciales()
        {
            IsLoading = true;
            try
            {
                // 1. Traemos TODOS los datos mensuales de una vez
                var datos = await _clientesService.ObtenerDatosBase(AnioSeleccionado, Filters.SucursalId);

                _todosLosDatos = datos;
                ListaClientes = new ObservableCollection<ClienteAnalisisModel>(datos);

                // 2. Aplicamos la vista actual (Configura columnas)
                CambiarVista();

                // Limpiamos selección
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

        // =========================================================
        // LÓGICA DE VISTA PIVOTE (TABLA + GRÁFICA)
        // =========================================================
        private void CambiarVista()
        {
            // A. CONFIGURAR COLUMNAS TABLA
            VerAnual = false; VerSemestral = false; VerTrimestral = false;

            switch ( ModoSeleccionado )
            {
                case "Anual": VerAnual = true; break;
                case "Semestral": VerSemestral = true; break;
                case "Trimestral": VerTrimestral = true; break;
            }

            // B. ACTUALIZAR GRÁFICA (Si hay un cliente seleccionado)
            if ( ClienteSeleccionado != null )
            {
                ActualizarGraficaLateral();
            }
        }

        // =========================================================
        // CARGA DE DETALLES (AL SELECCIONAR CLIENTE)
        // =========================================================
        private async void CargarDetalleCliente()
        {
            if ( ClienteSeleccionado == null ) return;
            IsLoading = true;

            try
            {
                // 1. Gráfica (Sincronizada con el modo de vista)
                ActualizarGraficaLateral();

                // 2. KPIs
                KpisCliente = await _clientesService.ObtenerKpisCliente(
                    ClienteSeleccionado.Cliente, AnioSeleccionado, Filters.SucursalId);

                // 3. Productos Top (Subidas y Bajadas)
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

        // =========================================================
        // GENERACIÓN DE GRÁFICA EN MEMORIA (SIN SQL)
        // =========================================================
        private void ActualizarGraficaLateral()
        {
            if ( ClienteSeleccionado == null ) return;

            List<decimal> valores = new List<decimal>();
            string[] etiquetas = null;
            var c = ClienteSeleccionado;

            // AQUÍ ESTÁ EL CAMBIO: SIEMPRE DESGLOSAMOS POR MESES
            // Para que la gráfica tenga "forma" y no sea un punto.
            switch ( ModoSeleccionado )
            {
                case "Anual":
                    // En lugar del total, mostramos los 12 meses
                    valores.AddRange(c.VentasMensualesActual);
                    etiquetas = new[] { "E", "F", "M", "A", "M", "J", "J", "A", "S", "O", "N", "D" };
                    break;

                case "Semestral":
                    // Mostramos los 6 meses del semestre correspondiente (no la suma S1/S2)
                    // Si quieres ser muy preciso, necesitarías saber cuál semestre se eligió.
                    // Como tu tabla muestra S1 y S2, aquí podríamos mostrar los 12 meses para ver el año completo
                    // O mostrar solo Ene-Jun y Jul-Dic como dos puntos si prefieres la simplicidad anterior.

                    // MI RECOMENDACIÓN: Muestra siempre el año completo (12 meses) en la gráfica
                    // para dar contexto, incluso si miras semestres.
                    valores.AddRange(c.VentasMensualesActual);
                    etiquetas = new[] { "E", "F", "M", "A", "M", "J", "J", "A", "S", "O", "N", "D" };
                    break;

                case "Trimestral":
                    // Igual, ver la tendencia de los 12 meses ayuda más.
                    valores.AddRange(c.VentasMensualesActual);
                    etiquetas = new[] { "E", "F", "M", "A", "M", "J", "J", "A", "S", "O", "N", "D" };
                    break;
            }

            // Configuración de escala Y (Igual que antes)
            decimal maxVal = valores.Any() ? valores.Max() : 0;
            double techo = ( double ) ( maxVal * 1.15m );

            EjeYGrafica = new Axis[]
            {
        new Axis
        {
            MinLimit = 0,
            MaxLimit = techo > 0 ? techo : 1, // Evitar división por cero
            Labeler = v => v >= 1000000 ? $"{v/1000000:N1}M" : $"{v/1000:N0}K",
            TextSize = 10,
            ShowSeparatorLines = true // Rejilla suave ayuda a leer
        }
            };

            SeriesGrafica = new ISeries[]
            {
        new LineSeries<decimal>
        {
            Values = valores,
            // DISEÑO VISUAL MEJORADO
            Fill = new SolidColorPaint(SKColors.DodgerBlue.WithAlpha(30)), // Relleno azul transparente abajo
            Stroke = new SolidColorPaint(SKColors.DodgerBlue) { StrokeThickness = 4 }, // Línea más gruesa
            GeometrySize = 8, // Puntos más discretos
            GeometryStroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 2 },
            LineSmoothness = 1, // <--- ESTO HACE LA LÍNEA CURVA (BEZIER) Y SE VE MUY MODERNO
            
            DataLabelsPaint = new SolidColorPaint(SKColors.Black),
            DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top,
            // Solo mostramos etiquetas si el valor no es 0 para no ensuciar
            DataLabelsFormatter = p => p.Model > 0 ? (p.Model >= 1000000 ? $"{p.Model/1000000:N1}M" : $"{p.Model/1000:N0}K") : ""
        }
            };

            EjeXGrafica = new Axis[]
            {
        new Axis { Labels = etiquetas, LabelsRotation = 0, TextSize = 10 }
            };

            OnPropertyChanged(nameof(SeriesGrafica));
            OnPropertyChanged(nameof(EjeXGrafica));
            OnPropertyChanged(nameof(EjeYGrafica));
        }

        // =========================================================
        // FILTRO VISUAL (BUSCADOR)
        // =========================================================
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