using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Application.DTO
{
    public class ContaBancoDTO
    {
        [Required(ErrorMessage = "O banco é obrigatório.")]
        public string Banco { get; set; }

        [Required(ErrorMessage = "A agência é obrigatória.")]
        public string Agencia { get; set; }

        [Required(ErrorMessage = "A conta é obrigatória.")]
        public string Conta { get; set; }

        public string? Tipo { get; set; }

        public string ChavePix { get; set; }
    }
}
