using System.Threading.Tasks;

using WPF_PAR.MVVM.Models;
using WPF_PAR.Repositories;

namespace WPF_PAR.Services
{
    public class AuthService
    {
        private readonly IAuthRepository _repository;

        public AuthService(IAuthRepository repository)
        {
            _repository = repository;
        }

        public async Task<UsuarioModel> ValidarLoginAsync(string usuario, string password)
        {
            return await _repository.ValidarLoginAsync(usuario, password);
        }
    }
}