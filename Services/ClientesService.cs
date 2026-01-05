using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using WPF_PAR.MVVM.Models;
using WPF_PAR.Repositories;

namespace WPF_PAR.Services
{
    public class ClientesService
    {
        private readonly IClientesRepository _repository;

        public ClientesService(IClientesRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<ClienteRankingModel>> ObtenerReporteAnualClientes(int sucursalId, int anio)
        {
            return await _repository.ObtenerReporteAnualClientesAsync(sucursalId, anio);
        }

        public async Task<List<ClienteRankingModel>> ObtenerRankingClientes(int sucursalId, DateTime inicioActual, DateTime finActual, DateTime inicioAnterior, DateTime finAnterior)
        {
            return await _repository.ObtenerRankingClientesAsync(sucursalId, inicioActual, finActual, inicioAnterior, finAnterior);
        }
    }
}