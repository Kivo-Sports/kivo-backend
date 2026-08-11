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

        public IngressoLoteService(
            IRepositoryGenerics<IngressoLote> repositoryIngressoLote,
            IRepositoryGenerics<Partida> repositoryPartida) : base(repositoryIngressoLote)
        {
            _repositoryIngressoLote = repositoryIngressoLote;
            _repositoryPartida = repositoryPartida;
        }

        public async Task<IngressoLote> CriarLote(CriarIngressoLoteDTO dto)
        {
            var partida = await _repositoryPartida.ObterPorId(dto.PartidaId);
            if (partida == null) throw new Exception("Partida não encontrada.");
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