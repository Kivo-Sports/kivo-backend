using System.ComponentModel.DataAnnotations;

namespace kivoBackend.Application.DTO
{
    /// <summary>
    /// DTO para confirmar a reativação de conta usando o código recebido por email
    /// </summary>
    public class ConfirmarReativacaoDTO
    {
        [Required(ErrorMessage = "Email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email deve ser um endereço válido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Código é obrigatório")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Código deve conter exatamente 6 dígitos")]
        public string Codigo { get; set; }
    }
}
