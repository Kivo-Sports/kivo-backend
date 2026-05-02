using kivoBackend.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Application.Interfaces
{
    public interface ICampeonatoService : IServiceGenerics<Campeonato>
    {
        Task<IEnumerable<Campeonato>> ObterCampeonatosComTimes();
        Task<Campeonato> ObterCampeonatoPorId(Guid id);
        Task AdicionarTimeAoCampeonato(Guid campeonatoId, Guid timeId);
        Task RemoverTimeDoCampeonato(Guid campeonatoId, Guid timeId);
        Task ResponderConviteCampeonato(Guid ParticipacaoId, Guid OrganizadorTimeId, bool aceito);
        Task<IEnumerable<CampeonatoTime>> ObterConvitesPorOrganizador(Guid organizadorTimeId);
        Task<IEnumerable<Campeonato>> ObterTodosComTimes();
        Task AbrirInscricoes(Guid campeonatoId);
    }
}
