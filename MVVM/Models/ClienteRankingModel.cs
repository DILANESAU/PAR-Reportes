using System;
using System.Collections.Generic;
using System.Text;

namespace WPF_PAR.MVVM.Models
{
    public class ClienteRankingModel
    {
        public string ClaveCliente { get; set; }
        public string Nombre { get; set; }

        public decimal VentaAnterior { get; set; }
        public decimal VentaActual { get; set; } 

        public decimal Diferencia => VentaActual - VentaAnterior;
        public double PorcentajeCambio
        {
            get
            {
                if ( VentaAnterior == 0 ) return VentaActual > 0 ? 100 : 0;
                return ( double ) ( ( VentaActual - VentaAnterior ) / VentaAnterior ) * 100;
            }
        }

        // Estado visual para la UI
        public string Estado => Diferencia < 0 ? "⚠️ En Riesgo" : "✅ Creciendo";
    }
}
