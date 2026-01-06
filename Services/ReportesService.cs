using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

using WPF_PAR.MVVM.Models;

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
            // CORRECCIÓN: Quitamos el LEFT JOIN Cte para evitar duplicados si la tabla de clientes tiene basura.
            // Usamos una subconsulta (SELECT TOP 1...) para obtener el nombre de forma segura.
            string query = @"
    SELECT
        v.FechaEmision,
        vd.Sucursal,
        ISNULL( (SELECT TOP 1 Nombre FROM Cte WHERE Cliente = v.Cliente), 'Cliente General') AS NombreCliente,
        v.MovID,
        vd.Articulo,
        ISNULL(SUM(vd.Cantidad), 0) AS CantidadTotal,
        ISNULL(SUM(vd.Cantidad * vd.Precio), 0) AS ImporteBrutoTotal,
        ISNULL(SUM(vd.DescuentoImporte), 0) AS DescuentoTotal
    FROM
        VentaD vd
        JOIN Venta v ON vd.ID = v.ID
    WHERE
        v.Estatus = 'CONCLUIDO'
        AND vd.Sucursal = @Sucursal
        AND vd.AplicaID IS NOT NULL
        -- RANGO DE FECHAS
        AND v.FechaEmision >= @Inicio 
        AND v.FechaEmision < DATEADD(day, 1, @Fin)
    GROUP BY
        v.FechaEmision, vd.Sucursal, v.Cliente, v.MovID, vd.Articulo";

            var parametros = new Dictionary<string, object>
                {
                    { "@Sucursal", sucursal },
                    { "@Inicio", inicio },
                    { "@Fin", fin }
                };

            return await _sqlHelper.QueryAsync(query, parametros, lector =>
            {
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
            // CORRECCIÓN: Ahora incluimos el Cliente en la agrupación
            string query = @"
        SELECT
            v.Periodo,
            vd.Articulo,
            -- 1. AGREGAMOS EL NOMBRE DEL CLIENTE
            ISNULL((SELECT TOP 1 Nombre FROM Cte WHERE Cliente = v.Cliente), 'Cliente General') AS NombreCliente,
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
            vd.Articulo,
            v.Cliente -- 2. IMPORTANTE: Agrupar también por cliente para no mezclarlos
        ";

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
                    // Fecha (Día 1 del mes correspondiente)
                    FechaEmision = new DateTime(int.Parse(ejercicio), Convert.ToInt32(lector["Periodo"]), 1),

                    Articulo = lector["Articulo"].ToString().Trim(),

                    // 3. ASIGNAMOS EL CLIENTE QUE AHORA SÍ VIENE DE SQL
                    Cliente = lector["NombreCliente"].ToString(),

                    Cantidad = cantidad,

                    // Calculamos Precio Unitario Promedio
                    PrecioUnitario = cantidad != 0 ? ( importeBruto / ( decimal ) cantidad ) : 0,

                    Descuento = descuento

                    // TotalVenta y LitrosTotales se calcularán solos gracias a los cambios que hicimos en el Modelo
                };
            });
        }
    }
}
