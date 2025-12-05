using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Text;

namespace WPF_PAR.MVVM.Models
{
    public class VentasModel
    {
        public DateTime Fecha {  get; set; }
        public string MovID { get; set; }
        public string Mov {  get; set; }
        public string Cliente { get; set; }
        public decimal PrecioTotal { get; set; }

        public string FechaCorta => Fecha.ToString("dd/MM/yyyy");
    }
}
