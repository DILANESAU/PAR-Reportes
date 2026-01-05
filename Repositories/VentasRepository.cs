using System;
using System.Collections.Generic;
using System.Linq; // Necesario para .Any()
using System.Threading.Tasks;

using WPF_PAR.Core; // Necesario para Session
using WPF_PAR.MVVM.Models;
using WPF_PAR.Services;

namespace WPF_PAR.Repositories
{
    public class VentasRepository : IVentasRepository
    {
        private readonly SqlHelper _sqlHelper;

        public VentasRepository(SqlHelper sqlHelper)
        {
            _sqlHelper = sqlHelper;
        }

        // --- MÉTODO 1: VENTAS POR RANGO DE FECHAS ---
        public async Task<List<VentasModel>> ObtenerVentasRangoAsync(int sucursalId, DateTime inicio, DateTime fin)
        {
            // 1. Generamos el filtro dinámico
            string filtroSucursal = GenerarFiltroSucursal(sucursalId);

            // 2. Inyectamos el filtro en la Query
            string query = $@"
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
                {filtroSucursal}  /* <--- Filtro dinámico aquí */
                AND v.FechaEmision >= @Inicio 
                AND v.FechaEmision < DATEADD(day, 1, @Fin) 
                AND v.Mov Like 'Factura%'";

            var parametros = new Dictionary<string, object>
            {
                { "@Inicio", inicio },
                { "@Fin", fin }
            };

            // 3. Solo agregamos el parámetro si es una sucursal específica
            if ( sucursalId != 0 )
            {
                parametros.Add("@Sucursal", sucursalId);
            }

            return await _sqlHelper.QueryAsync(query, parametros, lector => new VentasModel
            {
                Fecha = Convert.ToDateTime(lector["FechaEmision"]),
                MovID = lector["MovID"].ToString(),
                Mov = lector["Mov"].ToString(),
                Cliente = lector["Cliente"].ToString(),
                PrecioTotal = Convert.ToDecimal(lector["PrecioTotal"])
            });
        }

        // --- MÉTODO 2: VENTAS ANUALES (PARA GRÁFICOS) ---
        public async Task<List<VentasModel>> ObtenerVentaAnualAsync(int sucursalId, int anio)
        {
            // 1. Generamos el filtro dinámico
            string filtroSucursal = GenerarFiltroSucursal(sucursalId);

            string query = $@"
             SELECT 
                v.Periodo AS Mes,
                SUM(v.PrecioTotal) AS TotalMensual
            FROM Venta v
            WHERE 
                v.Estatus = 'CONCLUIDO'
                {filtroSucursal} /* <--- Filtro dinámico aquí */
                AND v.Ejercicio = @Ejercicio 
                AND v.Mov Like 'Factura%'
            GROUP BY v.Periodo
            ORDER BY v.Periodo";

            var parametros = new Dictionary<string, object>
            {
                { "@Ejercicio", anio }
            };

            // 3. Solo agregamos el parámetro si es una sucursal específica
            if ( sucursalId != 0 )
            {
                parametros.Add("@Sucursal", sucursalId);
            }

            return await _sqlHelper.QueryAsync(query, parametros, lector => new VentasModel
            {
                Mov = lector["Mes"].ToString(),
                PrecioTotal = Convert.ToDecimal(lector["TotalMensual"])
            });
        }

        // --- HELPER PRIVADO (REUTILIZABLE) ---
        private string GenerarFiltroSucursal(int sucursalId)
        {
            var usuario = Session.UsuarioActual;

            if ( sucursalId == 0 ) // "TODAS" o "CONSOLIDADO"
            {
                if ( usuario != null && usuario.Rol == "Admin" )
                {
                    return ""; // Admin ve todo (sin filtro)
                }
                else if ( usuario != null )
                {
                    // Usuario ve sus permitidas (IN 1, 2, 3)
                    var ids = usuario.SucursalesPermitidas != null && usuario.SucursalesPermitidas.Any()
                        ? string.Join(",", usuario.SucursalesPermitidas)
                        : "0";
                    return $"AND v.Sucursal IN ({ids})";
                }
                return "AND 1=0"; // Bloqueo de seguridad
            }
            else
            {
                // Sucursal específica
                return "AND v.Sucursal = @Sucursal";
            }
        }

        public Task<List<VentasModel>> ObtenerReporteVentasAsync(int sucursalId, DateTime inicio, DateTime fin)
        {
            throw new NotImplementedException();
        }

        public Task<List<VentasModel>> ObtenerVentasAnualAsync(int sucursalId, int anio)
        {
            throw new NotImplementedException();
        }
    }
}