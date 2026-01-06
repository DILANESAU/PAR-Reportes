using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using WPF_PAR.MVVM.Models;

namespace WPF_PAR.Services
{
    public class ResultadoGrafico
    {
        public ISeries[] Series { get; set; }
        public Axis[] EjesX { get; set; }
    }
    public class ResultadoTopProductos
    {
        public ISeries[] Series { get; set; }
        public Axis[] EjesX { get; set; }
        public Axis[] EjesY { get; set; }
    }

    public class ChartService
    {
        public ResultadoGrafico GenerarTendenciaLineas(List<VentaReporteModel> datos, string periodo)
        {
            if ( datos == null || !datos.Any() )
                return new ResultadoGrafico { Series = Array.Empty<ISeries>(), EjesX = new Axis[0] };

            DateTime fechaReferencia = DateTime.Now;
            int anioActual = fechaReferencia.Year;
            int mesActual = fechaReferencia.Month;

            int mesInicio = 1;
            int mesFin = mesActual;

            switch ( periodo )
            {
                case "ANUAL":
                    mesInicio = 1;
                    break;
                case "SEMESTRAL":
                    bool esSegundoSemestre = mesActual > 6;
                    mesInicio = esSegundoSemestre ? 7 : 1;
                    break;
                case "TRIMESTRAL":
                    int trimestre = ( mesActual - 1 ) / 3 + 1;
                    mesInicio = ( trimestre - 1 ) * 3 + 1;
                    break;
            }

            var nombresMeses = new string[] { "", "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" };
            var etiquetas = new List<string>();
            for ( int i = mesInicio; i <= mesFin; i++ )
            {
                etiquetas.Add(nombresMeses[i]);
            }
            var ejes = new Axis[] { new Axis { Labels = etiquetas, LabelsPaint = new SolidColorPaint(SKColors.Gray) } };

            var seriesList = new List<ISeries>();
            var grupos = datos
                .Where(x => x.FechaEmision.Year == anioActual)
                .GroupBy(x => x.Linea ?? "Otros")
                .ToList();

            foreach ( var grupo in grupos )
            {
                var valores = new List<decimal>();
                for ( int m = mesInicio; m <= mesFin; m++ )
                {
                    valores.Add(grupo.Where(x => x.FechaEmision.Month == m).Sum(x => x.TotalVenta));
                }

                if ( valores.Sum() > 0 )
                {
                    seriesList.Add(new LineSeries<decimal>
                    {
                        Name = grupo.Key,
                        Values = valores,
                        LineSmoothness = 0.5,
                        GeometrySize = 8,
                        Stroke = new SolidColorPaint { StrokeThickness = 3 },
                        GeometryStroke = new SolidColorPaint { StrokeThickness = 3 }
                    });
                }
            }

            return new ResultadoGrafico
            {
                Series = seriesList.ToArray(),
                EjesX = ejes
            };
        }
        public ResultadoTopProductos GenerarTopProductos(List<VentaReporteModel> datos, bool verPorLitros)
        {
            if ( datos == null || !datos.Any() )
            {
                return new ResultadoTopProductos
                {
                    Series = Array.Empty<ISeries>(),
                    EjesX = Array.Empty<Axis>(),
                    EjesY = Array.Empty<Axis>()
                };
            }

            var topProductos = datos
                .GroupBy(x => x.Descripcion)
                .Select(g => new
                {
                    NombreVisual = g.Key,
                    Venta = g.Sum(v => v.TotalVenta),
                    Litros = g.Sum(v => v.LitrosTotales)
                })
                .OrderByDescending(x => verPorLitros ? x.Litros : ( double ) x.Venta)
                .Take(5)
                .Reverse() 
                .ToList();

            ISeries[] series;
            Axis[] ejeX;

            if ( verPorLitros )
            {
                series = new ISeries[]
                {
                    new RowSeries<double>
                    {
                        Values = topProductos.Select(x => x.Litros).ToArray(),
                        Name = "Volumen",
                        Fill = new SolidColorPaint(SKColors.Orange),
                        DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                        DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.End,
                        DataLabelsFormatter = p => $"{p.Model:N0} L",
                        XToolTipLabelFormatter = p => $"{p.Model:N0} L"
                    }
                };
                ejeX = new Axis[] { new Axis { IsVisible = false, Labeler = v => $"{v:N0}" } };
            }
            else
            {
                series = new ISeries[]
                {
                    new RowSeries<decimal>
                    {
                        Values = topProductos.Select(x => x.Venta).ToArray(),
                        Name = "Venta",
                        Fill = new SolidColorPaint(SKColors.DodgerBlue),
                        DataLabelsPaint = new SolidColorPaint(SKColors.White),
                        DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.End,
                        DataLabelsFormatter = p => $"{p.Model:C0}",
                        XToolTipLabelFormatter = p => $"{p.Model:C0}"
                    }
                };
                ejeX = new Axis[] { new Axis { IsVisible = false, Labeler = v => $"{v:C0}" } };
            }

            var ejeY = new Axis[]
            {
                new Axis
                {
                    Labels = topProductos.Select(x => x.NombreVisual).ToArray(),
                    LabelsPaint = new SolidColorPaint(SKColors.Black),
                    TextSize = 11
                }
            };

            return new ResultadoTopProductos
            {
                Series = series,
                EjesX = ejeX,
                EjesY = ejeY
            };
        }
        public ISeries[] GenerarPieChart(List<LineaResumenModel> datos)
        {
            if ( datos == null || !datos.Any() ) return Array.Empty<ISeries>();

            return datos.Select(x => new PieSeries<decimal>
            {
                Values = new decimal[] { x.VentaTotal },
                Name = x.NombreLinea,
                DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                DataLabelsSize = 10,
                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Outer,
                DataLabelsFormatter = p => $"{p.Model:C0}",
                ToolTipLabelFormatter = (point) => $"{point.Context.Series.Name}: {point.Model:C2}"
            }).ToArray();
        }
    }
}