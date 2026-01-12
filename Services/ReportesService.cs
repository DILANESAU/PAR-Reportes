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
    v.Mov,
    vd.Articulo,

    -- 1. CANTIDAD (Volumen)
    ISNULL(SUM(
        CASE 
            -- Devoluciones: RESTAN cantidad
            WHEN v.Mov LIKE '%Devoluci%n%' THEN (vd.Cantidad * -1)
            
            -- CAMBIO AQUÍ: Usamos '%Bonifica%' para atrapar 'Bonifica Vta' y 'Bonificacion Vta'
            WHEN v.Mov LIKE '%Bonifica%' THEN 0
            
            ELSE vd.Cantidad
        END
    ), 0) AS CantidadTotal,

    -- 2. DINERO (Saldo)
    ISNULL(SUM(
        CASE 
            -- Devoluciones: RESTAN dinero
            WHEN v.Mov LIKE '%Devoluci%n%' THEN ((vd.Cantidad * vd.Precio) * -1)
            
            -- CAMBIO AQUÍ TAMBIÉN: '%Bonifica%' atrapa ambas variaciones
            WHEN v.Mov LIKE '%Bonifica%' THEN ((vd.Cantidad * vd.Precio) * -1)
            
            ELSE (vd.Cantidad * vd.Precio)
        END
    ), 0) AS ImporteBrutoTotal,

    -- 3. DESCUENTOS
    ISNULL(SUM(
        CASE 
           -- Mantenemos consistencia con Devoluciones
           WHEN v.Mov LIKE '%Devoluci%n%' THEN (vd.DescuentoImporte * -1)
           ELSE vd.DescuentoImporte
        END
    ), 0) AS DescuentoTotal

FROM
    VentaD vd
    JOIN Venta v ON vd.ID = v.ID
WHERE
    v.Estatus = 'CONCLUIDO'
    AND vd.Sucursal = @Sucursal
    AND v.FechaEmision >= @Inicio 
    AND v.FechaEmision < DATEADD(day, 1, @Fin)
    
    -- EXCLUSIONES
    AND v.Mov NOT LIKE '%Pedido%'
    AND v.Mov NOT LIKE '%Venta Perdida%'
    AND v.Mov NOT LIKE '%Cotiza%'
    AND v.Mov NOT LIKE '%Carta Porte%'

GROUP BY
    v.FechaEmision, 
    vd.Sucursal, 
    v.Cliente, 
    v.MovID, 
    v.Mov, 
    vd.Articulo";

            var parametros = new Dictionary<string, object>
    {
        { "@Sucursal", sucursal },
        { "@Inicio", inicio },
        { "@Fin", fin }
    };

            return await _sqlHelper.QueryAsync(query, parametros, lector =>
            {
                // Leemos los totales CALCULADOS POR SQL (que ya traen el signo correcto)
                decimal importeTotalSql = lector["ImporteBrutoTotal"] != DBNull.Value ? Convert.ToDecimal(lector["ImporteBrutoTotal"]) : 0m;
                double cantidadSql = lector["CantidadTotal"] != DBNull.Value ? Convert.ToDouble(lector["CantidadTotal"]) : 0d;
                decimal descuento = lector["DescuentoTotal"] != DBNull.Value ? Convert.ToDecimal(lector["DescuentoTotal"]) : 0m;

                // Calculamos precio unitario solo para referencia (visual), evitando división entre cero
                decimal precioUnitarioVisual = 0;
                if ( Math.Abs(cantidadSql) > 0.001 ) // Si cantidad no es cero
                {
                    // Usamos valor absoluto para que el precio unitario se vea "bonito" (positivo) en la tabla
                    // aunque el total sea negativo.
                    precioUnitarioVisual = Math.Abs(importeTotalSql / ( decimal ) cantidadSql);
                }
                else if ( lector["Mov"].ToString().Contains("Bonifica") )
                {
                    // Si es bonificación (Cant=0), el precio unitario visual es el importe total (en negativo)
                    // o lo dejamos en 0 según prefieras.
                    precioUnitarioVisual = importeTotalSql;
                }

                return new VentaReporteModel
                {
                    FechaEmision = Convert.ToDateTime(lector["FechaEmision"]),
                    Sucursal = lector["Sucursal"].ToString(),
                    MovID = lector["MovID"].ToString(),
                    Mov = lector["Mov"].ToString(),
                    Articulo = lector["Articulo"].ToString().Trim(),
                    Cliente = lector["NombreCliente"].ToString(),

                    // ASIGNAMOS DIRECTO DEL SQL
                    Cantidad = cantidadSql,          // Ya viene negativo o 0
                    Descuento = descuento,

                    // IMPORTANTE:
                    // Asegúrate de asignar el TotalVenta DIRECTO del SQL para que la bonificación (Cant=0) tenga valor.
                    // Si tu clase no tiene 'TotalVenta' con 'set', usa 'PrecioUnitario' o ajusta tu modelo.
                    TotalVenta = importeTotalSql,    // <--- ESTO ARREGLA QUE LA BONIFICACIÓN SEA 0

                    PrecioUnitario = Math.Abs(precioUnitarioVisual) // Solo para mostrar
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
        
        -- 1. CANTIDAD (Volumen)
        ISNULL(SUM(
            CASE 
                WHEN v.Mov LIKE '%Devoluci%n%' THEN (vd.Cantidad * -1)
                WHEN v.Mov LIKE '%Bonifica%' THEN 0
                ELSE vd.Cantidad
            END
        ), 0) AS CantidadTotal,

        -- 2. DINERO (Saldo Real con Negativos)
        ISNULL(SUM(
            CASE 
                WHEN v.Mov LIKE '%Devoluci%n%' THEN ((vd.Cantidad * vd.Precio) * -1)
                WHEN v.Mov LIKE '%Bonifica%' THEN ((vd.Cantidad * vd.Precio) * -1)
                ELSE (vd.Cantidad * vd.Precio)
            END
        ), 0) AS ImporteBrutoTotal,

        ISNULL(SUM(
            CASE 
               WHEN v.Mov LIKE '%Devoluci%n%' THEN (vd.DescuentoImporte * -1)
               ELSE vd.DescuentoImporte
            END
        ), 0) AS DescuentoTotal

    FROM
        VentaD vd
        JOIN Venta v ON vd.ID = v.ID
    WHERE
        v.Ejercicio = @Ejercicio AND
        vd.Sucursal = @Sucursal AND
        v.Estatus = 'CONCLUIDO'
        
        -- EXCLUSIONES
        AND v.Mov NOT LIKE '%Pedido%'
        AND v.Mov NOT LIKE '%Venta Perdida%'
        AND v.Mov NOT LIKE '%Cotiza%'
        AND v.Mov NOT LIKE '%Carta Porte%'

    GROUP BY
        v.Periodo, vd.Articulo, v.Cliente";

            var parametros = new Dictionary<string, object>
    {
        { "@Ejercicio", ejercicio },
        { "@Sucursal", sucursal }
    };

            return await _sqlHelper.QueryAsync(query, parametros, lector =>
            {
                // Mapeo corregido para usar los totales calculados por SQL
                decimal importeBruto = lector["ImporteBrutoTotal"] != DBNull.Value ? Convert.ToDecimal(lector["ImporteBrutoTotal"]) : 0m;
                double cantidad = lector["CantidadTotal"] != DBNull.Value ? Convert.ToDouble(lector["CantidadTotal"]) : 0d;

                // Evitamos división entre cero para el precio unitario visual
                decimal precioVisual = 0;
                if ( Math.Abs(cantidad) > 0.001 )
                {
                    precioVisual = Math.Abs(importeBruto / ( decimal ) cantidad);
                }

                return new VentaReporteModel
                {
                    // Creamos una fecha dummy con el mes (Periodo) para que el gráfico sepa dónde ponerlo
                    FechaEmision = new DateTime(int.Parse(ejercicio), Convert.ToInt32(lector["Periodo"]), 1),

                    Articulo = lector["Articulo"].ToString().Trim(),
                    Cliente = lector["NombreCliente"].ToString(),
                    Cantidad = cantidad,

                    // ¡AQUÍ ESTÁ LA CLAVE! Asignamos el dinero directo de SQL
                    TotalVenta = importeBruto,

                    PrecioUnitario = precioVisual,
                    Descuento = lector["DescuentoTotal"] != DBNull.Value ? Convert.ToDecimal(lector["DescuentoTotal"]) : 0m
                };
            });
        }

        // =========================================================================
        // SECCIÓN 2: REPORTES GENERALES / CABECERA (KPIs, Dashboard)
        // Usado por: DashboardViewModel
        // (Anteriormente en VentasServices)
        // =========================================================================

        // En ReportesService.cs

        public async Task<List<VentaReporteModel>> ObtenerVentasRangoAsync(int sucursalId, DateTime inicio, DateTime fin)
        {
            // Lógica para filtrar sucursal: Si es 0 o -1, traemos todas
            string filtroSucursal = sucursalId > 0 ? "AND vd.Sucursal = @Sucursal" : "";

            string query = $@"
    SELECT 
        v.FechaEmision, 
        vd.Sucursal,
        ISNULL(c.Nombre, 'Cliente General') as Cliente, 
        v.Mov,
        vd.Articulo,
        
        -- CALCULAMOS EL NETO REAL (Igual que en el reporte detallado)
        ISNULL(SUM(
            CASE 
                WHEN v.Mov LIKE '%Devoluci%n%' OR v.Mov LIKE '%Bonifica%' 
                THEN ((vd.Cantidad * vd.Precio) * -1)
                ELSE (vd.Cantidad * vd.Precio)
            END
        ), 0) AS ImporteNeto

    FROM VentaD vd
    JOIN Venta v ON vd.ID = v.ID
    LEFT JOIN Cte c ON v.Cliente = c.Cliente
    WHERE 
        v.Estatus = 'CONCLUIDO'
        {filtroSucursal}
        AND v.FechaEmision >= @Inicio 
        AND v.FechaEmision < DATEADD(day, 1, @Fin)
        
        -- EXCLUSIONES
        AND v.Mov NOT LIKE '%Pedido%'
        AND v.Mov NOT LIKE '%Venta Perdida%'
        AND v.Mov NOT LIKE '%Cotiza%'
        AND v.Mov NOT LIKE '%Carta Porte%'

    GROUP BY v.FechaEmision, vd.Sucursal, c.Nombre, v.Mov, vd.Articulo";

            var parametros = new Dictionary<string, object>
    {
        { "@Sucursal", sucursalId },
        { "@Inicio", inicio },
        { "@Fin", fin }
    };

            return await _sqlHelper.QueryAsync(query, parametros, lector => new VentaReporteModel
            {
                FechaEmision = Convert.ToDateTime(lector["FechaEmision"]),
                Sucursal = lector["Sucursal"].ToString(),
                Cliente = lector["Cliente"].ToString(),
                Mov = lector["Mov"].ToString(),
                Articulo = lector["Articulo"].ToString(),

                // ¡AQUÍ ESTÁ EL ARREGLO DEL $0!
                // Asignamos lo que calculó SQL directamente a TotalVenta
                TotalVenta = Convert.ToDecimal(lector["ImporteNeto"])
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

        // En ReportesService.cs

        public async Task<List<GraficoPuntoModel>> ObtenerTendenciaGrafica(int sucursalId, DateTime inicio, DateTime fin, bool agruparPorMes)
        {
            string filtroSucursal = sucursalId > 0 ? "AND vd.Sucursal = @Sucursal" : "";

            // Si es anual, agrupamos por MONTH, si no, por DAY
            string agrupador = agruparPorMes ? "MONTH(v.FechaEmision)" : "DAY(v.FechaEmision)";
            string seleccion = agruparPorMes ? "MONTH(v.FechaEmision)" : "DAY(v.FechaEmision)";

            // Reutilizamos TU lógica de negativos (Devoluciones/Bonificaciones)
            string query = $@"
    SELECT 
        {seleccion} as IndiceTiempo,
        ISNULL(SUM(
            CASE 
                WHEN v.Mov LIKE '%Devoluci%n%' OR v.Mov LIKE '%Bonifica%' 
                THEN ((vd.Cantidad * vd.Precio) * -1)
                ELSE (vd.Cantidad * vd.Precio)
            END
        ), 0) AS Total
    FROM VentaD vd
    JOIN Venta v ON vd.ID = v.ID
    WHERE 
        v.Estatus = 'CONCLUIDO'
        {filtroSucursal}
        AND v.FechaEmision >= @Inicio 
        AND v.FechaEmision < DATEADD(day, 1, @Fin)
        AND v.Mov NOT LIKE '%Pedido%'
        AND v.Mov NOT LIKE '%Venta Perdida%'
        AND v.Mov NOT LIKE '%Cotiza%'
        AND v.Mov NOT LIKE '%Carta Porte%'
    GROUP BY {agrupador}
    ORDER BY {agrupador}";

            var parametros = new Dictionary<string, object>
    {
        { "@Sucursal", sucursalId },
        { "@Inicio", inicio },
        { "@Fin", fin }
    };

            return await _sqlHelper.QueryAsync(query, parametros, r => new GraficoPuntoModel
            {
                Indice = Convert.ToInt32(r["IndiceTiempo"]),
                Total = Convert.ToDecimal(r["Total"])
            });
        }
    }
    public class GraficoPuntoModel { public int Indice { get; set; } public decimal Total { get; set; } }
}