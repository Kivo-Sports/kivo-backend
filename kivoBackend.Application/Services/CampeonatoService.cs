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
    public class CampeonatoService : ServiceGenerics<Campeonato>, ICampeonatoService
    {
        private readonly IRepositoryGenerics<CampeonatoTime> _CampeonatoTimeRepository;
        private readonly IRepositoryGenerics<Campeonato> _campeonatoRepository;
        public CampeonatoService(IRepositoryGenerics<Campeonato> repositoryGenerics, IRepositoryGenerics<CampeonatoTime> CampeonatoTimeRepository, IRepositoryCampeonato RepositoryCampeonato) : base(repositoryGenerics)
        {
            _CampeonatoTimeRepository = CampeonatoTimeRepository;
            _campeonatoRepository = repositoryGenerics;
        }

        public async Task<IEnumerable<Campeonato>> ObterTodosComTimes()
        {
            var campeonatos = await _campeonatoRepository.ObterTodosComIncludes(c => c.CampeonatoTimes);
            var agora = DateTime.Now;

            foreach (var c in campeonatos)
            {
                var novoStatus = ReconciliarStatus(c, agora);
                if (novoStatus != c.EnumStatusCampeonato)
                {
                    c.EnumStatusCampeonato = novoStatus;
                    await _campeonatoRepository.Atualizar(c);
                }
            }

            return campeonatos;
        }

        private static EnumStatusCampeonato ReconciliarStatus(Campeonato c, DateTime agora)
        {
            if (c.EnumStatusCampeonato == EnumStatusCampeonato.Cancelado ||
                c.EnumStatusCampeonato == EnumStatusCampeonato.Rascunho)
                return c.EnumStatusCampeonato;

            if (c.DataFim <= agora)
                return EnumStatusCampeonato.Finalizado;

            if (c.DataInicio <= agora && c.EnumStatusCampeonato == EnumStatusCampeonato.InscricoesAbertas)
                return EnumStatusCampeonato.EmAndamento;

            return c.EnumStatusCampeonato;
        }

        public async Task AbrirInscricoes(Guid campeonatoId)
        {
            var campeonato = await _campeonatoRepository.ObterPorId(campeonatoId);
            if (campeonato == null)
                throw new Exception("Campeonato não encontrado.");

            if (campeonato.EnumStatusCampeonato != EnumStatusCampeonato.Rascunho)
                throw new Exception("Inscrições só podem ser abertas a partir do status Rascunho.");

            campeonato.EnumStatusCampeonato = EnumStatusCampeonato.InscricoesAbertas;
            await _campeonatoRepository.Atualizar(campeonato);
        }

        public async Task AdicionarTimeAoCampeonato(Guid campeonatoId, Guid timeId)
        {
            var novoConvite = new CampeonatoTime
            {
                Id = Guid.NewGuid(),
                CampeonatoId = campeonatoId,
                TimeId = timeId,
                EnumStatusParticipacao = EnumStatusParticipacao.Pendente,
                ConvidadoEm = DateTime.Now
            };

            await _CampeonatoTimeRepository.Adicionar(novoConvite);
        }

        public async Task<IEnumerable<CampeonatoTime>> ObterConvitesPorOrganizador(Guid organizadorTimeId)
        {
            var vinculos = await _CampeonatoTimeRepository.ObterTodosComIncludes(
                x => x.Time,
                x => x.Campeonato
            );

            return vinculos.Where(x =>
                x.EnumStatusParticipacao == EnumStatusParticipacao.Pendente &&
                x.Time != null &&
                x.Time.OrganizadorTimeId == organizadorTimeId
            );
        }

        public async Task RemoverTimeDoCampeonato(Guid campeonatoId, Guid timeId)
        {
            var lista = await _CampeonatoTimeRepository.ObterTodos();
            var vinculo = lista.FirstOrDefault(x => x.CampeonatoId == campeonatoId && x.TimeId == timeId);
            if (vinculo != null)
            {
                await _CampeonatoTimeRepository.Remover(vinculo.Id);
            }
        }

        public async Task ResponderConviteCampeonato(Guid ParticipacaoId, Guid OrganizadorTimeId, bool aceito)
        {
            var participacao = await _CampeonatoTimeRepository.ObterPorId(ParticipacaoId);
            if (participacao != null)
            {
                participacao.EnumStatusParticipacao = aceito ? EnumStatusParticipacao.Aceito : EnumStatusParticipacao.Recusado;
                participacao.RespondidoEm = DateTime.Now;
                participacao.RespondidoPorOrganizadorTimeId = OrganizadorTimeId;
                await _CampeonatoTimeRepository.Atualizar(participacao);
            }
            else
            {
                throw new Exception("Esse convite não existe mais");
            }
        }
    }
}
