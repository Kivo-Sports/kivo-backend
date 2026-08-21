using kivoBackend.Application.DTO;
using kivoBackend.Application.Interfaces;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using kivoBackend.Core.Interfaces;
using kivoBackend.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace kivoBackend.Application.Services
{
    public class IngressoLoteService : ServiceGenerics<IngressoLote>, IIngressoLoteService
    {
        private readonly IRepositoryGenerics<IngressoLote> _repositoryIngressoLote;
        private readonly IRepositoryGenerics<Partida> _repositoryPartida;
        private readonly INotificacaoService _notificacaoService;
        private readonly IRepositoryGenerics<Favorito> _repositoryFavorito;
        private readonly IRepositoryTime _repositoryTime;

        public IngressoLoteService(
            IRepositoryGenerics<IngressoLote> repositoryIngressoLote, IRepositoryGenerics<Favorito> repositoryFavorito,
            IRepositoryGenerics<Partida> repositoryPartida, INotificacaoService notificacaoService, IRepositoryTime repositoryTime) : base(repositoryIngressoLote)
        {
            _repositoryIngressoLote = repositoryIngressoLote;
            _repositoryPartida = repositoryPartida;
            _notificacaoService = notificacaoService;
            _repositoryFavorito = repositoryFavorito;
            _repositoryTime = repositoryTime;
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
            await NotificarTorcedoresIngressosDisponiveis(partida, novoLote);
            return await _repositoryIngressoLote.Adicionar(novoLote);
        }

        public async Task<IEnumerable<IngressoLote>> ObterLotesPorPartida(Guid partidaId)
        {
            return await _repositoryIngressoLote.Buscar(l => l.PartidaId == partidaId && l.Ativo);
        }

        private async Task NotificarTorcedoresIngressosDisponiveis(Partida partida, IngressoLote lote)
        {
            var timesIds = new List<Guid>();
            if (partida.TimeCasaId.HasValue) timesIds.Add(partida.TimeCasaId.Value);
            if (partida.TimeVisitanteId.HasValue) timesIds.Add(partida.TimeVisitanteId.Value);

            if (!timesIds.Any()) return;

            var timeCasa = partida.TimeCasaId.HasValue ? await _repositoryTime.ObterPorId(partida.TimeCasaId.Value) : null;
            var timeVisitante = partida.TimeVisitanteId.HasValue ? await _repositoryTime.ObterPorId(partida.TimeVisitanteId.Value) : null;

            string confronto = $"{timeCasa?.Nome ?? "Time"} x {timeVisitante?.Nome ?? "Time"}";

            var favoritos = await _repositoryFavorito.Buscar(f => timesIds.Contains(f.ItemId));
            var usuariosNotificar = favoritos.Select(f => f.UsuarioId).Distinct().ToList();

            foreach (var usuarioId in usuariosNotificar)
            {
                await _notificacaoService.CriarNotificacaoAsync(
                    usuarioId,
                    "Ingressos Disponíveis! 🎟️",
                    $"Os ingressos para {confronto} ({lote.NomeLote} - R$ {lote.Preco:F2}) já estão à venda!",
                    EnumTipoNotificacao.IngressoConfirmado,
                    link: $"/partidas/{partida.Id}",
                    enviarEmail: false
                );
            }
        }
    }
}