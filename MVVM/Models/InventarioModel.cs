using System;
using System.Collections.Generic;
using System.Text;

namespace WPF_PAR.MVVM.Models
{
    public class InventarioModel
    {
        public string Clave { get; set; }
        public string Producto { get; set; }
        public string Unidad { get; set; }

        // Dato crudo de la BD
        public double Existencia { get; set; }

        // Dato calculado con el CSV
        public double FactorLitros { get; set; }

        public double TotalLitros => Existencia * FactorLitros;

        // Propiedad visual para saber si es pintura (tiene litros) o accesorio
        public bool EsPintura => FactorLitros > 0;
    }
}
