using System;
using System.Collections.Generic;
using System.Text;

namespace WPF_PAR.MVVM.Models
{
    public class VentaReporteModel
    {
        // --- DATOS DE CABECERA ---
        public DateTime FechaEmision { get; set; }
        public string Sucursal { get; set; }
        public string Mov { get; set; }      // Traído de VentasModel
        public string MovID { get; set; }
        public string Cliente { get; set; }

        // --- DATOS DE DETALLE (PRODUCTO) ---
        public string Articulo { get; set; }
        public string Descripcion { get; set; }
        public string Familia { get; set; }
        public string Linea { get; set; }
        public string Color { get; set; }

        // --- VALORES NUMÉRICOS ---
        public double Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; }

        // Propiedad calculada: Total de la línea (Cantidad * Precio - Descuento)
        public decimal TotalVenta { get; set; }

        // --- LITROS ---
        public double LitrosUnitarios { get; set; }
        public double LitrosTotales => Cantidad * LitrosUnitarios;

        // --- HELPERS VISUALES ---
        // Traído de VentasModel para facilitar formateo en listas
        public string FechaCorta => FechaEmision.ToString("dd/MM/yyyy");
    }
}