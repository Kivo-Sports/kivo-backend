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
        private readonly IEmailService _emailService;
        private readonly IRepositoryGenerics<CodigoReativacao> _codigoRepository;
        private readonly IVerificationCodeService _verificationCodeService;

        public UsuarioService(IRepositoryGenerics<Usuario> repositoryGenerics, IRepositoryUsuario usuarioRepository,
            UserManager<IdentityUser> userManager, IEmailService emailService, IRepositoryGenerics<CodigoReativacao> codigoRepository,
            IVerificationCodeService verificationCodeService)
            : base(repositoryGenerics)
        {
            _repositoryGenerics = repositoryGenerics;
            _usuarioRepository = usuarioRepository;
            _userManager = userManager;
            _emailService = emailService;
            _codigoRepository = codigoRepository;
            _verificationCodeService = verificationCodeService;
        }

        public async Task AtivarConta(Guid id)
        {
            var usuario = await _repositoryGenerics.ObterPorId(id);
            if(usuario == null)
            {
                throw new KeyNotFoundException("Usuário não encontrado.");
            }

            if (usuario.Ativo)
            {
                throw new InvalidOperationException("Esta conta já está ativa.");
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

            if (string.IsNullOrWhiteSpace(usuario.Email))
            {
                throw new ArgumentException("O e-mail é obrigatório.");
            }

            usuario.Email = usuario.Email.Trim();

            var usuarioExistente = await ObterUsuarioPorCpf(usuario.Cpf);
            if(usuarioExistente != null)
            {
                throw new InvalidOperationException("Já existe um usuário com este CPF.");
            }

            var identityComMesmoEmail = await _userManager.FindByEmailAsync(usuario.Email);
            var identityComMesmoUserName = await _userManager.FindByNameAsync(usuario.Email);
            if (identityComMesmoEmail != null || identityComMesmoUserName != null)
            {
                throw new InvalidOperationException("Já existe um usuário com este e-mail.");
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
            if (!usuario.Ativo)
            {
                throw new InvalidOperationException("Esta conta já está Desativada.");
            }
            usuario.Ativo = false;
            await _repositoryGenerics.Atualizar(usuario);
        }

        public async Task<Usuario> EditarDadosUsuario(Guid id, Usuario dadosEditados)
        {
            var existente = await _usuarioRepository.ObterUsuarioPorId(id);
            if (existente == null) throw new KeyNotFoundException("Usuário não encontrado.");

            if (!existente.Ativo)
            {
                throw new InvalidOperationException("Não é permitido atualizar uma conta que não está ativa. " +
                                                    "Reative-a e tente novamente");
            }

            var identityUser = await _userManager.FindByEmailAsync(existente.Email);
            if (identityUser != null && existente.Email != dadosEditados.Email)
            {
                identityUser.Email = dadosEditados.Email;
                identityUser.UserName = dadosEditados.Email;
                await _userManager.UpdateAsync(identityUser);
            }

            existente.Nome = dadosEditados.Nome;
            existente.Email = dadosEditados.Email;
            existente.Telefone = dadosEditados.Telefone;
            existente.DataNascimento = dadosEditados.DataNascimento;

            var enderecoNoBanco = existente.EnumCargo switch
            {
                EnumCargo.Torcedor => existente.Torcedor?.Endereco,
                EnumCargo.OrganizadorTime => existente.OrganizadorTime?.Endereco,
                EnumCargo.OrganizadorCampeonato => existente.OrganizadorCampeonato?.Endereco,
                _ => null
            };

            var enderecoEnviadoPelaAPI = dadosEditados.Torcedor?.Endereco
                               ?? dadosEditados.OrganizadorTime?.Endereco
                               ?? dadosEditados.OrganizadorCampeonato?.Endereco;

            if (enderecoNoBanco != null && enderecoEnviadoPelaAPI != null)
            {
                enderecoNoBanco.Cep = enderecoEnviadoPelaAPI.Cep;
                enderecoNoBanco.Rua = enderecoEnviadoPelaAPI.Rua;
                enderecoNoBanco.Numero = enderecoEnviadoPelaAPI.Numero;
                enderecoNoBanco.Cidade = enderecoEnviadoPelaAPI.Cidade;
                enderecoNoBanco.Estado = enderecoEnviadoPelaAPI.Estado;
                enderecoNoBanco.Complemento = enderecoEnviadoPelaAPI.Complemento ?? "";
                enderecoNoBanco.Pais = enderecoEnviadoPelaAPI.Pais ?? "Brasil";
            }

            if (existente.EnumCargo == EnumCargo.OrganizadorCampeonato && dadosEditados.OrganizadorCampeonato?.ContaBanco != null)
            {
                var cbExistente = existente.OrganizadorCampeonato.ContaBanco;
                var cbNova = dadosEditados.OrganizadorCampeonato.ContaBanco;

                cbExistente.Banco = cbNova.Banco;
                cbExistente.Agencia = cbNova.Agencia;
                cbExistente.Conta = cbNova.Conta;
                cbExistente.ChavePix = cbNova.ChavePix;
            }

            await _repositoryGenerics.Atualizar(existente);
            return existente;
        }

        public void InicializarPerfilPorCargo(Usuario usuario)
        {
            switch (usuario.EnumCargo)
            {
                case EnumCargo.Torcedor:
                    if (usuario.Torcedor == null)
                        usuario.Torcedor = new Torcedor { Id = Guid.NewGuid(), UsuarioId = usuario.Id };
                    else
                        usuario.Torcedor.UsuarioId = usuario.Id;
                    break;

                case EnumCargo.OrganizadorTime:
                    if (usuario.OrganizadorTime == null)
                        usuario.OrganizadorTime = new OrganizadorTime { Id = Guid.NewGuid(), UsuarioId = usuario.Id };
                    else
                        usuario.OrganizadorTime.UsuarioId = usuario.Id;
                    break;

                case EnumCargo.OrganizadorCampeonato:
                    if (usuario.OrganizadorCampeonato == null)
                        usuario.OrganizadorCampeonato = new OrganizadorCampeonato { Id = Guid.NewGuid(), UsuarioId = usuario.Id };
                    else
                        usuario.OrganizadorCampeonato.UsuarioId = usuario.Id;
                    break;

                case EnumCargo.Administrador:
                    // Admin users don't need a separate profile entity
                    break;

                default: throw new ArgumentException("Cargo inválido.");
            }
        }

        public async Task<IEnumerable<Usuario>> ObterTodosUsuarios()
        {
            var usuarios = await _usuarioRepository.ObterTodosUsuarios();
            if(usuarios == null || !usuarios.Any())
            {
                throw new KeyNotFoundException("Nenhum usuário encontrado.");
            }
            return usuarios;
        }

        public async Task<IEnumerable<Usuario>> ObterAdministradores()
        {
            var admins = await _usuarioRepository.ObterAdministradores();
            if (admins == null || !admins.Any())
            {
                throw new KeyNotFoundException("Nenhum administrador encontrado.");
            }
            return admins;
        }

        public async Task<Usuario?> ObterUsuarioPorCpf(string cpf)
        {
            return await _usuarioRepository.ObterUsuarioPorCpf(cpf);
        }

        public async Task<Usuario> ObterUsuarioPorId(Guid id)
        {
            var usuario = await _usuarioRepository.ObterUsuarioPorId(id);

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

        public async Task<Usuario?> ObterUsuarioPorEmail(string email)
        {
            return await _usuarioRepository.ObterUsuarioPorEmail(email);
        }

        /// <summary>
        /// Gera um código de 6 dígitos para reativação de conta e envia por email
        /// </summary>
        public async Task GerarCodigoReativacao(string email)
        {
            // Obter usuário pelo email
            var usuario = await _usuarioRepository.ObterUsuarioPorEmail(email);
            if (usuario == null)
            {
                throw new KeyNotFoundException("Usuário não encontrado.");
            }

            // Validar que não é admin
            if (usuario.EnumCargo == EnumCargo.Administrador)
            {
                throw new InvalidOperationException("Contas de administrador não podem ser reativadas via email. Use o painel de controle de admins.");
            }

            // Verificar se conta está ativa (se estiver, não precisa reativar)
            if (usuario.Ativo)
            {
                throw new InvalidOperationException("Esta conta já está ativa.");
            }

            // Gerar código usando o serviço genérico
            var codigo = await _verificationCodeService.GerarCodigoAsync(
                usuario.Id,
                VerificationCodeType.AccountReactivation,
                duracao: 5
            );

            // Enviar email com código
            await _emailService.EnviarEmailComCodigoAsync(
                usuario.Email,
                usuario.Nome,
                codigo,
                "Código de Reativação da Sua Conta",
                "Recebemos uma solicitação para reativar sua conta. Use o código abaixo para confirmar sua identidade:"
            );
        }

        /// <summary>
        /// Confirma a reativação usando o código recebido por email
        /// </summary>
        public async Task ConfirmarReativacao(string email, string codigo)
        {
            // Obter usuário pelo email
            var usuario = await _usuarioRepository.ObterUsuarioPorEmail(email);
            if (usuario == null)
            {
                throw new KeyNotFoundException("Usuário não encontrado.");
            }

            // Verificar se conta já está ativa
            if (usuario.Ativo)
            {
                throw new InvalidOperationException("Esta conta já está ativa.");
            }

            // Validar código usando o serviço genérico
            bool valido = await _verificationCodeService.ValidarCodigoAsync(
                usuario.Id,
                codigo,
                VerificationCodeType.AccountReactivation
            );

            if (!valido)
            {
                throw new InvalidOperationException("Código inválido ou expirado.");
            }

            // Marcar código como usado
            await _verificationCodeService.MarcarComoUsadoAsync(
                usuario.Id,
                codigo,
                VerificationCodeType.AccountReactivation
            );

            // Ativar conta
            await AtivarConta(usuario.Id);
        }

        /// <summary>
        /// Gera um código de 6 dígitos para recuperação de senha e envia por email
        /// </summary>
        public async Task GerarCodigoRecuperacaoSenha(string email)
        {
            // Obter usuário pelo email
            var usuario = await _usuarioRepository.ObterUsuarioPorEmail(email);
            if (usuario == null)
            {
                throw new KeyNotFoundException("Usuário não encontrado.");
            }

            // Gerar código usando o serviço genérico
            var codigo = await _verificationCodeService.GerarCodigoAsync(
                usuario.Id,
                VerificationCodeType.PasswordReset,
                duracao: 5
            );

            // Enviar email com código
            await _emailService.EnviarEmailComCodigoAsync(
                usuario.Email,
                usuario.Nome,
                codigo,
                "Código de Recuperação de Senha",
                "Recebemos uma solicitação para recuperar sua senha. Use o código abaixo para confirmar sua identidade:"
            );
        }

        /// <summary>
        /// Confirma a recuperação de senha usando o código recebido por email e atualiza a senha
        /// </summary>
        public async Task ConfirmarRecuperacaoSenha(string email, string codigo, string novaSenha)
        {
            // Obter usuário pelo email
            var usuario = await _usuarioRepository.ObterUsuarioPorEmail(email);
            if (usuario == null)
            {
                throw new KeyNotFoundException("Usuário não encontrado.");
            }

            // Validar código usando o serviço genérico
            bool valido = await _verificationCodeService.ValidarCodigoAsync(
                usuario.Id,
                codigo,
                VerificationCodeType.PasswordReset
            );

            if (!valido)
            {
                throw new InvalidOperationException("Código inválido ou expirado.");
            }

            // Marcar código como usado
            await _verificationCodeService.MarcarComoUsadoAsync(
                usuario.Id,
                codigo,
                VerificationCodeType.PasswordReset
            );

            // Atualizar senha no Identity
            var identityUser = await _userManager.FindByEmailAsync(email);
            if (identityUser != null)
            {
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(identityUser);
                var result = await _userManager.ResetPasswordAsync(identityUser, resetToken, novaSenha);

                if (!result.Succeeded)
                {
                    var erros = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Erro ao atualizar senha: {erros}");
                }
            }

            // Se conta estava desativada, ativar
            if (!usuario.Ativo)
            {
                await AtivarConta(usuario.Id);
            }
        }

        /// <summary>
        /// Redefinir senha de usuário autenticado (requer senha atual)
        /// </summary>
        public async Task RedefinirSenha(string email, string senhaAtual, string novaSenha)
        {
            // Obter usuário pelo email
            var usuario = await _usuarioRepository.ObterUsuarioPorEmail(email);
            if (usuario == null)
            {
                throw new KeyNotFoundException("Usuário não encontrado.");
            }

            // Obter IdentityUser
            var identityUser = await _userManager.FindByEmailAsync(email);
            if (identityUser == null)
            {
                throw new KeyNotFoundException("Usuário não encontrado no sistema de autenticação.");
            }

            // Validar senha atual
            var senhaValida = await _userManager.CheckPasswordAsync(identityUser, senhaAtual);
            if (!senhaValida)
            {
                throw new InvalidOperationException("Senha atual incorreta.");
            }

            // Atualizar para nova senha
            var result = await _userManager.ChangePasswordAsync(identityUser, senhaAtual, novaSenha);

            if (!result.Succeeded)
            {
                var erros = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Erro ao atualizar senha: {erros}");
            }
        }

        /// <summary>
        /// Verificar se email já existe no banco de dados
        /// </summary>
        public async Task<bool> VerificarEmailExiste(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            var emailNormalizado = email.ToLower().Trim();
            var usuario = await _usuarioRepository.ObterUsuarioPorEmail(emailNormalizado);
            if (usuario != null)
                return true;

            var identityUser = await _userManager.FindByEmailAsync(emailNormalizado);
            if (identityUser != null)
                return true;

            identityUser = await _userManager.FindByNameAsync(emailNormalizado);
            return identityUser != null;
        }

        /// <summary>
        /// Verificar se CPF já existe no banco de dados
        /// </summary>
        public async Task<bool> VerificarCpfExiste(string cpf)
        {
            if (string.IsNullOrEmpty(cpf))
                return false;

            var usuario = await _usuarioRepository.ObterUsuarioPorCpf(cpf);
            return usuario != null;
        }
    }
}
