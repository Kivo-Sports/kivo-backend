using System.Threading.Tasks;

namespace kivoBackend.Application.Interfaces
{
    /// <summary>
    /// Interface para serviço de envio de emails genérico com código de verificação
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Envia email genérico com código de verificação
        /// </summary>
        /// <param name="destinatario">Email do destinatário</param>
        /// <param name="nomeUsuario">Nome do usuário (para personalizar o email)</param>
        /// <param name="codigo">Código de verificação (6 dígitos ou token)</param>
        /// <param name="titulo">Título do email (ex: "Reativar sua conta")</param>
        /// <param name="mensagem">Mensagem do email (ex: "Seu código é: {0}")</param>
        Task EnviarEmailComCodigoAsync(
            string destinatario,
            string nomeUsuario,
            string codigo,
            string titulo,
            string mensagem);

        /// <summary>
        /// Envia email com código de reativação (método mantido para compatibilidade)
        /// </summary>
        Task EnviarCodigoReativacaoAsync(string destinatario, string nomeUsuario, string codigo);
    }
}
