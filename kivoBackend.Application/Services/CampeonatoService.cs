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
        private readonly IRepositoryGenerics<Time> _timeRepository;
        private readonly IRepositoryGenerics<Partida> _partidaRepository;
        private readonly IRepositoryCampeonato _repositoryCampeonato;
        public CampeonatoService(IRepositoryGenerics<Campeonato> repositoryGenerics, IRepositoryGenerics<CampeonatoTime> CampeonatoTimeRepository, IRepositoryGenerics<Time> timeRepository, IRepositoryGenerics<Partida> partidaRepository, IRepositoryCampeonato repositoryCampeonato) : base(repositoryGenerics)
        {
            _CampeonatoTimeRepository = CampeonatoTimeRepository;
            _repositoryGenerics = repositoryGenerics;
            _timeRepository = timeRepository;
            _partidaRepository = partidaRepository;
            _repositoryCampeonato = repositoryCampeonato;
        }

        public async Task<IEnumerable<Campeonato>> ObterCampeonatosComTimes()
        {
            return await _repositoryCampeonato.ObterCampeonatosComTimes();
        }

        public async Task<Campeonato> ObterCampeonatoPorId(Guid id)
        {
            var campeonato = await _repositoryCampeonato.ObterCampeonatoPorId(id);
            if (campeonato == null)
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
            var campeonato = await _repositoryGenerics.ObterPorId(campeonatoId);
            if (campeonato == null)
                throw new Exception("Campeonato não encontrado.");

            var time = await _timeRepository.ObterPorId(timeId);
            if (time == null)
                throw new Exception("Time não encontrado.");

            if (time.EsporteId != campeonato.EsporteId)
                throw new Exception("Só é possível convidar times do mesmo esporte do campeonato.");

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
            var vinculos = await _CampeonatoTimeRepository.ObterComIncludes(x => x.Time);
            var participacao = vinculos.FirstOrDefault(x => x.Id == ParticipacaoId);
            if (participacao == null)
                throw new Exception("Esse convite não existe mais");

            if (participacao.Time == null || participacao.Time.OrganizadorTimeId != OrganizadorTimeId)
                throw new UnauthorizedAccessException("Este convite não pertence ao organizador autenticado.");

            participacao.EnumStatusParticipacao = aceito ? EnumStatusParticipacao.Aceito : EnumStatusParticipacao.Recusado;
            participacao.RespondidoEm = DateTime.Now;
            participacao.RespondidoPorOrganizadorTimeId = OrganizadorTimeId;
            await _CampeonatoTimeRepository.Atualizar(participacao);
        }

        public Task<IEnumerable<Campeonato>> ObterTodosComTimes()
        {
            throw new NotImplementedException();
        }

        public async Task<Campeonato> IniciarCampeonato(Guid campeonatoId)
        {
            var campeonato = await _repositoryCampeonato.ObterCampeonatoPorId(campeonatoId);
            int timesConfirmados = campeonato.CampeonatoTimes?.Count(ct => ct.EnumStatusParticipacao == EnumStatusParticipacao.Aceito) ?? 0;
            if (timesConfirmados == 0)
            {
                throw new Exception("Não é possível iniciar um campeonato sem times confirmados.");
            }
            if (campeonato.FormatoCampeonato == EnumFormatoCampeonato.MataMata)
            {
                if (!ePotenciaDeDois(timesConfirmados))
                    throw new Exception("Mata-mata requer um número de times que seja potência de 2 (ex: 4, 8, 16).");
            }
            else if (campeonato.FormatoCampeonato == EnumFormatoCampeonato.PontosCorridos)
            {
                if (timesConfirmados < 8)
                    throw new Exception("Pontos corridos requer pelo menos 8 times.");
            }

            campeonato.EnumStatusCampeonato = EnumStatusCampeonato.EmAndamento;
            await _repositoryGenerics.Atualizar(campeonato);
            return campeonato;
        }
        private bool ePotenciaDeDois(int n) => n > 0 && (n & (n - 1)) == 0;

        public async Task<Campeonato> EditarCampeonato(Guid campeonatoId, EditarCampeonatoDto dto, bool ehAdmin = false)
        {
            var campeonato = await _repositoryCampeonato.ObterCampeonatoPorId(campeonatoId);
            if (campeonato == null)
                throw new Exception("Campeonato não encontrado.");

            if (!ehAdmin &&
                (campeonato.EnumStatusCampeonato == EnumStatusCampeonato.EmAndamento ||
                 campeonato.EnumStatusCampeonato == EnumStatusCampeonato.Finalizado))
            {
                throw new Exception("Não é possível editar um campeonato que já iniciou ou finalizou.");
            }

            if (dto.EsporteId != Guid.Empty)
                campeonato.EsporteId = dto.EsporteId;
            campeonato.Nome = dto.Nome;
            campeonato.DataInicio = dto.DataInicio;
            campeonato.DataFim = dto.DataFim;
            campeonato.PontosVitoria = dto.PontosVitoria;
            campeonato.PontosDerrota = dto.PontosDerrota;
            campeonato.PontosEmpate = dto.PontosEmpate;
            campeonato.FormatoCampeonato = dto.FormatoCampeonato;
            campeonato.QuantidadeTimesClassificam = dto.QuantidadeTimesClassificam;
            if (!string.IsNullOrEmpty(dto.LogoUrl))
                campeonato.LogoUrl = dto.LogoUrl;

            await _repositoryGenerics.Atualizar(campeonato);
            return campeonato;
        }

        public async Task DeletarCampeonatoAdmin(Guid campeonatoId)
        {
            var campeonato = await _repositoryGenerics.ObterPorId(campeonatoId);
            if (campeonato == null) throw new Exception("Campeonato não encontrado.");

            // Remove partidas (FK Restrict aponta para o campeonato)
            var partidas = await _partidaRepository.Buscar(p => p.CampeonatoId == campeonatoId);
            foreach (var partida in partidas)
                await _partidaRepository.Remover(partida.Id);

            // Remove vínculos com times
            var vinculos = await _CampeonatoTimeRepository.Buscar(ct => ct.CampeonatoId == campeonatoId);
            foreach (var vinculo in vinculos)
                await _CampeonatoTimeRepository.Remover(vinculo.Id);

            await _repositoryGenerics.Remover(campeonatoId);
        }

        public async Task DescancelarCampeonato(Guid campeonatoId)
        {
            var campeonato = await _repositoryGenerics.ObterPorId(campeonatoId);
            if (campeonato == null) throw new Exception("Campeonato não encontrado.");

            if (campeonato.EnumStatusCampeonato != EnumStatusCampeonato.Cancelado)
                throw new Exception("Apenas campeonatos cancelados podem ser descancelados.");

            // Volta o status para a linha do tempo normal (recalculado por data no getter).
            campeonato.EnumStatusCampeonato = EnumStatusCampeonato.InscricoesAbertas;
            await _repositoryGenerics.Atualizar(campeonato);
        }

        public async Task ReatribuirCampeonato(Guid campeonatoId, Guid novoOrganizadorCampeonatoId)
        {
            var campeonato = await _repositoryGenerics.ObterPorId(campeonatoId);
            if (campeonato == null) throw new Exception("Campeonato não encontrado.");

            campeonato.OrganizadorCampeonatoId = novoOrganizadorCampeonatoId;
            await _repositoryGenerics.Atualizar(campeonato);
        }

        public async Task CancelarCampeonato(Guid campeonatoId)
        {
            var campeonato = await _repositoryCampeonato.ObterCampeonatoPorId(campeonatoId);
            if (campeonato == null) throw new Exception("Campeonato não encontrado.");

            if (campeonato.EnumStatusCampeonato == EnumStatusCampeonato.Finalizado)
                throw new Exception("Não é possível cancelar um campeonato que já foi finalizado.");

            if (campeonato.EnumStatusCampeonato == EnumStatusCampeonato.Cancelado)
                throw new Exception("Este campeonato já está cancelado.");

            campeonato.EnumStatusCampeonato = EnumStatusCampeonato.Cancelado;
            await _repositoryGenerics.Atualizar(campeonato);

            if (campeonato.Partidas != null && campeonato.Partidas.Any())
            {
                var partidasParaRemover = campeonato.Partidas.Where(p => !p.Finalizado).ToList();

                foreach (var partida in partidasParaRemover)
                {
                    campeonato.Partidas.Remove(partida);
                }

                await _repositoryGenerics.Atualizar(campeonato);
            }
        }
    }
}
