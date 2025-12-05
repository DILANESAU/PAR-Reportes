using System;
using System.Collections.Generic;
using System.Text;

namespace WPF_PAR.MVVM.Models
{
    public class UsuarioModel
    {
        public string Username { get; set; }
        public string NombreCompleto { get; set; }
        public string Password { get; set; }

        public string Rol { get; set; }
        public List<int> SucursalesPermitidas { get; set; }
    }
}
