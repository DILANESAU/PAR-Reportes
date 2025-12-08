using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using WPF_PAR.MVVM.Models;
using Microsoft.Data.SqlClient;

namespace WPF_PAR.Services
{
    public class ReportesService
    {
        private readonly SqlHelper _sqlHelper;

        public ReportesService()
        {
            _sqlHelper = new SqlHelper();
        }

        public async Task<List<VentaReporteModel>> ObtenerVentasBrutas(string ejercicio, string sucursal, int mes)
        {
            string query = @"
               SELECT
            v.FechaEmision,
            vd.Sucursal,
            ISNULL(c.Nombre, 'Cliente General') AS NombreCliente,
            v.MovID,
            vd.Articulo,
            SUM(vd.Cantidad) AS CantidadTotal,
            
            -- BLINDAJE: Calculamos el importe bruto directo en SQL
            SUM(vd.Cantidad * vd.Precio) AS ImporteBrutoTotal, 
            
            SUM(ISNULL(vd.DescuentoImporte, 0)) AS DescuentoTotal
        FROM
            VentaD vd
            JOIN Venta v ON vd.ID = v.ID
            LEFT JOIN Cte c ON v.Cliente = c.Cliente
        WHERE
            v.Ejercicio = @Ejercicio AND
            v.Periodo = @Periodo AND
            vd.Sucursal = @Sucursal AND
            vd.AplicaID IS NOT NULL AND
            v.Estatus = 'CONCLUIDO'
        GROUP BY
            v.FechaEmision,
            vd.Sucursal,
            c.Nombre,
            v.MovID,
            vd.Articulo";

            var parametros = new Dictionary<string, object>
            {
                { "@Ejercicio", ejercicio },
                { "@Sucursal", sucursal },
                { "@Periodo", mes }
            };

            return await _sqlHelper.QueryAsync(query, parametros, lector =>
            {
                // Leemos los valores calculados por SQL
                decimal importeBruto = Convert.ToDecimal(lector["ImporteBrutoTotal"]);
                decimal descuento = Convert.ToDecimal(lector["DescuentoTotal"]);
                double cantidad = Convert.ToDouble(lector["CantidadTotal"]);

                return new VentaReporteModel
                {
                    FechaEmision = Convert.ToDateTime(lector["FechaEmision"]),
                    Sucursal = lector["Sucursal"].ToString(),
                    MovID = lector["MovID"].ToString(),
                    Articulo = lector["Articulo"].ToString().Trim(),
                    Cantidad = cantidad,
                    Cliente = lector["NombreCliente"].ToString(),

                    Descuento = descuento,

                    PrecioUnitario = cantidad > 0 ? ( importeBruto / ( decimal ) cantidad ) : 0,
                };
            });
        }
    }
}
