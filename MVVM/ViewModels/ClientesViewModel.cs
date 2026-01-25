using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;

using SkiaSharp;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using WPF_PAR.Core;
using WPF_PAR.MVVM.Models;
using WPF_PAR.Services;
using WPF_PAR.Services.Interfaces;

namespace WPF_PAR.MVVM.ViewModels
{
    public class ClientesViewModel : ObservableObject
    {
        private readonly ReportesService _reportesService;
        private readonly ClientesLogicService _logicService;
        private readonly IDialogService _dialogService;
        public FilterService Filters { get; }

        // --- FILTROS DE AÑO (NUEVO) ---
        public ObservableCollection<int> AñosDisponibles { get; set; }
        private int _anioSeleccionado;
        public int AnioSeleccionado
        {
            get => _anioSeleccionado;
            set
            {
                if ( _anioSeleccionado != value )
                {
                    _anioSeleccionado = value;
                    OnPropertyChanged();
                    CargarDatosIniciales(); // Recargar al cambiar año
                }
            }
        }

        // --- DATOS PRINCIPALES ---
        private List<ClienteResumenModel> _todosLosClientes;
        private ObservableCollection<ClienteResumenModel> _clientesResumen;
        public ObservableCollection<ClienteResumenModel> ClientesResumen { get => _clientesResumen; set { _clientesResumen = value; OnPropertyChanged(); } }

        // --- DETALLE CLIENTE SELECCIONADO ---
        private ClienteResumenModel _clienteSeleccionado;
        public ClienteResumenModel ClienteSeleccionado
        {
            get => _clienteSeleccionado;
            set
            {
                _clienteSeleccionado = value;
                OnPropertyChanged();
                if ( value != null ) CargarDetalleAdicional(value); // <--- Llenar KPIs al seleccionar
                ActualizarGrafica();
            }
        }

        // KPIs DETALLADOS (NUEVO MODELO PARA EL DETALLE)
        private KpiClienteModel _kpisDetalle;
        public KpiClienteModel KpisDetalle { get => _kpisDetalle; set { _kpisDetalle = value; OnPropertyChanged(); } }

        public ObservableCollection<ProductoAnalisisModel> ProductosEnDeclive { get; set; }
        public ObservableCollection<ProductoAnalisisModel> ProductosEnAumento { get; set; }


        // --- CONTROL DE NAVEGACIÓN ---
        private bool _enModoDetalle;
        public bool EnModoDetalle
        {
            get => _enModoDetalle;
            set { _enModoDetalle = value; OnPropertyChanged(); OnPropertyChanged(nameof(EnModoLista)); }
        }
        public bool EnModoLista => !EnModoDetalle;

        public RelayCommand VerDetalleCommand { get; set; }
        public RelayCommand VolverListaCommand { get; set; }

        // --- KPIs GLOBALES ---
        private int _totalClientesActivos;
        public int TotalClientesActivos { get => _totalClientesActivos; set { _totalClientesActivos = value; OnPropertyChanged(); } }
        private int _totalClientesInactivos;
        public int TotalClientesInactivos { get => _totalClientesInactivos; set { _totalClientesInactivos = value; OnPropertyChanged(); } }

        // --- MODOS Y GRÁFICA ---
        public ObservableCollection<string> ModosVista { get; } = new ObservableCollection<string> { "Anual", "Semestral", "Trimestral" };
        private string _modoSeleccionado = "Anual";
        public string ModoSeleccionado
        {
            get => _modoSeleccionado;
            set { _modoSeleccionado = value; OnPropertyChanged(); CalcularVisibilidadPeriodos(); ActualizarGrafica(); }
        }

        public ISeries[] SeriesGrafica { get; set; }
        public Axis[] EjeXGrafica { get; set; }
        public Axis[] EjeYGrafica { get; set; }

        // --- VISIBILIDAD DINÁMICA ---
        public Visibility VisibilityQ1 { get; set; } = Visibility.Collapsed;
        public Visibility VisibilityQ2 { get; set; } = Visibility.Collapsed;
        public Visibility VisibilityQ3 { get; set; } = Visibility.Collapsed;
        public Visibility VisibilityQ4 { get; set; } = Visibility.Collapsed;
        public Visibility VisibilityS1 { get; set; } = Visibility.Collapsed;
        public Visibility VisibilityS2 { get; set; } = Visibility.Collapsed;

        private bool _isLoading;
        public bool IsLoading { get => _isLoading; set { _isLoading = value; OnPropertyChanged(); } }
        private string _textoBusqueda;
        public string TextoBusqueda { get => _textoBusqueda; set { _textoBusqueda = value; OnPropertyChanged(); FiltrarTabla(); } }
        public RelayCommand ActualizarCommand { get; set; }

        public ClientesViewModel(ReportesService reportesService, FilterService filterService, IDialogService dialogService)
        {
            _reportesService = reportesService;
            Filters = filterService;
            _dialogService = dialogService;
            _logicService = new ClientesLogicService();

            // Inicializar años (Ej: 2026, 2025, 2024)
            int year = DateTime.Now.Year;
            AñosDisponibles = new ObservableCollection<int> { year, year - 1, year - 2, year - 3 };
            _anioSeleccionado = year; // Sin setter para no disparar recarga doble inicial

            ActualizarCommand = new RelayCommand(o => CargarDatosIniciales());
            Filters.OnFiltrosCambiados += CargarDatosIniciales;

            VerDetalleCommand = new RelayCommand(param =>
            {
                if ( param is ClienteResumenModel cliente )
                {
                    ClienteSeleccionado = cliente;
                    EnModoDetalle = true;
                }
            });

            VolverListaCommand = new RelayCommand(o =>
            {
                ClienteSeleccionado = null;
                EnModoDetalle = false;
                SeriesGrafica = null;
                KpisDetalle = null; // Limpiar detalle
            });
        }

        public async void CargarDatosIniciales()
        {
            IsLoading = true;
            try
            {
                // USAMOS EL AÑO SELECCIONADO, NO EL ACTUAL
                int anioBase = AnioSeleccionado;

                var t1 = _reportesService.ObtenerHistoricoAnualPorArticulo(anioBase.ToString(), Filters.SucursalId.ToString());
                var t2 = _reportesService.ObtenerHistoricoAnualPorArticulo(( anioBase - 1 ).ToString(), Filters.SucursalId.ToString());
                await Task.WhenAll(t1, t2);

                _todosLosClientes = await Task.Run(() => _logicService.ProcesarClientes(t1.Result, t2.Result));

                TotalClientesActivos = _todosLosClientes.Count(x => x.VentaAnualActual > 0);
                TotalClientesInactivos = _todosLosClientes.Count(x => x.VentaAnualActual == 0 && x.VentaAnualAnterior > 0);

                FiltrarTabla();
                CalcularVisibilidadPeriodos(); // <--- Recalcular visibilidad según si es año pasado o actual

                ClienteSeleccionado = null;
                SeriesGrafica = null;
            }
            catch ( Exception ex ) { _dialogService.ShowMessage("Error", ex.Message); }
            finally { IsLoading = false; }
        }

        // --- NUEVO: CARGAR DETALLES AL DAR CLIC ---
        // En ClientesViewModel.cs

        private async void CargarDetalleAdicional(ClienteResumenModel cliente)
        {
            if ( cliente == null ) return;
            IsLoading = true; // Mostramos spinner mientras carga el detalle

            try
            {
                // 1. Cargar KPIs (Ticket, Frecuencia, Ultima Compra)
                KpisDetalle = await _reportesService.ObtenerKpisCliente(
                    cliente.Nombre,
                    AnioSeleccionado,
                    Filters.SucursalId);

                // 2. Cargar Productos (Variación)
                var todosProductos = await _reportesService.ObtenerVariacionProductosCliente(
                    cliente.Nombre,
                    AnioSeleccionado,
                    Filters.SucursalId);

                // 3. Filtrar y Ordenar en Memoria para las dos tablas

                // DECLIVE: Diferencia negativa (se vendió menos que el año pasado)
                var declive = todosProductos
                    .Where(x => x.Diferencia < 0)
                    .OrderBy(x => x.Diferencia) // De mayor pérdida a menor
                    .Take(10) // Top 10
                    .ToList();

                // AUMENTO: Diferencia positiva
                var aumento = todosProductos
                    .Where(x => x.Diferencia > 0)
                    .OrderByDescending(x => x.Diferencia) // De mayor ganancia a menor
                    .Take(10) // Top 10
                    .ToList();

                ProductosEnDeclive = new ObservableCollection<ProductoAnalisisModel>(declive);
                ProductosEnAumento = new ObservableCollection<ProductoAnalisisModel>(aumento);

                // Notificar a la vista
                OnPropertyChanged(nameof(KpisDetalle));
                OnPropertyChanged(nameof(ProductosEnDeclive));
                OnPropertyChanged(nameof(ProductosEnAumento));
            }
            catch ( Exception ex )
            {
                _dialogService.ShowMessage("Error al cargar detalle", ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CalcularVisibilidadPeriodos()
        {
            // SI ES UN AÑO PASADO (Ej: 2024 cuando estamos en 2025) -> MOSTRAR TODO
            if ( AnioSeleccionado < DateTime.Now.Year )
            {
                bool esTri = ModoSeleccionado == "Trimestral";
                bool esSem = ModoSeleccionado == "Semestral";

                VisibilityQ1 = VisibilityQ2 = VisibilityQ3 = VisibilityQ4 = esTri ? Visibility.Visible : Visibility.Collapsed;
                VisibilityS1 = VisibilityS2 = esSem ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                // SI ES AÑO ACTUAL -> Lógica progresiva
                var hoy = DateTime.Now;
                bool q1Ok = hoy.Month >= 4; bool q2Ok = hoy.Month >= 7; bool q3Ok = hoy.Month >= 10;
                bool s1Ok = hoy.Month >= 7;

                bool esTri = ModoSeleccionado == "Trimestral";
                bool esSem = ModoSeleccionado == "Semestral";

                VisibilityQ1 = ( esTri && q1Ok ) ? Visibility.Visible : Visibility.Collapsed;
                VisibilityQ2 = ( esTri && q2Ok ) ? Visibility.Visible : Visibility.Collapsed;
                VisibilityQ3 = ( esTri && q3Ok ) ? Visibility.Visible : Visibility.Collapsed;
                VisibilityQ4 = Visibility.Collapsed; // Aún no acaba

                VisibilityS1 = ( esSem && s1Ok ) ? Visibility.Visible : Visibility.Collapsed;
                VisibilityS2 = Visibility.Collapsed;
            }

            OnPropertyChanged(nameof(VisibilityQ1)); OnPropertyChanged(nameof(VisibilityQ2)); OnPropertyChanged(nameof(VisibilityQ3)); OnPropertyChanged(nameof(VisibilityQ4));
            OnPropertyChanged(nameof(VisibilityS1)); OnPropertyChanged(nameof(VisibilityS2));
        }

        // ... (ActualizarGrafica y FiltrarTabla se mantienen igual) ...
        private void ActualizarGrafica() { /* Tu código existente */ }
        private void FiltrarTabla() { /* Tu código existente */ }
    }
}