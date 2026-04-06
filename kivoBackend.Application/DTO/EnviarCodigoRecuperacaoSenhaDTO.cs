using System.ComponentModel.DataAnnotations;

namespace kivoBackend.Application.DTO
{
    /// <summary>
    /// DTO para solicitar envio de código de recuperação de senha por email
    /// </summary>
    public class EnviarCodigoRecuperacaoSenhaDTO
    {
        [Required(ErrorMessage = "Email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email deve ser um endereço válido")]
        public string Email { get; set; }
    }
}
