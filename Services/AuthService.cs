using System;
using System.Collections.Generic;
using System.Text;

using WPF_PAR.MVVM.Models;

namespace WPF_PAR.Services
{
    public class AuthService
    {
        // Simulamos usuarios (Mock Data)
        private List<UsuarioModel> _usuarios = new List<UsuarioModel>
        {
            // 1. ADMIN: Ve todo (SucursalesPermitidas = null)
            new UsuarioModel { Username="admin", Password="123", NombreCompleto="Administrador", Rol="Admin", SucursalesPermitidas = null },
            
            // 2. GERENTE OCOSINGO: Solo ve la 1508
            new UsuarioModel { Username="gerente", Password="123", NombreCompleto="Gerente Ocosingo", Rol="Gerente", SucursalesPermitidas = new List<int>{ 1508 } },
            
            // 3. SUPERVISOR: Ve dos sucursales
            new UsuarioModel { Username="super", Password="123", NombreCompleto="Supervisor Zona", Rol="Supervisor", SucursalesPermitidas = new List<int>{ 1508, 1202 } }
        };

        public UsuarioModel ValidarLogin(string user, string pass)
        {
            return _usuarios.FirstOrDefault(u => u.Username == user && u.Password == pass);
        }
    }
}
