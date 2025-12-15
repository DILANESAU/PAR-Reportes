using System;
using System.Collections.Generic;
using System.Text;

namespace WPF_PAR.MVVM.Models
{
    public class ClienteRankingModel
    {
        public string ClaveCliente { get; set; }
        public string Nombre { get; set; }

        // Propiedades por mes
        public decimal Enero { get; set; }
        public decimal Febrero { get; set; }
        public decimal Marzo { get; set; }
        public decimal Abril { get; set; }
        public decimal Mayo { get; set; }
        public decimal Junio { get; set; }
        public decimal Julio { get; set; }
        public decimal Agosto { get; set; }
        public decimal Septiembre { get; set; }
        public decimal Octubre { get; set; }
        public decimal Noviembre { get; set; }
        public decimal Diciembre { get; set; }

        // Total Anual
        public decimal TotalAnual =>
            Enero + Febrero + Marzo + Abril + Mayo + Junio +
            Julio + Agosto + Septiembre + Octubre + Noviembre + Diciembre;

        // LÓGICA DE TENDENCIA (Colores)
        // Retorna: "UP" (Verde), "DOWN" (Rojo), "FLAT" (Sin color)
        public string Tendencia
        {
            get
            {
                // 1. Ponemos los meses en una lista ordenada
                var ventas = new List<decimal> { Enero, Febrero, Marzo, Abril, Mayo, Junio, Julio, Agosto, Septiembre, Octubre, Noviembre, Diciembre };

                // 2. Filtramos solo los meses que ya pasaron o tienen venta (para no contar ceros futuros)
                // Asumimos que si hay venta, cuenta. Si es 0 al inicio y luego vende, es subida.
                // Una lógica simple: Comparamos el promedio del primer trimestre con ventas vs el último.

                var mesesConVenta = ventas.Select((v, i) => new { Mes = i, Venta = v }).Where(x => x.Venta > 0).ToList();

                if ( mesesConVenta.Count < 2 ) return "FLAT"; // No hay suficientes datos

                // Tomamos la primera venta registrada y la última
                decimal primeraVenta = mesesConVenta.First().Venta;
                decimal ultimaVenta = mesesConVenta.Last().Venta;

                // Umbral de sensibilidad (ej. 5% de diferencia) para considerar cambio
                decimal diferencia = ultimaVenta - primeraVenta;

                if ( diferencia > ( primeraVenta * 0.05m ) ) return "UP";   // Creció > 5%
                if ( diferencia < -( primeraVenta * 0.05m ) ) return "DOWN"; // Cayó > 5%

                return "FLAT"; // Se mantuvo estable
            }
        }
    }
}
