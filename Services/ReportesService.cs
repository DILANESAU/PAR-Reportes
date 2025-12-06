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
                    c.Nombre,
                    v.MovID,
                    vd.Articulo,
                    vd.Cantidad,
                    vd.Precio as CostoUnitario,
                    ISNULL(vd.DescuentoImporte, 0) AS [DescuentoImporte]
                FROM
                    VentaD vd
                    JOIN ArtR a ON a.Articulo = vd.Articulo
                    JOIN Venta v ON vd.ID = v.ID
                    LEFT JOIN Cte c ON v.Cliente = c.Cliente
                WHERE
                    v.Ejercicio = @Ejercicio AND
                    v.Periodo = @Periodo AND
                    vd.Sucursal = @Sucursal AND
                    vd.AplicaID IS NOT NULL AND
                    v.Estatus = 'CONCLUIDO'";

            var parametros = new Dictionary<string, object>
            {
                { "@Ejercicio", ejercicio },
                { "@Sucursal", sucursal },
                { "@Periodo", mes }
            };

            return await _sqlHelper.QueryAsync(query, parametros, lector => new VentaReporteModel
            {
                FechaEmision = Convert.ToDateTime(lector["FechaEmision"]),
                Sucursal = lector["Sucursal"].ToString(),
                MovID = lector["MovID"].ToString(),
                Articulo = lector["Articulo"].ToString().Trim(),
                Cantidad = Convert.ToDouble(lector["Cantidad"]),
                PrecioUnitario = Convert.ToDecimal(lector["CostoUnitario"]),
                Cliente = lector["Nombre"].ToString(),
                Descuento = Convert.ToDecimal(lector["DescuentoImporte"])
            });
        }
    }
}
