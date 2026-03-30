using System.ComponentModel.DataAnnotations;

namespace kivoBackend.Application.DTO
{
    /// <summary>
    /// DTO para solicitar envio de código de reativação por email
    /// </summary>
    public class EnviarCodigoReativacaoDTO
    {
        [Required(ErrorMessage = "Email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email deve ser um endereço válido")]
        public string Email { get; set; }
    }
}
