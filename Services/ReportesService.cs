using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        // =========================================================================
        // SECCIÓN 1: REPORTES DETALLADOS (Productos, Líneas, Familias)
        // Usado por: FamiliaViewModel
        // =========================================================================

        public async Task<List<VentaReporteModel>> ObtenerVentasBrutasRango(int sucursal, DateTime inicio, DateTime fin)
        {
            string query = @"
            SELECT
                v.FechaEmision,
                vd.Sucursal,
                ISNULL((SELECT TOP 1 Nombre FROM Cte WHERE Cliente = v.Cliente), 'Cliente General') AS NombreCliente,
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
            string query = @"
            SELECT
                v.Periodo,
                vd.Articulo,
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
                v.Periodo, vd.Articulo, v.Cliente";

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
                    FechaEmision = new DateTime(int.Parse(ejercicio), Convert.ToInt32(lector["Periodo"]), 1),
                    Articulo = lector["Articulo"].ToString().Trim(),
                    Cliente = lector["NombreCliente"].ToString(),
                    Cantidad = cantidad,
                    PrecioUnitario = cantidad != 0 ? ( importeBruto / ( decimal ) cantidad ) : 0,
                    Descuento = descuento
                };
            });
        }

        // =========================================================================
        // SECCIÓN 2: REPORTES GENERALES / CABECERA (KPIs, Dashboard)
        // Usado por: DashboardViewModel
        // (Anteriormente en VentasServices)
        // =========================================================================

        public async Task<List<VentaReporteModel>> ObtenerVentasRangoAsync(int sucursalId, DateTime inicio, DateTime fin)
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
                AND v.FechaEmision >= @Inicio 
                AND v.FechaEmision < DATEADD(day, 1, @Fin) 
                AND v.Mov Like 'Factura%'";

            var parametros = new Dictionary<string, object>
            {
                { "@Sucursal", sucursalId },
                { "@Inicio", inicio },
                { "@Fin", fin }
            };

            return await _sqlHelper.QueryAsync(query, parametros, lector => new VentaReporteModel
            {
                FechaEmision = Convert.ToDateTime(lector["FechaEmision"]),
                MovID = lector["MovID"].ToString(),
                Mov = lector["Mov"].ToString(),
                Cliente = lector["Cliente"].ToString(),
                // Simulamos detalle para que TotalVenta funcione
                Cantidad = 1,
                PrecioUnitario = Convert.ToDecimal(lector["PrecioTotal"]),
                Descuento = 0
            });
        }

        public async Task<List<VentaReporteModel>> ObtenerVentaAnualAsync(int sucursalId, int anio)
        {
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

            return await _sqlHelper.QueryAsync(query, parametros, lector => new VentaReporteModel
            {
                Mov = lector["Mes"].ToString(), // Guardamos el mes aquí
                Cantidad = 1,
                PrecioUnitario = Convert.ToDecimal(lector["TotalMensual"]),
                Descuento = 0,
                FechaEmision = new DateTime(anio, 1, 1) // Fecha dummy
            });
        }
    }
}