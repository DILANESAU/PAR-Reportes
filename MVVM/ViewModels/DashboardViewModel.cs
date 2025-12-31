using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;

using SkiaSharp;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

using WPF_PAR.Core;
using WPF_PAR.MVVM.Models;
using WPF_PAR.Services;
using WPF_PAR.Services.Interfaces;

namespace WPF_PAR.MVVM.ViewModels
{
    public class DashboardViewModel : ObservableObject
    {
        private readonly VentasServices _ventasService;
        private readonly IDialogService _dialogService;
        public FilterService Filters { get; }

        // ---------------------------------------------------------
        // 1. FILTROS Y COMANDOS
        // ---------------------------------------------------------
        public ObservableCollection<string> FiltrosFecha { get; set; }

        private string _periodoSeleccionado;
        public string PeriodoSeleccionado
        {
            get => _periodoSeleccionado;
            set
            {
                _periodoSeleccionado = value;
                OnPropertyChanged();
                // Aquí podrías lógica para cambiar Filters.FechaInicio/Fin según la selección
                ActualizarCommand.Execute(null);
            }
        }

        public RelayCommand ActualizarCommand { get; set; }
        public RelayCommand IrAVentasCommand { get; set; }
        public RelayCommand IrAInventarioCommand { get; set; }

        // ---------------------------------------------------------
        // 2. KPIs (Adaptados a tu lógica real)
        // ---------------------------------------------------------
        private decimal _kpiVentas;
        public decimal KpiVentas { get => _kpiVentas; set { _kpiVentas = value; OnPropertyChanged(); } }

        private int _kpiTransacciones;
        public int KpiTransacciones { get => _kpiTransacciones; set { _kpiTransacciones = value; OnPropertyChanged(); } }

        private int _kpiClientes;
        public int KpiClientes { get => _kpiClientes; set { _kpiClientes = value; OnPropertyChanged(); } }

        private int _kpiClientesNuevos;
        public int KpiClientesNuevos { get => _kpiClientesNuevos; set { _kpiClientesNuevos = value; OnPropertyChanged(); } }

        // ---------------------------------------------------------
        // 3. GRÁFICOS
        // ---------------------------------------------------------
        // Usaremos tu lógica de 'Historico' para el gráfico principal
        private ISeries[] _seriesVentas;
        public ISeries[] SeriesVentas { get => _seriesVentas; set { _seriesVentas = value; OnPropertyChanged(); } } // Enlazado al XAML

        public Axis[] EjeX { get; set; }
        public Axis[] EjeY { get; set; }

        // ---------------------------------------------------------
        // 4. LISTAS (Calculadas desde tus datos)
        // ---------------------------------------------------------
        public ObservableCollection<TopProductoItem> TopProductosList { get; set; }
        public ObservableCollection<ClienteRecienteItem> UltimosClientesList { get; set; }

        // Mantenemos tu lista original por si la usas en otra parte
        public ObservableCollection<VentasModel> ListaVentas { get; set; }

        // ---------------------------------------------------------
        // 5. ESTADO
        // ---------------------------------------------------------
        private bool _isLoading;
        public bool IsLoading { get => _isLoading; set { _isLoading = value; OnPropertyChanged(); } }

        private bool _hayAlertas;
        public bool HayAlertas { get => _hayAlertas; set { _hayAlertas = value; OnPropertyChanged(); } }

        // ---------------------------------------------------------
        // CONSTRUCTOR
        // ---------------------------------------------------------
        public DashboardViewModel(VentasServices ventasService, IDialogService dialogService, FilterService filterService)
        {
            _dialogService = dialogService;
            Filters = filterService;
            _ventasService = ventasService;

            // Inicialización
            FiltrosFecha = new ObservableCollection<string> { "Hoy", "Esta Semana", "Este Mes", "Anual" };
            TopProductosList = new ObservableCollection<TopProductoItem>();
            UltimosClientesList = new ObservableCollection<ClienteRecienteItem>();
            ListaVentas = new ObservableCollection<VentasModel>();

            // Comandos
            ActualizarCommand = new RelayCommand(o => CargarDatos());
            IrAVentasCommand = new RelayCommand(o => System.Diagnostics.Debug.WriteLine("Navegar a Ventas")); // Aquí conectarías tu navegación
            IrAInventarioCommand = new RelayCommand(o => System.Diagnostics.Debug.WriteLine("Navegar a Inventario"));

            // Estilos base de Ejes
            EjeX = new Axis[] { new Axis { LabelsPaint = new SolidColorPaint(SKColors.Gray) } };
            EjeY = new Axis[] { new Axis { Labeler = v => $"{v:C0}", LabelsPaint = new SolidColorPaint(SKColors.Gray) } };

            // Carga inicial
            PeriodoSeleccionado = "Este Mes"; // Esto lanzará ActualizarCommand
        }

        public async void CargarDatos()
        {
            IsLoading = true;
            try
            {
                // 1. OBTENER DATOS REALES (Tu servicio)
                var datosRango = await _ventasService.ObtenerVentasRangoAsync(
                    Filters.SucursalId,
                    Filters.FechaInicio,
                    Filters.FechaFin
                );

                ListaVentas = new ObservableCollection<VentasModel>(datosRango);

                // 2. CALCULAR KPIs Y LISTAS (Lógica nueva con tus datos)
                ProcesarDatosResumen(datosRango);

                // 3. OBTENER HISTÓRICO PARA EL GRÁFICO (Tu servicio)
                var datosAnuales = await _ventasService.ObtenerVentaAnualAsync(
                    Filters.SucursalId,
                    Filters.FechaFin.Year
                );

                ConfigurarGraficoPrincipal(datosAnuales);
            }
            catch ( Exception ex )
            {
                _dialogService.ShowError($"Error al cargar dashboard: {ex.Message}", "Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ProcesarDatosResumen(List<VentasModel> datos)
        {
            if ( datos == null || !datos.Any() )
            {
                KpiVentas = 0;
                KpiTransacciones = 0;
                KpiClientes = 0;
                TopProductosList.Clear(); // Nota: Aunque se llame TopProductosList, guardaremos Clientes aquí
                UltimosClientesList.Clear();
                return;
            }

            // A. KPIs Básicos
            KpiVentas = datos.Sum(x => x.PrecioTotal);
            KpiTransacciones = datos.Count;
            KpiClientes = datos.Select(x => x.Cliente).Distinct().Count();

            // B. CORRECCIÓN: CAMBIAR TOP PRODUCTOS POR TOP CLIENTES (VIP)
            // Como VentasModel no tiene 'Descripcion', usamos 'Cliente'.
            var topClientes = datos
                .GroupBy(x => x.Cliente) // <--- Agrupamos por Cliente
                .Select(g => new TopProductoItem
                {
                    // Usamos el nombre del cliente en lugar del producto
                    Nombre = string.IsNullOrEmpty(g.Key) ? "Público General" : g.Key,
                    Monto = g.Sum(x => x.PrecioTotal)
                })
                .OrderByDescending(x => x.Monto)
                .Take(5)
                .ToList();

            // Asignar Ranking
            for ( int i = 0; i < topClientes.Count; i++ ) topClientes[i].Ranking = i + 1;

            // Guardamos en la lista (visualizará Clientes en vez de Productos)
            TopProductosList = new ObservableCollection<TopProductoItem>(topClientes);

            // C. Generar Lista ÚLTIMOS CLIENTES (Clientes Recientes)
            // Tomamos las últimas 5 ventas ordenadas por fecha
            var ultimos = datos
                .OrderByDescending(x => x.Fecha)
                .Take(5) // Tomamos las últimas 5 transacciones
                .Select(x => new ClienteRecienteItem
                {
                    Nombre = string.IsNullOrEmpty(x.Cliente) ? "Público General" : x.Cliente,
                    Fecha = x.Fecha,
                    Iniciales = ObtenerIniciales(x.Cliente)
                })
                .ToList();

            UltimosClientesList = new ObservableCollection<ClienteRecienteItem>(ultimos);
        }

        private void ConfigurarGraficoPrincipal(List<VentasModel> datosAnuales)
        {
            // Reusamos tu lógica de mapeo mensual
            var valores = new decimal[12];
            var meses = new string[] { "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" };

            foreach ( var item in datosAnuales )
            {
                if ( int.TryParse(item.Mov, out int mes) && mes >= 1 && mes <= 12 )
                {
                    valores[mes - 1] = item.PrecioTotal;
                }
            }

            SeriesVentas = new ISeries[]
            {
                new LineSeries<decimal>
                {
                    Name = "Venta Mensual",
                    Values = valores,
                    Fill = new SolidColorPaint(SKColors.CornflowerBlue.WithAlpha(30)), // Relleno moderno
                    Stroke = new SolidColorPaint(SKColors.CornflowerBlue) { StrokeThickness = 3 },
                    GeometrySize = 8,
                    GeometryStroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 2 }
                }
            };

            EjeX = new Axis[]
            {
                new Axis { Labels = meses, LabelsPaint = new SolidColorPaint(SKColors.Gray) }
            };
        }

        // Helper para las bolitas de colores
        private string ObtenerIniciales(string nombre)
        {
            if ( string.IsNullOrWhiteSpace(nombre) ) return "?";
            var partes = nombre.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if ( partes.Length == 0 ) return "?";
            if ( partes.Length == 1 ) return partes[0].Substring(0, Math.Min(2, partes[0].Length)).ToUpper();
            return ( partes[0][0].ToString() + partes[1][0].ToString() ).ToUpper();
        }
    }

    // --- MODELOS AUXILIARES PARA EL DASHBOARD ---
    public class TopProductoItem
    {
        public int Ranking { get; set; }
        public string Nombre { get; set; }
        public decimal Monto { get; set; }
    }

    public class ClienteRecienteItem
    {
        public string Iniciales { get; set; }
        public string Nombre { get; set; }
        public DateTime Fecha { get; set; }
    }
}