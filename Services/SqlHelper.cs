using Microsoft.Data.SqlClient;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WPF_PAR.Services
{
    public class SqlHelper
    {
        private readonly string _connectionString;
        public enum TipoConexion { Auth, Data }
        public SqlHelper(TipoConexion tipo, string connectionStringOverride = null)
        {
            if ( !string.IsNullOrEmpty(connectionStringOverride) )
            {
                _connectionString = connectionStringOverride;
            }
            else
            {
                string server = "", db = "", user = "", pass = "";
                var secure = new SecureStorageService();

                // 3. Switch para decidir qué credenciales cargar
                switch ( tipo )
                {
                    case TipoConexion.Auth:
                        server = Properties.Settings.Default.Auth_Server;
                        db = Properties.Settings.Default.Auth_Db;
                        user = Properties.Settings.Default.Auth_User;
                        pass = secure.RecuperarPassword(SecureStorageService.KeyAuth);
                        break;

                    case TipoConexion.Data:
                        server = Properties.Settings.Default.Data_Server;
                        db = Properties.Settings.Default.Data_Db;
                        user = Properties.Settings.Default.Data_User;
                        pass = secure.RecuperarPassword(SecureStorageService.KeyData);
                        break;
                }

                // Validación simple
                if ( string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(db) )
                    _connectionString = "";
                else
                    _connectionString = $"Data Source={server};Initial Catalog={db};User ID={user};Password={pass};TrustServerCertificate=True;Timeout=30";
            }
        }

        // ====================================================================
        // MÉTODO NUEVO: Para el botón "Probar Conexión"
        // ====================================================================
        public async Task<bool> ProbarConexionAsync()
        {
            try
            {
                using ( var conexion = new SqlConnection(_connectionString) )
                {
                    await conexion.OpenAsync();
                    return true; // ¡Éxito!
                }
            }
            catch ( Exception ex )
            {
                // Aquí podrías loguear el error si quisieras
                System.Diagnostics.Debug.WriteLine("Error conexión: " + ex.Message);
                return false;
            }
        }

        // ====================================================================
        // TUS MÉTODOS EXISTENTES (Sin cambios lógicos, solo siguen usando _connectionString)
        // ====================================================================
        public async Task<List<T>> QueryAsync<T>(string query, Dictionary<string, object> parameters, Func<SqlDataReader, T> mapFunction)
        {
            var lista = new List<T>();

            using ( var conexion = new SqlConnection(_connectionString) )
            {
                await conexion.OpenAsync();

                using ( var comando = new SqlCommand(query, conexion) )
                {
                    if ( parameters != null )
                    {
                        foreach ( var param in parameters )
                        {
                            comando.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                    }

                    try
                    {
                        using ( var lector = await comando.ExecuteReaderAsync() )
                        {
                            while ( await lector.ReadAsync() )
                            {
                                lista.Add(mapFunction(lector));
                            }
                        }
                    }
                    catch ( Exception ex )
                    {
                        throw new Exception($"Error al ejecutar SQL: {ex.Message}", ex);
                    }
                }
            }
            return lista;
        }
    }
}