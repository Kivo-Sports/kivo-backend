using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Application.DTO
{
    public class ListarConviteDTO
    {
        public Guid ParticipacaoId { get; set; }
        public Guid CampeonatoId { get; set; }
        public string NomeCampeonato { get; set; }
        public string NomeTime { get; set; }
        public DateTime ConvidadoEm { get; set; }
    }
}
