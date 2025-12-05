using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;

using SkiaSharp;

using System.Collections.ObjectModel;
using System.Windows;

using WPF_PAR.Core;
using WPF_PAR.MVVM.Models;
using WPF_PAR.Services;
using WPF_PAR.Services.Interfaces;

namespace WPF_PAR.MVVM.ViewModels
{
    public class DashboardViewModel : ObservableObject
    {
        private readonly VentasServices _ventasService;
        private readonly SucursalesService _sucursalesService;
        private List<VentasModel> _datosMemoria;
        private readonly IDialogService _dialogService;
        private decimal _totalVentas;
        public decimal TotalVentas
        {
            get { return _totalVentas; }
            set { _totalVentas = value; OnPropertyChanged(); }
        }
        private int _cantidadTransacciones;
        public int CantidadTransacciones
        {
            get { return _cantidadTransacciones; }
            set { _cantidadTransacciones = value; OnPropertyChanged(); }
        }
        private string _topCliente;
        public string TopCliente
        {
            get { return _topCliente; }
            set { _topCliente = value; OnPropertyChanged(); }
        }
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<VentasModel> _listaVentas;
        public ObservableCollection<VentasModel> ListaVentas
        {
            get { return _listaVentas; }
            set { _listaVentas = value; OnPropertyChanged(); }
        }
        public ISeries[] SeriesGrafico { get; set; }
        public Axis[] EjeX { get; set; }
        public Axis[] EjeY { get; set; }

        public Dictionary<int, string> ListaSucursales { get; set; }
        private int _sucursalSeleccionadaId;
        public int SucursalSeleccionadaId
        {
            get => _sucursalSeleccionadaId;
            set { _sucursalSeleccionadaId = value; OnPropertyChanged(); }
        }
        public ObservableCollection<int> ListaAnios { get; set; }
        private int _anioSeleccionado;
        public int AnioSeleccionado
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
        public RelayCommand ActualizarCommand { get; set; }
        public DashboardViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;

            _ventasService = new VentasServices();
            _sucursalesService = new SucursalesService();
            ListaVentas = new ObservableCollection<VentasModel>();

            ActualizarCommand = new RelayCommand(o => CargarDatos());

            CargarFiltrosIniciales();
            CargarDatos();
        }
        private void CargarFiltrosIniciales()
        {
            ListaAnios = new ObservableCollection<int> { 2023, 2024, 2025 };
            AnioSeleccionado = DateTime.Now.Year;
            ListaMeses = new Dictionary<int, string>
            {
                {1, "Enero"}, {2, "Febrero"}, {3, "Marzo"}, {4, "Abril"},
                {5, "Mayo"}, {6, "Junio"}, {7, "Julio"}, {8, "Agosto"},
                {9, "Septiembre"}, {10, "Octubre"}, {11, "Noviembre"}, {12, "Diciembre"}
            };
            MesSeleccionado = DateTime.Now.Month;

            var todasLasSucursales = _sucursalesService.CargarSucursales();

            if ( Session.UsuarioActual.SucursalesPermitidas == null || Session.UsuarioActual.SucursalesPermitidas.Count == 0 )
            {
                ListaSucursales = todasLasSucursales;
            }
            else
            {
                ListaSucursales = todasLasSucursales
                    .Where(x => Session.UsuarioActual.SucursalesPermitidas.Contains(x.Key))
                    .ToDictionary(x => x.Key, x => x.Value);
            }

            int sucursalGuardada = Properties.Settings.Default.SucursalDefaultId;

            if ( ListaSucursales.ContainsKey(sucursalGuardada) )
            {
                SucursalSeleccionadaId = sucursalGuardada;
            }
            else if ( ListaSucursales.Count > 0 )
            {
                SucursalSeleccionadaId = ListaSucursales.Keys.First();
            }
        }
        public async void CargarDatos()
        {
            IsLoading = true;
            try
            {
                _datosMemoria = await _ventasService.ObtenerVentasAsync(SucursalSeleccionadaId, AnioSeleccionado, MesSeleccionado);

                ListaVentas = new ObservableCollection<VentasModel>(_datosMemoria);

                CalcularResumen();

                ConfigurarGrafico();
            }
            catch ( Exception ex )
            {
                _dialogService.ShowError($"Error al cargar datos: {ex.Message}", "Error de Conexión");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CalcularResumen()
        {
            if ( _datosMemoria == null || !_datosMemoria.Any() )
            {
                TotalVentas = 0;
                CantidadTransacciones = 0;
                TopCliente = "Sin datos";
                return;
            }

            TotalVentas = _datosMemoria.Sum(x => x.PrecioTotal);
            CantidadTransacciones = _datosMemoria.Count;

            var mejorCliente = _datosMemoria
                                .GroupBy(x => x.Cliente)
                                .Select(g => new { Cliente = g.Key, Total = g.Sum(x => x.PrecioTotal) })
                                .OrderByDescending(g => g.Total)
                                .FirstOrDefault();

            TopCliente = mejorCliente != null ? mejorCliente.Cliente : "N/A";
        }

        private void ConfigurarGrafico()
        {
            var ventasPorDia = _datosMemoria
                .GroupBy(x => x.Fecha.Day)
                .OrderBy(g => g.Key)
                .Select(g => new { Dia = g.Key, Monto = g.Sum(x => x.PrecioTotal) })
                .ToList();

            if ( ventasPorDia.Count == 0 )
            {
                SeriesGrafico = Array.Empty<ISeries>();
                OnPropertyChanged(nameof(SeriesGrafico));
                return;
            }

            SeriesGrafico = new ISeries[]
            {
                new ColumnSeries<decimal>
                {
                    Name = "Ventas",
                    Values = ventasPorDia.Select(x => x.Monto).ToArray(),
                    Fill = new SolidColorPaint(SKColors.CornflowerBlue),
                }
            };

            EjeX = new Axis[]
            {
                new Axis
                {
                    Labels = ventasPorDia.Select(x => x.Dia.ToString()).ToArray(),
                    Name = "Dia"
                }
            };

            EjeY = new Axis[]
            {
                new Axis
                {
                    Labeler = value => value.ToString("C2")
                }
            };

            OnPropertyChanged(nameof(SeriesGrafico));
            OnPropertyChanged(nameof(EjeX));
            OnPropertyChanged(nameof(EjeY));
        }
    }
}

