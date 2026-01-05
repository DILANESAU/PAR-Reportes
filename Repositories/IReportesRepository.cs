using System;
using System.Collections.Generic;
using System.Text;

using WPF_PAR.MVVM.Models;

namespace WPF_PAR.Repositories
{
    public interface IReportesRepository
    {
        Task<List<VentaReporteModel>> ObtenerVentasBrutasRangoAsync(int sucursal, DateTime inicio, DateTime fin);
        Task<List<VentaReporteModel>> ObtenerHistoricoAnualPorArticuloAsync(string ejercicio, string sucursal);
    }
}
