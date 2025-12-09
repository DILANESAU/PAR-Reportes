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
        public async Task<List<VentaReporteModel>> ObtenerVentasBrutasRango(int sucursal, DateTime inicio, DateTime fin)
        {
            string query = @"
            SELECT
                v.FechaEmision,
                vd.Sucursal,
                ISNULL(c.Nombre, 'Cliente General') AS NombreCliente,
                v.MovID,
                vd.Articulo,
                ISNULL(SUM(vd.Cantidad), 0) AS CantidadTotal,
                ISNULL(SUM(vd.Cantidad * vd.Precio), 0) AS ImporteBrutoTotal,
                ISNULL(SUM(vd.DescuentoImporte), 0) AS DescuentoTotal
            FROM
                VentaD vd
                JOIN Venta v ON vd.ID = v.ID
                LEFT JOIN Cte c ON v.Cliente = c.Cliente
            WHERE
                v.Estatus = 'CONCLUIDO'
                AND vd.Sucursal = @Sucursal
                AND vd.AplicaID IS NOT NULL
                -- RANGO DE FECHAS
                AND v.FechaEmision >= @Inicio 
                AND v.FechaEmision < DATEADD(day, 1, @Fin)
            GROUP BY
                v.FechaEmision, vd.Sucursal, c.Nombre, v.MovID, vd.Articulo";

            var parametros = new Dictionary<string, object>
        {
            { "@Sucursal", sucursal },
            { "@Inicio", inicio },
            { "@Fin", fin }
        };

            return await _sqlHelper.QueryAsync(query, parametros, lector =>
            {
                // (Mismo mapeo seguro que hicimos antes)
                decimal importeBruto = lector["ImporteBrutoTotal"] != DBNull.Value ? Convert.ToDecimal(lector["ImporteBrutoTotal"]) : 0m;
                decimal descuento = lector["DescuentoTotal"] != DBNull.Value ? Convert.ToDecimal(lector["DescuentoTotal"]) : 0m;
                double cantidad = lector["CantidadTotal"] != DBNull.Value ? Convert.ToDouble(lector["CantidadTotal"]) : 0d;

                return new VentaReporteModel
                {
                    FechaEmision = Convert.ToDateTime(lector["FechaEmision"]),
                    Sucursal = lector["Sucursal"].ToString(),
                    MovID = lector["MovID"].ToString(),
                    Articulo = lector["Articulo"].ToString().Trim(),
                    Cantidad = cantidad,
                    Cliente = lector["NombreCliente"].ToString(),
                    Descuento = descuento,
                    PrecioUnitario = cantidad != 0 ? ( importeBruto / ( decimal ) cantidad ) : 0
                };
            });
        }
        public async Task<List<VentaReporteModel>> ObtenerHistoricoAnualPorArticulo(string ejercicio, string sucursal)
        {
            // Traemos la venta de TODO EL AÑO, pero agrupada por Mes y Artículo.
            // Esto es mucho más ligero que traer ticket por ticket.
            string query = @"
        SELECT
            v.Periodo,
            vd.Articulo,
            SUM(vd.Cantidad) AS CantidadTotal,
            ISNULL(SUM(vd.Cantidad * vd.Precio), 0) AS ImporteBrutoTotal,
            ISNULL(SUM(vd.DescuentoImporte), 0) AS DescuentoTotal
        FROM
            VentaD vd
            JOIN Venta v ON vd.ID = v.ID
        WHERE
            v.Ejercicio = @Ejercicio AND
            vd.Sucursal = @Sucursal AND
            vd.AplicaID IS NOT NULL AND
            v.Estatus = 'CONCLUIDO'
        GROUP BY
            v.Periodo,
            vd.Articulo";

            var parametros = new Dictionary<string, object>
    {
        { "@Ejercicio", ejercicio },
        { "@Sucursal", sucursal }
    };

            return await _sqlHelper.QueryAsync(query, parametros, lector =>
            {
                decimal importeBruto = lector["ImporteBrutoTotal"] != DBNull.Value ? Convert.ToDecimal(lector["ImporteBrutoTotal"]) : 0m;
                decimal descuento = lector["DescuentoTotal"] != DBNull.Value ? Convert.ToDecimal(lector["DescuentoTotal"]) : 0m;
                double cantidad = lector["CantidadTotal"] != DBNull.Value ? Convert.ToDouble(lector["CantidadTotal"]) : 0d;

                return new VentaReporteModel
                {
                    // Guardamos el Mes en una propiedad auxiliar o usamos FechaEmision (Dia 1 del mes)
                    FechaEmision = new DateTime(int.Parse(ejercicio), Convert.ToInt32(lector["Periodo"]), 1),
                    Articulo = lector["Articulo"].ToString().Trim(),
                    Cantidad = cantidad,
                    PrecioUnitario = cantidad != 0 ? ( importeBruto / ( decimal ) cantidad ) : 0,
                    Descuento = descuento
                    // TotalVenta se calcula solo en el modelo
                };
            });
        }
    }
}
