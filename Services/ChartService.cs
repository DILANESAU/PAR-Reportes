using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;

using SkiaSharp;

using System.Windows.Media;

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
                return new ResultadoGrafico { Series = new ISeries[0], EjesX = new Axis[0] };

            // ... (El código de fechas y cálculo de meses se queda IGUAL) ...
            // COPIA AQUÍ LA LÓGICA DE FECHAS/PERIODOS QUE YA TENÍAS
            DateTime fechaReferencia = DateTime.Now;
            int anioActual = fechaReferencia.Year;
            int mesActual = fechaReferencia.Month;
            int mesInicio = 1;
            int mesFin = mesActual;

            switch ( periodo )
            {
                case "ANUAL": mesInicio = 1; break;
                case "SEMESTRAL": mesInicio = ( mesActual > 6 ) ? 7 : 1; break;
                case "TRIMESTRAL": int trim = ( mesActual - 1 ) / 3 + 1; mesInicio = ( trim - 1 ) * 3 + 1; break;
            }

            var nombresMeses = new string[] { "", "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" };
            var etiquetas = new List<string>();
            for ( int i = mesInicio; i <= mesFin; i++ ) etiquetas.Add(nombresMeses[i]);

            var ejes = new Axis[] { new Axis { Labels = etiquetas, LabelsPaint = new SolidColorPaint(SKColors.Gray) } };

            // --- AQUÍ ESTÁ EL CAMBIO PARA QUE NO SE SATURE ---

            // 1. Agrupar
            var gruposRaw = datos
                .Where(x => x.FechaEmision.Year == anioActual)
                .GroupBy(x => x.Linea ?? "Otros")
                .Select(g => new
                {
                    Nombre = g.Key,
                    VentaTotal = g.Sum(x => x.TotalVenta), // Calculamos venta total anual para rankear
                    Datos = g
                })
                .OrderByDescending(x => x.VentaTotal) // Ordenamos de mayor a menor
                .Take(5) // <--- ¡SOLO TOMAMOS LAS 5 MEJORES!
                .ToList();

            var seriesList = new List<ISeries>();

            foreach ( var grupo in gruposRaw )
            {
                var valores = new List<decimal>();
                for ( int m = mesInicio; m <= mesFin; m++ )
                {
                    valores.Add(grupo.Datos.Where(x => x.FechaEmision.Month == m).Sum(x => x.TotalVenta));
                }

                if ( valores.Sum() > 0 )
                {
                    seriesList.Add(new LineSeries<decimal>
                    {
                        Name = grupo.Nombre,
                        Values = valores,
                        LineSmoothness = 1, // Curvas suaves (más moderno)
                        GeometrySize = 8,   // Puntos visibles pero no gigantes
                        Stroke = new SolidColorPaint { StrokeThickness = 3 },
                        GeometryStroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 3 }
                    });
                }
            }

            return new ResultadoGrafico { Series = seriesList.ToArray(), EjesX = ejes };
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

            // 1. Preparamos los datos SIN TRUNCAR el nombre
            var topProductos = datos
                .GroupBy(x => x.Descripcion)
                .Select(g => new
                {
                    // CAMBIO: Usamos el nombre completo. Si es EXCESIVAMENTE largo, LiveCharts lo manejará o se verá en el Tooltip.
                    NombreCompleto = g.Key,
                    Venta = g.Sum(v => v.TotalVenta),
                    Litros = g.Sum(v => v.LitrosTotales)
                })
                .OrderByDescending(x => verPorLitros ? x.Litros : ( double ) x.Venta)
                .Take(5)
                .Reverse()
                .ToList();

            // Guardamos los nombres en una lista para usarla en el Tooltip
            var nombresEje = topProductos.Select(x => x.NombreCompleto).ToArray();

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
                        
                        // CAMBIO: El tooltip ahora busca el nombre en nuestra lista usando el índice de la barra
                        XToolTipLabelFormatter = p => $"{nombresEje[p.Index]}: {p.Model:N0} L"
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
                        
                        // CAMBIO: Tooltip completo con Nombre + Dinero
                        XToolTipLabelFormatter = p => $"{nombresEje[p.Index]}: {p.Model:C0}"
                    }
                };
                ejeX = new Axis[] { new Axis { IsVisible = false, Labeler = v => $"{v:C0}" } };
            }

            var ejeY = new Axis[]
            {
                new Axis
                {
                    Labels = nombresEje, // Usamos los nombres completos en el eje
                    LabelsPaint = new SolidColorPaint(SKColors.Black),
                    TextSize = 12
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
                DataLabelsPaint = new SolidColorPaint(SKColors.White),
                DataLabelsSize = 10,
                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                DataLabelsFormatter = p => $"{p.Model:C0}",
                ToolTipLabelFormatter = (point) => $"{point.Context.Series.Name}: {point.Model:C2}"
            }).ToArray();
        }
    }
}