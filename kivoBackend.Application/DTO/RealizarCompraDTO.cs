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
        /// <summary>
        /// Itens do carrinho. Permite ingressos de lotes diferentes na mesma cobrança.
        /// </summary>
        [Required]
        [MinLength(1, ErrorMessage = "Informe pelo menos um lote de ingressos.")]
        public List<ItemCompraIngressoDTO> Itens { get; set; } = new();
    }

    public class ItemCompraIngressoDTO
    {
        [Required]
        public Guid IngressoLoteId { get; set; }

        [Range(1, 10, ErrorMessage = "A quantidade máxima por lote é de 10 ingressos.")]
        public int Quantidade { get; set; } = 1;
    }
}
