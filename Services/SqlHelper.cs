using Microsoft.Data.SqlClient;

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Windows.Navigation;

namespace WPF_PAR.Services
{
    public class SqlHelper
    {
        private readonly string _connectionString;

        public SqlHelper()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SQLServerConnection"].ConnectionString;
        }
        public async Task<List<T>> QueryAsync<T>(string query, Dictionary<string, object> parameters, Func<SqlDataReader, T> mapFunction)
        {
            var lista = new List<T>();

            using ( var conexion = new SqlConnection(_connectionString) )
            {
                await conexion.OpenAsync();

                using ( var comando = new SqlCommand(query, conexion) )
                {
                    // Agregar parámetros si existen
                    if ( parameters != null )
                    {
                        foreach ( var param in parameters )
                        {
                            // Manejo seguro de nulos (DBNull)
                            comando.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                    }

                    try
                    {
                        using ( var lector = await comando.ExecuteReaderAsync() )
                        {
                            while ( await lector.ReadAsync() )
                            {
                                // Aquí ocurre la magia: convertimos la fila en objeto
                                lista.Add(mapFunction(lector));
                            }
                        }
                    }
                    catch ( Exception ex )
                    {
                        // Lanzamos la excepción para que el ViewModel (y tu DialogService) la muestre
                        throw new Exception($"Error al ejecutar SQL: {ex.Message}", ex);
                    }
                }
            }
            return lista;
        }
    }
}
