using System;
using System.Collections.Generic;
using System.Text;

using WPF_PAR.MVVM.Models;

namespace WPF_PAR.Repositories
{
    public interface IClientesRepository
    {
        Task<List<ClienteRankingModel>> ObtenerReporteAnualClientesAsync(int sucursalId, int anio);
        Task<List<ClienteRankingModel>> ObtenerRankingClientesAsync(int sucursalId, DateTime inicioActual, DateTime finActual, DateTime inicioAnterior, DateTime finAnterior);
    }
}
