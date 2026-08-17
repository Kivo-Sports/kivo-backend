using kivoBackend.Application.DTO;
using kivoBackend.Application.Interfaces;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace kivoBackend.Application.Services
{
    public class IngressoLoteService : ServiceGenerics<IngressoLote>, IIngressoLoteService
    {
        private readonly IRepositoryGenerics<IngressoLote> _repositoryIngressoLote;
        private readonly IRepositoryGenerics<Partida> _repositoryPartida;
        private readonly IRepositoryCampeonato _repositoryCampeonato;

        public IngressoLoteService(
            IRepositoryGenerics<IngressoLote> repositoryIngressoLote,
            IRepositoryGenerics<Partida> repositoryPartida,
            IRepositoryCampeonato repositoryCampeonato) : base(repositoryIngressoLote)
        {
            _repositoryIngressoLote = repositoryIngressoLote;
            _repositoryPartida = repositoryPartida;
            _repositoryCampeonato = repositoryCampeonato;
        }

        public async Task<IngressoLote> CriarLote(CriarIngressoLoteDTO dto, Guid? organizadorCampeonatoId, bool ehAdmin)
        {
            var partida = await _repositoryPartida.ObterPorId(dto.PartidaId);
            if (partida == null) throw new Exception("Partida não encontrada.");

            var campeonato = await _repositoryCampeonato.ObterCampeonatoPorId(partida.CampeonatoId);
            if (campeonato == null) throw new Exception("Campeonato da partida não encontrado.");
            if (!ehAdmin && organizadorCampeonatoId != campeonato.OrganizadorCampeonatoId)
                throw new UnauthorizedAccessException("Você não tem permissão para criar lote nesta partida.");

            if (dto.QuantidadeTotal <= 0) throw new Exception("Quantidade total deve ser maior que zero.");
            if (dto.Preco <= 0) throw new Exception("Preço deve ser maior que zero.");

            var novoLote = new IngressoLote
            {
                Id = Guid.NewGuid(),
                PartidaId = dto.PartidaId,
                NomeLote = dto.NomeLote,
                Preco = dto.Preco,
                QuantidadeTotal = dto.QuantidadeTotal,
                QuantidadeDisponivel = dto.QuantidadeTotal,
                Ativo = true,
            };

            return await _repositoryIngressoLote.Adicionar(novoLote);
        }

        public async Task<IEnumerable<IngressoLote>> ObterLotesPorPartida(Guid partidaId)
        {
            return await _repositoryIngressoLote.Buscar(l => l.PartidaId == partidaId && l.Ativo);
        }
    }
}
