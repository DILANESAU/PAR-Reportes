using System;
using System.Collections.Generic;
using System.Text;

using WPF_PAR.MVVM.Models;

namespace WPF_PAR.Services
{
    public class ClientesService
    {
        private readonly SqlHelper _sqlHelper;

        public ClientesService()
        {
            _sqlHelper = new SqlHelper();
        }
        public async Task<List<ClienteRankingModel>> ObtenerReporteAnualClientes(int sucursalId, int anio)
        {
            string query = @"
        SELECT 
            c.Cliente AS Clave,
            ISNULL(c.Nombre, 'Cliente Mostrador') AS Nombre,
            
            -- PIVOTE MANUAL DE MESES
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
            SUM(v.PrecioTotal) > 0 -- Solo clientes que compraron algo en el año
        ORDER BY 
            SUM(v.PrecioTotal) DESC"; // Ordenamos por el que más compró en total

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
        public async Task<List<ClienteRankingModel>> ObtenerRankingClientes(int sucursalId, DateTime inicioActual, DateTime finActual, DateTime inicioAnterior, DateTime finAnterior)
        {

            string query = @"
                SELECT 
                    c.Cliente AS Clave,
                    ISNULL(c.Nombre, 'Cliente Mostrador') AS Nombre,
                    
                    -- Venta del rango seleccionado (ACTUAL)
                    ISNULL(SUM(CASE 
                        WHEN v.FechaEmision >= @Inicio AND v.FechaEmision < DATEADD(day, 1, @Fin) 
                        THEN v.PrecioTotal 
                        ELSE 0 
                    END), 0) AS VentaActual,
                    
                    -- Venta del rango espejo (ANTERIOR)
                    ISNULL(SUM(CASE 
                        WHEN v.FechaEmision >= @InicioAnt AND v.FechaEmision < DATEADD(day, 1, @FinAnt) 
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

            // NOTA SQL: Usamos DATEADD(day, 1, @Fin) y '<' para incluir todas las horas del último día seleccionado.

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
                //VentaAnterior = Convert.ToDecimal(lector["VentaAnterior"]),
                //VentaActual = Convert.ToDecimal(lector["VentaActual"])
            });
        }
    }
}
