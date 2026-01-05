using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using WPF_PAR.MVVM.Models;
using WPF_PAR.Services;

namespace WPF_PAR.Repositories
{
    public class ClientesRepository : IClientesRepository
    {
        private readonly SqlHelper _sqlHelper;

        public ClientesRepository(SqlHelper sqlHelper)
        {
            _sqlHelper = sqlHelper;
        }

        public async Task<List<ClienteRankingModel>> ObtenerReporteAnualClientesAsync(int sucursalId, int anio)
        {
            string query = @"
                SELECT 
                    c.Cliente AS Clave,
                    ISNULL(c.Nombre, 'Cliente Mostrador') AS Nombre,
                    SUM(CASE WHEN MONTH(v.FechaEmision) = 1 THEN v.PrecioTotal ELSE 0 END) AS Ene,
                    SUM(CASE WHEN MONTH(v.FechaEmision) = 2 THEN v.PrecioTotal ELSE 0 END) AS Feb,
                    SUM(CASE WHEN MONTH(v.FechaEmision) = 3 THEN v.PrecioTotal ELSE 0 END) AS Mar,
                    SUM(CASE WHEN MONTH(v.FechaEmision) = 4 THEN v.PrecioTotal ELSE 0 END) AS Abr,
                    SUM(CASE WHEN MONTH(v.FechaEmision) = 5 THEN v.PrecioTotal ELSE 0 END) AS May,
                    SUM(CASE WHEN MONTH(v.FechaEmision) = 6 THEN v.PrecioTotal ELSE 0 END) AS Jun,
                    SUM(CASE WHEN MONTH(v.FechaEmision) = 7 THEN v.PrecioTotal ELSE 0 END) AS Jul,
                    SUM(CASE WHEN MONTH(v.FechaEmision) = 8 THEN v.PrecioTotal ELSE 0 END) AS Ago,
                    SUM(CASE WHEN MONTH(v.FechaEmision) = 9 THEN v.PrecioTotal ELSE 0 END) AS Sep,
                    SUM(CASE WHEN MONTH(v.FechaEmision) = 10 THEN v.PrecioTotal ELSE 0 END) AS Oct,
                    SUM(CASE WHEN MONTH(v.FechaEmision) = 11 THEN v.PrecioTotal ELSE 0 END) AS Nov,
                    SUM(CASE WHEN MONTH(v.FechaEmision) = 12 THEN v.PrecioTotal ELSE 0 END) AS Dic
                FROM Venta v
                LEFT JOIN Cte c ON v.Cliente = c.Cliente
                WHERE 
                    v.Estatus = 'CONCLUIDO'
                    AND v.Sucursal = @Sucursal
                    AND YEAR(v.FechaEmision) = @Anio
                GROUP BY 
                    c.Cliente, c.Nombre
                HAVING 
                    SUM(v.PrecioTotal) > 0
                ORDER BY 
                    SUM(v.PrecioTotal) DESC";

            var parametros = new Dictionary<string, object>
            {
                { "@Sucursal", sucursalId },
                { "@Anio", anio }
            };

            return await _sqlHelper.QueryAsync(query, parametros, lector => new ClienteRankingModel
            {
                ClaveCliente = lector["Clave"].ToString(),
                Nombre = lector["Nombre"].ToString(),
                Enero = Convert.ToDecimal(lector["Ene"]),
                Febrero = Convert.ToDecimal(lector["Feb"]),
                Marzo = Convert.ToDecimal(lector["Mar"]),
                Abril = Convert.ToDecimal(lector["Abr"]),
                Mayo = Convert.ToDecimal(lector["May"]),
                Junio = Convert.ToDecimal(lector["Jun"]),
                Julio = Convert.ToDecimal(lector["Jul"]),
                Agosto = Convert.ToDecimal(lector["Ago"]),
                Septiembre = Convert.ToDecimal(lector["Sep"]),
                Octubre = Convert.ToDecimal(lector["Oct"]),
                Noviembre = Convert.ToDecimal(lector["Nov"]),
                Diciembre = Convert.ToDecimal(lector["Dic"])
            });
        }

        public async Task<List<ClienteRankingModel>> ObtenerRankingClientesAsync(int sucursalId, DateTime inicioActual, DateTime finActual, DateTime inicioAnterior, DateTime finAnterior)
        {
            string query = @"
                SELECT 
                    c.Cliente AS Clave,
                    ISNULL(c.Nombre, 'Cliente Mostrador') AS Nombre,
                    ISNULL(SUM(CASE 
                        WHEN v.FechaEmision >= @InicioActual AND v.FechaEmision < DATEADD(day, 1, @FinActual) 
                        THEN v.PrecioTotal 
                        ELSE 0 
                    END), 0) AS VentaActual,
                    ISNULL(SUM(CASE 
                        WHEN v.FechaEmision >= @InicioAnterior AND v.FechaEmision < DATEADD(day, 1, @FinAnterior) 
                        THEN v.PrecioTotal 
                        ELSE 0 
                    END), 0) AS VentaAnterior
                FROM Venta v
                LEFT JOIN Cte c ON v.Cliente = c.Cliente
                WHERE 
                    v.Estatus = 'CONCLUIDO'
                    AND v.Sucursal = @Sucursal
                   AND (
                        (v.FechaEmision >= @InicioActual AND v.FechaEmision < DATEADD(day, 1, @FinActual)) OR 
                        (v.FechaEmision >= @InicioAnterior AND v.FechaEmision < DATEADD(day, 1, @FinAnterior))
                    )
                GROUP BY 
                    c.Cliente, c.Nombre
                HAVING 
                    SUM(v.PrecioTotal) > 0
                ORDER BY 
                    VentaActual DESC";

            var parametros = new Dictionary<string, object>
            {
                { "@Sucursal", sucursalId },
                { "@InicioActual", inicioActual },
                { "@FinActual", finActual },
                { "@InicioAnterior", inicioAnterior },
                { "@FinAnterior", finAnterior }
            };

            return await _sqlHelper.QueryAsync(query, parametros, lector => new ClienteRankingModel
            {
                ClaveCliente = lector["Clave"].ToString(),
                Nombre = lector["Nombre"].ToString(),
                // Nota: Aquí solo estás mapeando lo básico en tu modelo original, 
                // si agregaste VentaActual/Anterior al modelo ClienteRankingModel, mapealo aquí.
            });
        }
    }
}