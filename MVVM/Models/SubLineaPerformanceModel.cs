using System;
using System.Collections.Generic;
using System.Text;

namespace WPF_PAR.MVVM.Models
{
    public class PeriodoBloque
    {
        public string Etiqueta { get; set; }
        public decimal Valor { get; set; }
        public double Litros { get; set; }
        public bool EsFuturo { get; set; }
    }

    public class SubLineaPerformanceModel
    {
        public string Nombre { get; set; }
        public decimal VentaTotal { get; set; } 
        public double LitrosTotales { get; set; }
        public List<PeriodoBloque> Bloques { get; set; }

        public string TopProductoNombre { get; set; }
        public decimal TopProductoVenta { get; set; }
        public decimal Crecimiento { get; set; }
        public bool EsPositivo { get; set; }
    }
}