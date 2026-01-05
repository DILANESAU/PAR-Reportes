using System;
using System.Collections.Generic;
using System.Linq; // Necesario para .Any()
using System.Threading.Tasks;

using WPF_PAR.Core;
using WPF_PAR.MVVM.Models;
using WPF_PAR.Services;

namespace WPF_PAR.Repositories
{
    public class ReportesRepository : IReportesRepository
    {
        private readonly SqlHelper _sqlHelper;

        public ReportesRepository(SqlHelper sqlHelper)
        {
            _sqlHelper = sqlHelper;
        }

        // --- MÉTODO 1: VENTAS POR RANGO (YA ESTABA BIEN) ---
        public async Task<List<VentaReporteModel>> ObtenerVentasBrutasRangoAsync(int sucursal, DateTime inicio, DateTime fin)
        {
            string filtroSucursal = GenerarFiltroSucursal(sucursal); // Refactoricé la lógica para reusarla

            string query = $@"
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
                    {filtroSucursal}
                    AND vd.AplicaID IS NOT NULL
                    AND v.FechaEmision >= @Inicio 
                    AND v.FechaEmision < DATEADD(day, 1, @Fin)
                GROUP BY
                    v.FechaEmision, vd.Sucursal, v.Cliente, v.MovID, vd.Articulo";

            var parametros = new Dictionary<string, object>
            {
                { "@Inicio", inicio },
                { "@Fin", fin }
            };

            if ( sucursal != 0 ) parametros.Add("@Sucursal", sucursal);

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

        // --- MÉTODO 2: HISTÓRICO ANUAL (CORREGIDO) ---
        public async Task<List<VentaReporteModel>> ObtenerHistoricoAnualPorArticuloAsync(string ejercicio, string sucursalStr)
        {
            // Convertimos el string a int para poder usar la misma lógica
            int.TryParse(sucursalStr, out int sucursalId);

            // Aplicamos la misma lógica dinámica
            string filtroSucursal = GenerarFiltroSucursal(sucursalId);

            string query = $@"
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
                    v.Ejercicio = @Ejercicio 
                    {filtroSucursal}  /* <--- AQUI FALTABA EL FILTRO DINÁMICO */
                    AND vd.AplicaID IS NOT NULL 
                    AND v.Estatus = 'CONCLUIDO'
                GROUP BY
                    v.Periodo,
                    vd.Articulo,
                    v.Cliente";

            var parametros = new Dictionary<string, object>
            {
                { "@Ejercicio", ejercicio }
            };

            if ( sucursalId != 0 ) parametros.Add("@Sucursal", sucursalId);

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

        // --- HELPER PRIVADO PARA NO REPETIR CÓDIGO ---
        private string GenerarFiltroSucursal(int sucursalId)
        {
            var usuario = Session.UsuarioActual;

            if ( sucursalId == 0 ) // Opción "TODAS" o "CONSOLIDADO"
            {
                if ( usuario != null && usuario.Rol == "Admin" )
                {
                    return ""; // Admin ve todo
                }
                else if ( usuario != null )
                {
                    // Usuario normal ve sus permitidas
                    var ids = usuario.SucursalesPermitidas != null && usuario.SucursalesPermitidas.Any()
                        ? string.Join(",", usuario.SucursalesPermitidas)
                        : "0";
                    return $"AND vd.Sucursal IN ({ids})";
                }
                return "AND 1=0"; // Seguridad por si usuario es null
            }
            else
            {
                return "AND vd.Sucursal = @Sucursal";
            }
        }
    }
}