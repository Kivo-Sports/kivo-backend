using kivoBackend.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Core.Interfaces
{
    public interface IRepositoryUsuario
    {
        Task<Usuario?> ObterUsuarioPorCpf(string cpf);
        Task<Usuario?> ObterUsuarioPorEmail(string email);
        Task<Usuario?> ObterUsuarioPorId(Guid id);
        Task<IEnumerable<Usuario>> ObterTodosUsuarios();
        Task<IEnumerable<Usuario>> ObterAdministradores();
    }
}
