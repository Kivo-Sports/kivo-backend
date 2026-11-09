using kivoBackend.Application.Interfaces;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using kivoBackend.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Application.Services
{
    public class TimeService : ServiceGenerics<Time>, ITimeService
    {
        // Passa o IRepositoryTime para a base para que ObterTodos/ObterPorId tragam o Esporte (Include).
        private readonly IRepositoryGenerics<CampeonatoTime>? _campeonatoTimeRepository;

        public TimeService(IRepositoryTime timeRepository, IRepositoryGenerics<CampeonatoTime>? campeonatoTimeRepository = null) : base(timeRepository)
        {
            _campeonatoTimeRepository = campeonatoTimeRepository;
        }

        public async Task<bool> ParticipaDeCampeonatoAtivo(Guid timeId)
        {
            var repositorio = _campeonatoTimeRepository ?? throw new InvalidOperationException("Repositório de participações obrigatório.");
            var vinculos = await repositorio.ObterComIncludes(ct => ct.Campeonato);
            return vinculos.Any(ct =>
                ct.TimeId == timeId &&
                ct.Campeonato != null &&
                (ct.Campeonato.EnumStatusCampeonato == EnumStatusCampeonato.InscricoesAbertas ||
                 ct.Campeonato.EnumStatusCampeonato == EnumStatusCampeonato.EmAndamento));
        }
    }
}
