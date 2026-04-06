using System.ComponentModel.DataAnnotations;

namespace kivoBackend.Application.DTO
{
    /// <summary>
    /// DTO para confirmar recuperação de senha com código de 6 dígitos + nova senha
    /// </summary>
    public class ConfirmarRecuperacaoSenhaDTO
    {
        [Required(ErrorMessage = "Email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email deve ser um endereço válido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Código é obrigatório")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Código deve conter exatamente 6 dígitos")]
        public string Codigo { get; set; }

        [Required(ErrorMessage = "Nova senha é obrigatória")]
        [StringLength(100, MinimumLength = 6,
            ErrorMessage = "Senha deve ter entre 6 e 100 caracteres")]
        public string NovaSenha { get; set; }
    }
}
