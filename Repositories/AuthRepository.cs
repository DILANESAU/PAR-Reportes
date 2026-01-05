using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using WPF_PAR.MVVM.Models;
using WPF_PAR.Services;

namespace WPF_PAR.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly SqlHelper _sqlHelper;

        // Aquí inyectamos el SqlHelper "normal", pero en App.xaml.cs haremos el truco 
        // para pasarle el que tiene la conexión correcta.
        public AuthRepository(SqlHelper sqlHelper)
        {
            _sqlHelper = sqlHelper;
        }

        public async Task<UsuarioModel> ValidarLoginAsync(string usuarioInput, string passwordInput)
        {
            string query = @"
                SELECT IdUsuario, [user], NombreCompleto, Correo, Rol
                FROM Usuarios 
                WHERE [user] = @User AND Clave = @Pass";

            var parametros = new Dictionary<string, object>
            {
                { "@User", usuarioInput },
                { "@Pass", passwordInput }
            };

            var usuarios = await _sqlHelper.QueryAsync(query, parametros, lector => new UsuarioModel
            {
                IdUsuario = Convert.ToInt32(lector["IdUsuario"]),
                Username = lector["user"].ToString(),
                NombreCompleto = lector["NombreCompleto"].ToString(),
                Rol = lector["Rol"].ToString(),
                Password = ""
            });

            var usuario = usuarios.FirstOrDefault();

            if ( usuario != null )
            {
                // Lógica de permisos (Subconsulta)
                if ( usuario.Rol.Equals("Admin", StringComparison.OrdinalIgnoreCase) )
                {
                    usuario.SucursalesPermitidas = null;
                }
                else
                {
                    string queryPermisos = "SELECT IdSucursal FROM UsuarioSucursales WHERE IdUsuario = @Id";
                    var paramsPermisos = new Dictionary<string, object> { { "@Id", usuario.IdUsuario } };

                    usuario.SucursalesPermitidas = await _sqlHelper.QueryAsync(queryPermisos, paramsPermisos, l => Convert.ToInt32(l["IdSucursal"]));
                }
            }
            return usuario;
        }
    }
}