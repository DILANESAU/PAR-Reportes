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

        public async Task<List<ClienteRankingModel>> ObtenerRankingClientes(int sucursalId, DateTime inicio, DateTime fin)
        {
            // Calculamos el "Periodo Espejo" (Mismas fechas, año anterior)
            DateTime inicioAnt = inicio.AddYears(-1);
            DateTime finAnt = fin.AddYears(-1);

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
                        (v.FechaEmision >= @Inicio AND v.FechaEmision < DATEADD(day, 1, @Fin)) OR 
                        (v.FechaEmision >= @InicioAnt AND v.FechaEmision < DATEADD(day, 1, @FinAnt))
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
                { "@Inicio", inicio },
                { "@Fin", fin },
                { "@InicioAnt", inicioAnt },
                { "@FinAnt", finAnt }
            };

            return await _sqlHelper.QueryAsync(query, parametros, lector => new ClienteRankingModel
            {
                ClaveCliente = lector["Clave"].ToString(),
                Nombre = lector["Nombre"].ToString(),
                VentaAnterior = Convert.ToDecimal(lector["VentaAnterior"]),
                VentaActual = Convert.ToDecimal(lector["VentaActual"])
            });
        }
    }
}
