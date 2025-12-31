using System;
using System.Collections.Generic;
using System.Text;

namespace WPF_PAR.MVVM.Models
{
    public class VentaReporteModel
    {
        public DateTime FechaEmision { get; set; }
        public string Sucursal { get; set; }
        public string MovID { get; set; }
        public string Articulo { get; set; }
        public string Descripcion { get; set; }
        public decimal Descuento { get; set; }
        public double Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public string Cliente { get; set; }

        // --- CORRECCIÓN AQUÍ ---
        // Usamos un campo privado (nullable) para guardar el valor manual si existe
        private decimal? _totalVentaManual;

        public decimal TotalVenta
        {
            get
            {
                // Si le asignamos un valor manual, devuélvelo.
                if ( _totalVentaManual.HasValue )
                    return _totalVentaManual.Value;

                // Si no, calcúlalo como siempre (Fórmula Original)
                return ( ( decimal ) Cantidad * PrecioUnitario ) - Descuento;
            }
            set
            {
                // Aquí permitimos guardar el valor manual
                _totalVentaManual = value;
            }
        }
        // -----------------------

        public string Familia { get; set; }
        public double LitrosUnitarios { get; set; }

        // --- CORRECCIÓN AQUÍ TAMBIÉN ---
        private double? _litrosTotalesManual;

        public double LitrosTotales
        {
            get
            {
                if ( _litrosTotalesManual.HasValue )
                    return _litrosTotalesManual.Value;

                return Cantidad * LitrosUnitarios;
            }
            set
            {
                _litrosTotalesManual = value;
            }
        }
        // -------------------------------

        public string Linea { get; set; }
        public string Color { get; set; }
    }
}