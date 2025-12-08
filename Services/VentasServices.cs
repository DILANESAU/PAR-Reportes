using System;
using System.Collections.Generic;
using System.Configuration;

using Microsoft.Data.SqlClient;

using WPF_PAR.Core;
using WPF_PAR.MVVM.Models;

namespace WPF_PAR.Services
{
    public class VentasServices
    {
        private readonly SqlHelper _sqlHelper;
        public VentasServices()
        {
            _sqlHelper = new SqlHelper();
        }

        public async Task<List<VentasModel>> ObtenerVentasAsync(int sucursalId, int anio, int mes)
        {
            string query = @"
                SELECT 
                    v.FechaEmision, 
                    v.MovID, 
                    v.Mov, 
                    ISNULL(c.Nombre, 'Cliente General') as Cliente, 
                    v.PrecioTotal 
                FROM Venta v
                LEFT JOIN Cte c ON v.Cliente = c.Cliente
                WHERE 
                    v.Estatus = 'CONCLUIDO'
                    AND v.Sucursal = @Sucursal 
                    AND v.Ejercicio = @Ejercicio 
                    AND v.Periodo = @Periodo 
                    AND v.Mov Like 'Factura%'";

            // Definimos los parámetros limpios
            var parametros = new Dictionary<string, object>
            {
                { "@Sucursal", sucursalId },
                { "@Ejercicio", anio },
                { "@Periodo", mes }
            };


            // Ejecutamos usando el Helper
            // La expresión lambda define cómo convertir cada fila del lector en un VentasModel
            return await _sqlHelper.QueryAsync(query, parametros, lector => new VentasModel
            {
                Fecha = Convert.ToDateTime(lector["FechaEmision"]),
                MovID = lector["MovID"].ToString(),
                Mov = lector["Mov"].ToString(),
                Cliente = lector["Cliente"].ToString(),
                PrecioTotal = Convert.ToDecimal(lector["PrecioTotal"])
            });
        }
        public async Task<List<VentasModel>> ObtenerVentaAnualAsync(int sucursalId, int anio)
        {
            // Query agrupada por MES (Periodo)
            string query = @"
             SELECT 
                v.Periodo AS Mes,
                SUM(v.PrecioTotal) AS TotalMensual
            FROM Venta v
            WHERE 
                v.Estatus = 'CONCLUIDO'
                AND v.Sucursal = @Sucursal 
                AND v.Ejercicio = @Ejercicio 
                AND v.Mov Like 'Factura%'
            GROUP BY v.Periodo
            ORDER BY v.Periodo";

            var parametros = new Dictionary<string, object>
            {
                { "@Sucursal", sucursalId },
                { "@Ejercicio", anio }
            };

            return await _sqlHelper.QueryAsync(query, parametros, lector => new VentasModel
            {
                // Usamos la propiedad 'Mov' temporalmente para guardar el número de mes
                // O podrías crear un modelo nuevo 'VentaMensualModel', pero reciclaremos este.
                Mov = lector["Mes"].ToString(),
                PrecioTotal = Convert.ToDecimal(lector["TotalMensual"])
            });
        }
    }
}