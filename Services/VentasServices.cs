using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks; // <--- OBLIGATORIO

using WPF_PAR.Core;
using WPF_PAR.MVVM.Models;

namespace WPF_PAR.Services
{
    public class VentasServices
    {
        // Usamos el ConnectionHelper que creamos antes
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["SQLServerConnection"].ConnectionString;

        // CAMBIO 1: Task<List<>> y nombre termina en Async
        public async Task<List<VentasModel>> ObtenerVentasAsync(int sucursalId, int anio, int mes)
        {
            var lista = new List<VentasModel>();

            using ( var conexion = new SqlConnection(_connectionString) )
            {
                // CAMBIO 2: Apertura asíncrona
                await conexion.OpenAsync();

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

                using ( var comando = new SqlCommand(query, conexion) )
                {
                    comando.Parameters.AddWithValue("@Sucursal", sucursalId);
                    comando.Parameters.AddWithValue("@Ejercicio", anio);
                    comando.Parameters.AddWithValue("@Periodo", mes);

                    // CAMBIO 3: Ejecución asíncrona
                    using ( var lector = await comando.ExecuteReaderAsync() )
                    {
                        // CAMBIO 4: Lectura asíncrona fila por fila
                        while ( await lector.ReadAsync() )
                        {
                            lista.Add(new VentasModel
                            {
                                Fecha = Convert.ToDateTime(lector["FechaEmision"]),
                                MovID = lector["MovID"].ToString(),
                                Mov = lector["Mov"].ToString(),
                                Cliente = lector["Cliente"].ToString(),
                                PrecioTotal = Convert.ToDecimal(lector["PrecioTotal"])
                            });
                        }
                    }
                }
            }
            return lista;
        }
    }
}