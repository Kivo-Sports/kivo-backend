using System.ComponentModel.DataAnnotations;

namespace kivoBackend.Application.DTO
{
    public class AtribuirTitularIngressoDTO
    {
        [Required]
        public string Nome { get; set; } = string.Empty;

        [Required]
        [StringLength(14, MinimumLength = 11, ErrorMessage = "Informe um CPF válido.")]
        public string Cpf { get; set; } = string.Empty;
    }
}
