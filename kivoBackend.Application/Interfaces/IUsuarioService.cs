using kivoBackend.Core.Entities;
using kivoBackend.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Application.Interfaces
{
    public interface IUsuarioService
    {
        Task<Usuario> CriarUsuario(Usuario usuario, string senha);
        Task<Usuario> ObterUsuarioPorId(Guid id);
        Task<IEnumerable<Usuario>> ObterTodosUsuarios();
        Task<Usuario> EditarUsuario(Guid id, Usuario usuario);
        Task<Usuario> RemoverUsuario(Guid id);
        Task<Usuario> ObterUsuarioPorCpf(string cpf);
        void InicializarPerfilPorCargo(Usuario usuario);
        Task DesativarConta(Usuario usuario);
        Task AtivarConta(Guid id);
    }
}
