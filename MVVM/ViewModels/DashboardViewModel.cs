using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

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
    public class DashboardViewModel : ObservableObject
    {
        private readonly VentasServices _ventasService;
        private readonly IDialogService _dialogService;
        private readonly FilterService _filters;

        // --- PROPIEDADES DE DATOS ---
        private decimal _totalVentas;
        public decimal TotalVentas
        {
            get => _totalVentas;
            set { _totalVentas = value; OnPropertyChanged(); }
        }

        private int _cantidadTransacciones;
        public int CantidadTransacciones
        {
            get => _cantidadTransacciones;
            set { _cantidadTransacciones = value; OnPropertyChanged(); }
        }

        private string _topCliente;
        public string TopCliente
        {
            get => _topCliente;
            set { _topCliente = value; OnPropertyChanged(); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        // --- COLECCIONES Y GRÁFICOS ---
        public ObservableCollection<VentasModel> ListaVentas { get; set; }

        public ISeries[] SeriesGrafico { get; set; }
        public Axis[] EjeX { get; set; }
        public Axis[] EjeY { get; set; }

        public ISeries[] SeriesHistorico { get; set; }
        public Axis[] EjeXHistorico { get; set; }

        public RelayCommand ActualizarCommand { get; set; }

        // --- CONSTRUCTOR ---
        public DashboardViewModel(VentasServices ventasService, IDialogService dialogService, FilterService filterService)
        {
            _dialogService = dialogService;
            _filters = filterService;
            _ventasService = ventasService;
            ListaVentas = new ObservableCollection<VentasModel>();

            // Suscribirse al filtro global
            _filters.OnFiltrosCambiados += CargarDatos;

            ActualizarCommand = new RelayCommand(o => CargarDatos());

            // Carga inicial
            CargarDatos();
        }

        public async void CargarDatos()
        {
            IsLoading = true;
            try
            {
                // 1. Obtener Ventas del Rango (Diario/Tabla)
                var datosRango = await _ventasService.ObtenerVentasRangoAsync(
                    _filters.SucursalId,
                    _filters.FechaInicio,
                    _filters.FechaFin
                );

                ListaVentas = new ObservableCollection<VentasModel>(datosRango);

                CalcularResumen(datosRango);
                ConfigurarGraficoDiario(datosRango);

                // 2. Obtener Histórico Anual (Usando el año de la fecha fin)
                var datosAnuales = await _ventasService.ObtenerVentaAnualAsync(
                    _filters.SucursalId,
                    _filters.FechaFin.Year
                );

                ConfigurarGraficoHistorico(datosAnuales);
            }
            catch ( Exception ex )
            {
                _dialogService.ShowError($"Error al cargar dashboard: {ex.Message}", "Error de Conexión");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CalcularResumen(List<VentasModel> datos)
        {
            if ( datos == null || !datos.Any() )
            {
                TotalVentas = 0;
                CantidadTransacciones = 0;
                TopCliente = "Sin datos";
                return;
            }

            TotalVentas = datos.Sum(x => x.PrecioTotal);
            CantidadTransacciones = datos.Count;

            var mejorCliente = datos
                                .GroupBy(x => x.Cliente)
                                .Select(g => new { Cliente = g.Key, Total = g.Sum(x => x.PrecioTotal) })
                                .OrderByDescending(g => g.Total)
                                .FirstOrDefault();

            TopCliente = mejorCliente != null ? mejorCliente.Cliente : "N/A";
        }

        private void ConfigurarGraficoDiario(List<VentasModel> datos)
        {
            var ventasPorDia = datos
                .GroupBy(x => x.Fecha.Day)
                .OrderBy(g => g.Key)
                .Select(g => new { Dia = g.Key, Monto = g.Sum(x => x.PrecioTotal) })
                .ToList();

            if ( ventasPorDia.Count == 0 )
            {
                SeriesGrafico = Array.Empty<ISeries>();
            }
            else
            {
                SeriesGrafico = new ISeries[]
                {
                    new ColumnSeries<decimal>
                    {
                        Name = "Ventas",
                        Values = ventasPorDia.Select(x => x.Monto).ToArray(),
                        Fill = new SolidColorPaint(SKColors.CornflowerBlue),
                        Rx = 6, Ry = 6,
                        DataLabelsSize = 12,
                        DataLabelsPaint = new SolidColorPaint(SKColors.Gray),
                        DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top,
                        DataLabelsFormatter = (point) => point.Model.ToString("C0"),
                        XToolTipLabelFormatter = (point) => $"{point.Model:C2}"
                    }
                };
            }

            EjeX = new Axis[]
            {
                new Axis { Labels = ventasPorDia.Select(x => x.Dia.ToString()).ToArray(), Name = "Día" }
            };

            EjeY = new Axis[]
            {
                new Axis { Labeler = value => value.ToString("C0"), ShowSeparatorLines = true }
            };

            OnPropertyChanged(nameof(SeriesGrafico));
            OnPropertyChanged(nameof(EjeX));
            OnPropertyChanged(nameof(EjeY));
        }

        private void ConfigurarGraficoHistorico(List<VentasModel> datosAnuales)
        {
            var valores = new decimal[12];
            var meses = new string[] { "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" };

            // Mapeamos los datos (Asumiendo que 'Mov' trae el número de mes 1-12)
            foreach ( var item in datosAnuales )
            {
                if ( int.TryParse(item.Mov, out int mes) && mes >= 1 && mes <= 12 )
                {
                    valores[mes - 1] = item.PrecioTotal;
                }
            }

            SeriesHistorico = new ISeries[]
            {
                new LineSeries<decimal>
                {
                    Name = "Venta Mensual",
                    Values = valores,
                    Fill = new SolidColorPaint(SKColors.CornflowerBlue.WithAlpha(50)),
                    Stroke = new SolidColorPaint(SKColors.CornflowerBlue) { StrokeThickness = 4 },
                    GeometrySize = 10,
                    GeometryStroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 3 },
                    XToolTipLabelFormatter = (p) => $"{p.Model:C0}"
                }
            };

            EjeXHistorico = new Axis[]
            {
                new Axis { Labels = meses, LabelsPaint = new SolidColorPaint(SKColors.Gray) }
            };

            OnPropertyChanged(nameof(SeriesHistorico));
            OnPropertyChanged(nameof(EjeXHistorico));
        }
    }
}