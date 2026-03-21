using kivoBackend.Application.Interfaces;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using kivoBackend.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Application.Services
{
    public class UsuarioService : ServiceGenerics<Usuario>, IUsuarioService
    {
        private readonly IRepositoryGenerics<Usuario> _repositoryGenerics;
        private readonly IRepositoryUsuario _usuarioRepository;
        private readonly UserManager<IdentityUser> _userManager;

        public UsuarioService(IRepositoryGenerics<Usuario> repositoryGenerics, IRepositoryUsuario usuarioRepository, 
            UserManager<IdentityUser> userManager) : base(repositoryGenerics)
        {
            _repositoryGenerics = repositoryGenerics;
            _usuarioRepository = usuarioRepository;
            _userManager = userManager;

        }

        public async Task AtivarConta(Guid id)
        {
            var usuario = await _repositoryGenerics.ObterPorId(id);
            if(usuario == null)
            {
                throw new KeyNotFoundException("Usuário não encontrado.");
            }

            var identityUser = await _userManager.FindByEmailAsync(usuario.Email);
            if(identityUser != null)
            {
                await _userManager.SetLockoutEndDateAsync(identityUser, null);
            }
            usuario.Ativo = true;
            await _repositoryGenerics.Atualizar(usuario);
        }

        public async Task<Usuario> CriarUsuario(Usuario usuario, string senha)
        {
            if(string.IsNullOrEmpty(usuario.Cpf))
            {
                throw new ArgumentException("O CPF é obrigatório.");
            }

            var usuarioExistente = await ObterUsuarioPorCpf(usuario.Cpf);
            if(usuarioExistente != null)
            {
                throw new InvalidOperationException("Já existe um usuário com este CPF.");
            }

            var identityUser = new IdentityUser
            {
                UserName = usuario.Email,
                Email = usuario.Email,
                PhoneNumber = usuario.Telefone
            };
            var result = await _userManager.CreateAsync(identityUser, senha);

            if (!result.Succeeded)
            {
                var erros = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Erro na autenticação: {erros}");
            }

            try
            {
                await _userManager.AddToRoleAsync(identityUser, usuario.EnumCargo.ToString());

                usuario.Id = Guid.NewGuid();
                usuario.CriadoEm = DateTime.Now;
                usuario.Ativo = true;

                InicializarPerfilPorCargo(usuario);

                return await _repositoryGenerics.Adicionar(usuario);
            }
            catch (Exception ex)
            {
                await _userManager.DeleteAsync(identityUser);
                throw new Exception($"Erro ao salvar perfil do usuário: {ex.Message}");
            }
        }

        public async Task DesativarConta(Usuario usuario)
        {
            var usuarioExistente = await _repositoryGenerics.ObterPorId(usuario.Id);
            usuario.Ativo = false;
            await _repositoryGenerics.Atualizar(usuario);
        }

        public async Task<Usuario> EditarUsuario(Guid id, Usuario usuario)
        {
            var existente = await _repositoryGenerics.ObterPorId(id);
            if(existente == null)
            {
                throw new KeyNotFoundException("Usuário não encontrado.");
            }

            var identityUser = await _userManager.FindByEmailAsync(existente.Email);
            if(identityUser != null)
            {
                identityUser.Email = usuario.Email;
                identityUser.PhoneNumber = usuario.Telefone;
                identityUser.UserName = usuario.Email;

                var identityResult = await _userManager.UpdateAsync(identityUser);

                if(!identityResult.Succeeded)
                {
                    var erros = string.Join(", ", identityResult.Errors.Select(e => e.Description));
                    throw new Exception($"Erro na atualização do usuário: {erros}");
                }
            }
                existente.Nome = usuario.Nome;
                existente.Email = usuario.Email;
                existente.Telefone = usuario.Telefone;
                existente.DataNascimento = usuario.DataNascimento;
                existente.Ativo = usuario.Ativo;

                 await _repositoryGenerics.Atualizar(existente);
                return existente;
        }

        public void InicializarPerfilPorCargo(Usuario usuario)
        {
            switch (usuario.EnumCargo)
            {
                case EnumCargo.Torcedor:
                    usuario.Torcedor ??= new Torcedor { Id = Guid.NewGuid(), UsuarioId = usuario.Id };
                    break;

                case EnumCargo.OrganizadorTime:
                    usuario.OrganizadorTime ??= new OrganizadorTime { Id = Guid.NewGuid(), UsuarioId = usuario.Id };
                    usuario.OrganizadorTime.UsuarioId = usuario.Id;
                    break;

                case EnumCargo.OrganizadorCampeonato:
                    usuario.OrganizadorCampeonato ??= new OrganizadorCampeonato { Id = Guid.NewGuid(), UsuarioId = usuario.Id };
                    usuario.OrganizadorCampeonato.UsuarioId = usuario.Id;
                    break;

                case EnumCargo.Administrador:
                    usuario.Administrador ??= new Administrador { Id = Guid.NewGuid(), UsuarioId = usuario.Id };
                    usuario.Administrador.UsuarioId = usuario.Id;
                    break;

                default: throw new ArgumentException("Cargo inválido.");
            }
        }

        public async Task<IEnumerable<Usuario>> ObterTodosUsuarios()
        {
            var usuarios = await _repositoryGenerics.ObterTodos();
            if(usuarios == null || !usuarios.Any())
            {
                throw new KeyNotFoundException("Nenhum usuário encontrado.");
            }
            return usuarios;
        }

        public Task<Usuario> ObterUsuarioPorCpf(string cpf)
        {
            return _usuarioRepository.ObterUsuarioPorCpf(cpf);
        }

        public async Task<Usuario> ObterUsuarioPorId(Guid id)
        {
            var usuario = await _repositoryGenerics.ObterPorId(id);

            if(usuario == null)
            {
                throw new KeyNotFoundException("Usuário não encontrado.");
            }

            return usuario;
        }

        public async Task<Usuario> RemoverUsuario(Guid id)
        {
           var usuario = await _repositoryGenerics.ObterPorId(id);
            if(usuario == null)
            {
                throw new KeyNotFoundException("Usuário não encontrado.");
            }
            var identityUser = await _userManager.FindByEmailAsync(usuario.Email);
            if (identityUser != null)
            {
                await _userManager.SetLockoutEnabledAsync(identityUser, true);
                await _userManager.SetLockoutEndDateAsync(identityUser, DateTimeOffset.MaxValue);
            }

            await DesativarConta(usuario);
            return usuario;
        }
    }
}
