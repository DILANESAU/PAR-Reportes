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
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["SQLServerConnection"].ConnectionString;

        public async Task<List<VentaReporteModel>> ObtenerVentasBrutas(string ejercicio, string sucursal, int mes)
        {
           var lista = new List<VentaReporteModel>();

            using (var conexion = new SqlConnection(_connectionString) )
            {
                await conexion.OpenAsync();

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
                        v.Estatus = 'CONCLUIDO'
                        GROUP BY
                        v.FechaEmision,
                        vd.Sucursal,
                        c.Nombre,
                        v.MovID,
                        vd.Articulo,
                        vd.Cantidad,
                        vd.Precio,
                        vd.DescuentoImporte";

                using (var comando = new SqlCommand(query, conexion))
                {
                    comando.Parameters.AddWithValue("@Ejercicio", ejercicio);
                    comando.Parameters.AddWithValue("@Sucursal", sucursal);
                    comando.Parameters.AddWithValue("@Periodo", mes);
                    try
                    {
                        using ( var lector = await comando.ExecuteReaderAsync() )
                        {
                            while ( await lector.ReadAsync() )
                            {
                                lista.Add(new VentaReporteModel
                                {
                                    FechaEmision = Convert.ToDateTime(lector["FechaEmision"]),
                                    Sucursal = lector["Sucursal"].ToString(),
                                    MovID = lector["MovID"].ToString(),
                                    Articulo = lector["Articulo"].ToString().Trim(), //
                                    Cantidad = Convert.ToDouble(lector["Cantidad"]),
                                    PrecioUnitario = Convert.ToDecimal(lector["CostoUnitario"]),
                                    Cliente = lector["Nombre"].ToString(),
                                   Descuento = Convert.ToDecimal(lector["DescuentoImporte"])
                                });
                            }
                        }
                    }
                    catch ( Exception ex )
                    {
                        // Manejo de error básico (podrías guardarlo en un log)
                        System.Diagnostics.Debug.WriteLine("Error SQL: " + ex.Message);
                    }
                }
            }
            return lista;
        }
    }
}
