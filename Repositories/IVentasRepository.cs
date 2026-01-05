using System;
using System.Collections.Generic;
using System.Text;

using WPF_PAR.MVVM.Models;

namespace WPF_PAR.Repositories
{
    public interface IVentasRepository
    {
        Task<List<VentasModel>> ObtenerVentasRangoAsync(int sucursalId, DateTime inicio, DateTime fin);
        Task<List<VentasModel>> ObtenerVentaAnualAsync(int sucursalId, int anio);
    }
}
