using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using WPF_PAR.MVVM.Models;
using WPF_PAR.Repositories; // Importante

namespace WPF_PAR.Services
{
    public class VentasServices
    {
        private readonly IVentasRepository _repository;

        // Ahora pedimos el Repositorio, no el SqlHelper
        public VentasServices(IVentasRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<VentasModel>> ObtenerVentasRangoAsync(int sucursalId, DateTime inicio, DateTime fin)
        {
            // Aquí podrías agregar lógica extra (ej. validar fechas, transformar datos, etc.)
            return await _repository.ObtenerVentasRangoAsync(sucursalId, inicio, fin);
        }

        public async Task<List<VentasModel>> ObtenerVentaAnualAsync(int sucursalId, int anio)
        {
            return await _repository.ObtenerVentaAnualAsync(sucursalId, anio);
        }
    }
}