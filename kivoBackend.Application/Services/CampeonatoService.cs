using kivoBackend.Application.DTO;
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
        private readonly IRepositoryGenerics<Campeonato> _repositoryGenerics;
        private readonly IRepositoryCampeonato _repositoryCampeonato;
        public CampeonatoService(IRepositoryGenerics<Campeonato> repositoryGenerics, IRepositoryGenerics<CampeonatoTime> CampeonatoTimeRepository, IRepositoryCampeonato repositoryCampeonato) : base(repositoryGenerics)
        {
            _CampeonatoTimeRepository = CampeonatoTimeRepository;
            _repositoryGenerics = repositoryGenerics;
            _repositoryCampeonato = repositoryCampeonato;
        }

        public async Task<IEnumerable<Campeonato>> ObterCampeonatosComTimes()
        {
            return await _repositoryCampeonato.ObterCampeonatosComTimes();
        }

        public async Task<Campeonato> ObterCampeonatoPorId(Guid id)
        {
            var campeonato = await _repositoryCampeonato.ObterCampeonatoPorId(id);
            if(campeonato == null)
                throw new Exception("Campeonato não encontrado");
            return campeonato;
        }

        public async Task AbrirInscricoes(Guid campeonatoId)
        {
            var campeonato = await _repositoryGenerics.ObterPorId(campeonatoId);
            if (campeonato == null)
                throw new Exception("Campeonato não encontrado.");

            if (campeonato.EnumStatusCampeonato != EnumStatusCampeonato.Rascunho)
                throw new Exception("Inscrições só podem ser abertas a partir do status Rascunho.");

            campeonato.EnumStatusCampeonato = EnumStatusCampeonato.InscricoesAbertas;
            await _repositoryGenerics.Atualizar(campeonato);
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
             var vinculos = await _CampeonatoTimeRepository.ObterComIncludes(
                 x => x.Time,
                 x => x.Campeonato
             );

            return vinculos.Where(x =>
                x.EnumStatusParticipacao == EnumStatusParticipacao.Pendente &&
                x.Time != null &&
                x.Time.OrganizadorTimeId == organizadorTimeId
            );
        }

        public async Task<IEnumerable<CampeonatoTime>> ObterConvitesPorCampeonato(Guid campeonatoId)
        {
            var convites = await _CampeonatoTimeRepository.ObterComIncludes
                (x => x.Time);

            return convites.Where(x => x.CampeonatoId == campeonatoId);
        }

        public async Task RemoverTimeDoCampeonato(Guid campeonatoId, Guid timeId)
        {
            var vinculo = await _CampeonatoTimeRepository
                .BuscarPrimeiro(x => x.CampeonatoId == campeonatoId && x.TimeId == timeId);

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

        public Task<IEnumerable<Campeonato>> ObterTodosComTimes()
        {
            throw new NotImplementedException();
        }

        public async Task<Campeonato> IniciarCampeonato(Guid campeonatoId)
        {
            var campeonato = await _repositoryCampeonato.ObterCampeonatoPorId(campeonatoId);

            if (campeonato == null)
                throw new Exception("Campeonato não encontrado.");

            int contagemTimesConfirmados = campeonato.CampeonatoTimes?
                .Count(ct => ct.EnumStatusParticipacao == EnumStatusParticipacao.Aceito) ?? 0;

            if (contagemTimesConfirmados < 8)
            {
                throw new Exception($"O campeonato precisa de pelo menos 8 times confirmados. Atuais: {contagemTimesConfirmados}");
            }

            campeonato.EnumStatusCampeonato = EnumStatusCampeonato.EmAndamento;
            await _repositoryGenerics.Atualizar(campeonato);
            return campeonato;
        }

        public async Task<Campeonato> EditarCampeonato(Guid campeonatoId, EditarCampeonatoDto dto)
        {
            var campeonato = await _repositoryCampeonato.ObterCampeonatoPorId(campeonatoId);
            if (campeonato == null)
                throw new Exception("Campeonato não encontrado.");

            if (campeonato.EnumStatusCampeonato == EnumStatusCampeonato.EmAndamento ||
                     campeonato.EnumStatusCampeonato == EnumStatusCampeonato.Finalizado)
            {
                throw new Exception("Não é possível editar um campeonato que já iniciou ou finalizou.");
            }

            campeonato.Nome = dto.Nome;
            campeonato.DataInicio = dto.DataInicio;
            campeonato.DataFim = dto.DataFim;
            campeonato.PontosVitoria = dto.PontosVitoria;
            campeonato.PontosDerrota = dto.PontosDerrota;
            campeonato.PontosEmpate = dto.PontosEmpate;

            await _repositoryGenerics.Atualizar(campeonato);
            return campeonato;
        }
    }
}
