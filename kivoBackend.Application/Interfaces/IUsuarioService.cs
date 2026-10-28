using kivoBackend.Core.Entities;
using kivoBackend.Application.DTO;
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
        Task<GestaoUsuarioPagina> ListarGestao(GestaoUsuarioFiltro filtro);
        Task AlterarEstadoAdministrativo(Guid id, Guid autorId, string acao);
        Task AlterarCargoAdministrativo(Guid id, Guid autorId, AlterarCargoUsuarioDTO dto);
        Task<bool> ResponsavelValido(Guid perfilId, kivoBackend.Core.Enums.EnumCargo cargo, bool permitirAdmin = false);
        Task<Usuario> CriarUsuario(Usuario usuario, string senha);
        Task<Usuario> ObterUsuarioPorId(Guid id);
        Task<IEnumerable<Usuario>> ObterTodosUsuarios();
        Task<IEnumerable<Usuario>> ObterAdministradores();
        Task<Usuario> EditarDadosUsuario(Guid id, Usuario usuario);
        Task<Usuario> RemoverUsuario(Guid id);
        Task<Usuario?> ObterUsuarioPorCpf(string cpf);
        Task<Usuario?> ObterUsuarioPorEmail(string email);
        void InicializarPerfilPorCargo(Usuario usuario);
        Task DesativarConta(Usuario usuario);
        Task AtivarConta(Guid id);
        Task GerarCodigoReativacao(string email);
        Task ConfirmarReativacao(string email, string codigo);
        Task GerarCodigoRecuperacaoSenha(string email);
        Task ConfirmarRecuperacaoSenha(string email, string codigo, string novaSenha);
        Task RedefinirSenha(string email, string senhaAtual, string novaSenha);
        Task<bool> VerificarEmailExiste(string email);
        Task<bool> VerificarCpfExiste(string cpf);
    }
}
