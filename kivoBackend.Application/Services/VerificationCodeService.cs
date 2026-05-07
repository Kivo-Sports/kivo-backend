using kivoBackend.Application.Interfaces;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using kivoBackend.Core.Interfaces;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Application.Services
{
    public class VerificationCodeService : IVerificationCodeService
    {
        private readonly IVerificationCodeRepository _repository;

        public VerificationCodeService(IVerificationCodeRepository repository)
        {
            _repository = repository;
        }
        private string GerarHash(string codigo)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(codigo);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        public async Task<string> GerarCodigoAsync(Guid usuarioId, VerificationCodeType tipo, int duracao = 5)
        {
            var codigoExistente = await _repository.ObterCodigoValidoAsync(usuarioId, tipo);
            if (codigoExistente != null)
            {
                codigoExistente.Usado = true;
                await _repository.Atualizar(codigoExistente);
            }

            var random = new Random();
            var codigo = random.Next(100000, 999999).ToString();

            var novoCodigoVerificacao = new VerificationCode
            {
                UsuarioId = usuarioId,
                Codigo = GerarHash(codigo),
                Tipo = tipo,
                ExpiraEm = DateTime.Now.AddMinutes(duracao),
                MaximoTentativas = 5 
            };

            await _repository.Adicionar(novoCodigoVerificacao);
            return codigo;
        }

        public async Task<bool> ValidarCodigoAsync(Guid usuarioId, string codigo, VerificationCodeType tipo)
        {
            var codigoValido = await _repository.ObterCodigoValidoAsync(usuarioId, tipo);

            if (codigoValido == null)
                throw new InvalidOperationException("Nenhum código válido encontrado. Solicite um novo.");

            if (codigoValido.Usado)
                throw new InvalidOperationException("Este código já foi utilizado.");

            if (DateTime.Now > codigoValido.ExpiraEm) { 
                await _repository.Remover(codigoValido.Id);
                throw new InvalidOperationException("Código expirado. Solicite um novo.");
            }

            if (codigoValido.Tentativas >= codigoValido.MaximoTentativas)
            {
                await _repository.Remover(codigoValido.Id);

                throw new InvalidOperationException("Número máximo de tentativas excedido. Solicite um novo código.");
            }

            var codigoHash = GerarHash(codigo);
            codigoValido.Tentativas++;

            if (codigoValido.Codigo != codigoHash)
            {
                if (codigoValido.Tentativas >= codigoValido.MaximoTentativas)
                {
                    await _repository.Remover(codigoValido.Id);

                    throw new InvalidOperationException("Número máximo de tentativas excedido. Solicite um novo código.");
                }

                await _repository.Atualizar(codigoValido);

                var restantes = codigoValido.MaximoTentativas - codigoValido.Tentativas;

                throw new InvalidOperationException($"Código inválido. Tentativas restantes: {restantes}");
            }
            codigoValido.Usado = true;
            await _repository.Remover(codigoValido.Id);

            return true;
        }

        public async Task MarcarComoUsadoAsync(Guid usuarioId, string codigo, VerificationCodeType tipo)
        {
            var codigoValido = await _repository.ObterCodigoValidoAsync(usuarioId, tipo);

            if (codigoValido != null)
            {
                codigoValido.Usado = true;
                await _repository.Atualizar(codigoValido);
            }
        }

        public string ObterDescricaoTipo(VerificationCodeType tipo)
        {
            return tipo switch
            {
                VerificationCodeType.AccountReactivation => "Reativar Conta",
                VerificationCodeType.PasswordReset => "Recuperar Senha",
                VerificationCodeType.PasswordRedefinition => "Redefinir Senha",
                VerificationCodeType.TwoFactorAuth => "Autenticação 2FA",
                VerificationCodeType.EmailConfirmation => "Confirmação de Email",
                _ => "Verificação"
            };
        }
    }
}
