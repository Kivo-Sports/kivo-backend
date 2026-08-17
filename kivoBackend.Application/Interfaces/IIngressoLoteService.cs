using kivoBackend.Application.DTO;
using kivoBackend.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Application.Interfaces
{
    public interface IIngressoLoteService
    {
        public Task<IngressoLote> CriarLote(CriarIngressoLoteDTO dto, Guid? organizadorCampeonatoId, bool ehAdmin);
        Task<IEnumerable<IngressoLote>> ObterLotesPorPartida(Guid partidaId);
    }
}
