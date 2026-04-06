using System.ComponentModel.DataAnnotations;

namespace kivoBackend.Application.DTO
{
    /// <summary>
    /// DTO para redefinir senha de um usuário autenticado
    /// </summary>
    public class RedefinirSenhaDTO
    {
        [Required(ErrorMessage = "Senha atual é obrigatória")]
        public string SenhaAtual { get; set; }

        [Required(ErrorMessage = "Nova senha é obrigatória")]
        [StringLength(100, MinimumLength = 6,
            ErrorMessage = "Senha deve ter entre 6 e 100 caracteres")]
        public string NovaSenha { get; set; }
    }
}
