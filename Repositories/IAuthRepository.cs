using System;
using System.Collections.Generic;
using System.Text;

using WPF_PAR.MVVM.Models;

namespace WPF_PAR.Repositories
{
    public interface IAuthRepository
    {
        Task<UsuarioModel> ValidarLoginAsync(string usuario, string password);
    }
}
