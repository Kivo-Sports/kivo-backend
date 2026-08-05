using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Application.DTO
{
    public class RealizarCompraDTO
    {
        [Required]
        public Guid IngressoLoteId { get; set; }

        [Range(1, 10, ErrorMessage = "A quantidade máxima por compra é de 10 ingressos.")]
        public int Quantidade { get; set; } = 1;
    }
}
