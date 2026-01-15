using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;

using SkiaSharp;

using System;
using System.Collections.Generic;
using System.Linq;

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
        // ----------------------------------------------------------------
        // 1. GENERAR TENDENCIA DE LÍNEAS (Para el Desglose Mensual/Trimestral)
        // ----------------------------------------------------------------
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
                    mesFin = 12; // Muestra todo el año para ver la tendencia completa
                    break;
                case "SEMESTRAL":
                    bool esSegundoSemestre = mesActual > 6;
                    mesInicio = esSegundoSemestre ? 7 : 1;
                    mesFin = esSegundoSemestre ? 12 : 6;
                    break;
                case "TRIMESTRAL":
                    int trimestre = ( mesActual - 1 ) / 3 + 1;
                    mesInicio = ( trimestre - 1 ) * 3 + 1;
                    mesFin = mesInicio + 2;
                    break;
            }

            var nombresMeses = new string[] { "", "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" };
            var etiquetas = new List<string>();

            // Generar etiquetas solo para el rango seleccionado
            for ( int i = mesInicio; i <= mesFin; i++ )
            {
                if ( i <= 12 ) etiquetas.Add(nombresMeses[i]);
            }

            var ejes = new Axis[] { new Axis { Labels = etiquetas, LabelsPaint = new SolidColorPaint(SKColors.Gray) } };

            var seriesList = new List<ISeries>();
            var grupos = datos
                .Where(x => x.FechaEmision.Year == anioActual)
                .GroupBy(x => x.Linea ?? "Otros")
                .ToList();

            foreach ( var grupo in grupos )
            {
                var valores = new List<double>(); // Usamos double para LiveCharts
                for ( int m = mesInicio; m <= mesFin; m++ )
                {
                    // Convertimos decimal a double aquí
                    valores.Add(( double ) grupo.Where(x => x.FechaEmision.Month == m).Sum(x => x.TotalVenta));
                }

                if ( valores.Sum() > 0 )
                {
                    seriesList.Add(new LineSeries<double>
                    {
                        Name = grupo.Key,
                        Values = valores,
                        LineSmoothness = 0.5,
                        GeometrySize = 8,
                        Stroke = new SolidColorPaint { StrokeThickness = 3 },
                        GeometryStroke = new SolidColorPaint { StrokeThickness = 3 },
                        DataLabelsFormatter = p => p.Model >= 1000 ? $"{p.Model / 1000:N0}K" : $"{p.Model:N0}"
                    });
                }
            }

            return new ResultadoGrafico
            {
                Series = seriesList.ToArray(),
                EjesX = ejes
            };
        }

        // ----------------------------------------------------------------
        // 2. GENERAR TOP PRODUCTOS (Barras Horizontales)
        // ----------------------------------------------------------------
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
                    Venta = ( double ) g.Sum(v => v.TotalVenta), // Convertir a double
                    Litros = ( double ) g.Sum(v => v.LitrosTotales) // Convertir a double
                })
                .OrderByDescending(x => verPorLitros ? x.Litros : x.Venta)
                .Take(5)
                .Reverse() // Invertir para que el Top 1 quede arriba en la gráfica de barras
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
                        // CORRECCIÓN: Usamos p.Model directamente (porque es double)
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
                    new RowSeries<double>
                    {
                        Values = topProductos.Select(x => x.Venta).ToArray(),
                        Name = "Venta",
                        Fill = new SolidColorPaint(SKColors.DodgerBlue),
                        DataLabelsPaint = new SolidColorPaint(SKColors.White), // Blanco sobre azul se ve mejor
                        DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.End, // End lo pone fuera o al borde
                        // CORRECCIÓN: Formato moneda
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
                    Labels = topProductos.Select(x => NormalizarNombreProducto(x.NombreVisual)).ToArray(), // Normalizamos nombres aquí también
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

        // ----------------------------------------------------------------
        // 3. GENERAR PIE CHART (Pastel)
        // ----------------------------------------------------------------
        public ISeries[] GenerarPieChart(List<LineaResumenModel> datos)
        {
            if ( datos == null || !datos.Any() ) return Array.Empty<ISeries>();

            // LiveCharts maneja mejor los doubles en PieCharts para cálculos de porcentajes internos
            return datos.Select(x => new PieSeries<double>
            {
                Values = new double[] { ( double ) x.VentaTotal },
                Name = x.NombreLinea,
                DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                DataLabelsSize = 10,
                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Outer,

                // CORRECCIÓN: Para PieSeries<double>, usamos p.Model (valor) y p.StackedValue.Share (porcentaje)
                DataLabelsFormatter = p => $"{p.Model:C0}",
                ToolTipLabelFormatter = point => $"{point.Context.Series.Name}: {point.Model:C0} ({point.StackedValue.Share:P1})"
            }).ToArray();
        }

        // Helper para nombres bonitos
        private string NormalizarNombreProducto(string nombreOriginal)
        {
            if ( string.IsNullOrEmpty(nombreOriginal) ) return "";
            string limpio = nombreOriginal.Trim();
            if ( limpio.Contains("-") )
            {
                var partes = limpio.Split('-');
                if ( partes.Length > 1 ) limpio = partes[1];
            }
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(limpio.ToLower());
        }
    }
}