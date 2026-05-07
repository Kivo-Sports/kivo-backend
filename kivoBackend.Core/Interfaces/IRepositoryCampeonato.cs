using kivoBackend.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Core.Interfaces
{
    public interface IRepositoryCampeonato : IRepositoryGenerics<Campeonato>
    {
        Task<Campeonato> ObterCampeonatoPorId(Guid id);
        Task<IEnumerable<Campeonato>> ObterCampeonatosComTimes();
    }
}
