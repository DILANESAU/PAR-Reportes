using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using WPF_PAR.MVVM.Models;
using WPF_PAR.Repositories;

namespace WPF_PAR.Services
{
    public class ReportesService
    {
        private readonly IReportesRepository _repository;

        public ReportesService(IReportesRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<VentaReporteModel>> ObtenerVentasBrutasRango(int sucursal, DateTime inicio, DateTime fin)
        {
            return await _repository.ObtenerVentasBrutasRangoAsync(sucursal, inicio, fin);
        }

        public async Task<List<VentaReporteModel>> ObtenerHistoricoAnualPorArticulo(string ejercicio, string sucursal)
        {
            return await _repository.ObtenerHistoricoAnualPorArticuloAsync(ejercicio, sucursal);
        }
    }
}