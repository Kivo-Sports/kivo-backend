using kivoBackend.Application.Interfaces;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using kivoBackend.Core.Interfaces;
using System;
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

        public async Task<string> GerarCodigoAsync(Guid usuarioId, VerificationCodeType tipo, int duracao = 5)
        {
            // 1. Buscar código válido existente (reutilizar se já enviado)
            var codigoExistente = await _repository.ObterCodigoValidoAsync(usuarioId, tipo);
            if (codigoExistente != null)
                return codigoExistente.Codigo;

            // 2. Gerar novo código (6 dígitos)
            var random = new Random();
            var codigo = random.Next(100000, 999999).ToString();

            // 3. Salvar no banco
            var novoCodigoVerificacao = new VerificationCode
            {
                UsuarioId = usuarioId,
                Codigo = codigo,
                Tipo = tipo,
                ExpiraEm = DateTime.Now.AddMinutes(duracao)
            };

            await _repository.Adicionar(novoCodigoVerificacao);
            return codigo;
        }

        public async Task<bool> ValidarCodigoAsync(Guid usuarioId, string codigo, VerificationCodeType tipo)
        {
            var codigoValido = await _repository.ObterCodigoAsync(usuarioId, codigo, tipo);

            if (codigoValido == null)
                return false;

            if (codigoValido.Usado)
                return false;

            if (DateTime.Now > codigoValido.ExpiraEm)
                return false;

            return true;
        }

        public async Task MarcarComoUsadoAsync(Guid usuarioId, string codigo, VerificationCodeType tipo)
        {
            var codigoValido = await _repository.ObterCodigoAsync(usuarioId, codigo, tipo);

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
