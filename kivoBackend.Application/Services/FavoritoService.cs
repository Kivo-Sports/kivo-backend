using kivoBackend.Application.DTO;
using kivoBackend.Application.Interfaces;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using kivoBackend.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace kivoBackend.Application.Services
{
    public class FavoritoService : IFavoritoService
    {
        private readonly IRepositoryGenerics<Favorito> _favoritoRepository;
        private readonly IRepositoryGenerics<Time> _timeRepository;
        private readonly IRepositoryGenerics<Partida> _partidaRepository;
        private readonly IRepositoryCampeonato _repositoryCampeonato;

        public FavoritoService(
            IRepositoryGenerics<Favorito> favoritoRepository,
            IRepositoryGenerics<Time> timeRepository,
            IRepositoryGenerics<Partida> partidaRepository,
            IRepositoryCampeonato repositoryCampeonato)
        {
            _favoritoRepository = favoritoRepository;
            _timeRepository = timeRepository;
            _partidaRepository = partidaRepository;
            _repositoryCampeonato = repositoryCampeonato;
        }

        public async Task Adicionar(Guid usuarioId, EnumTipoFavorito tipo, Guid itemId)
        {
            await ValidarItemFavoritoExiste(tipo, itemId);

            var existente = await _favoritoRepository.BuscarPrimeiro(f =>
                f.UsuarioId == usuarioId && f.Tipo == tipo && f.ItemId == itemId);

            if (existente != null) return; // já favoritado

            await _favoritoRepository.Adicionar(new Favorito
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuarioId,
                Tipo = tipo,
                ItemId = itemId,
                CriadoEm = DateTime.Now
            });
        }

        private async Task ValidarItemFavoritoExiste(EnumTipoFavorito tipo, Guid itemId)
        {
            if (itemId == Guid.Empty)
                throw new ArgumentException("Item favorito inválido.");

            switch (tipo)
            {
                case EnumTipoFavorito.Time:
                    if (await _timeRepository.ObterPorId(itemId) == null)
                        throw new KeyNotFoundException("Time não encontrado para favoritar.");
                    return;
                case EnumTipoFavorito.Campeonato:
                    if (await _repositoryCampeonato.ObterCampeonatoPorId(itemId) == null)
                        throw new KeyNotFoundException("Campeonato não encontrado para favoritar.");
                    return;
                default:
                    throw new ArgumentException("Tipo de favorito inválido.");
            }
        }

        public async Task Remover(Guid usuarioId, EnumTipoFavorito tipo, Guid itemId)
        {
            var favorito = await _favoritoRepository.BuscarPrimeiro(f =>
                f.UsuarioId == usuarioId && f.Tipo == tipo && f.ItemId == itemId);

            if (favorito != null)
                await _favoritoRepository.Remover(favorito.Id);
        }

        public async Task<FavoritosResponseDTO> ListarPorUsuario(Guid usuarioId)
        {
            var favoritos = (await _favoritoRepository.Buscar(f => f.UsuarioId == usuarioId)).ToList();
            var timeIds = favoritos.Where(f => f.Tipo == EnumTipoFavorito.Time).Select(f => f.ItemId).ToHashSet();
            var campIds = favoritos.Where(f => f.Tipo == EnumTipoFavorito.Campeonato).Select(f => f.ItemId).ToHashSet();

            var resposta = new FavoritosResponseDTO();

            if (timeIds.Count > 0)
            {
                var times = await _timeRepository.ObterComIncludes(t => t.Esporte);
                resposta.Times = times
                    .Where(t => timeIds.Contains(t.Id))
                    .Select(t => new FavoritoTimeDTO
                    {
                        Id = t.Id,
                        Nome = t.Nome,
                        LogoUrl = t.LogoUrl,
                        Cidade = t.Cidade,
                        Estado = t.Estado,
                        EsporteNome = t.Esporte?.Nome,
                        EsporteIcone = t.Esporte?.Icone
                    })
                    .OrderBy(t => t.Nome)
                    .ToList();
            }

            if (campIds.Count > 0)
            {
                var campeonatos = await _repositoryCampeonato.ObterCampeonatosComTimes();
                resposta.Campeonatos = campeonatos
                    .Where(c => campIds.Contains(c.Id))
                    .Select(c => new FavoritoCampeonatoDTO
                    {
                        Id = c.Id,
                        Nome = c.Nome,
                        LogoUrl = c.LogoUrl,
                        Status = c.EnumStatusCampeonato.ToString(),
                        EsporteNome = c.Esporte?.Nome,
                        EsporteIcone = c.Esporte?.Icone,
                        DataInicio = c.DataInicio,
                        DataFim = c.DataFim
                    })
                    .OrderBy(c => c.Nome)
                    .ToList();
            }

            return resposta;
        }

        public async Task<List<TimelineItemDTO>> ObterTimeline(Guid usuarioId)
        {
            var favoritos = (await _favoritoRepository.Buscar(f => f.UsuarioId == usuarioId)).ToList();
            var timeIds = favoritos.Where(f => f.Tipo == EnumTipoFavorito.Time).Select(f => f.ItemId).ToHashSet();
            var campIds = favoritos.Where(f => f.Tipo == EnumTipoFavorito.Campeonato).Select(f => f.ItemId).ToHashSet();

            if (timeIds.Count == 0 && campIds.Count == 0)
                return new List<TimelineItemDTO>();

            var agora = DateTime.Now;

            var partidas = await _partidaRepository.ObterComIncludes(p => p.TimeCasa, p => p.TimeVisitante);

            var proximos = partidas
                .Where(p =>
                    !p.Finalizado &&
                    p.DataHora != null &&
                    p.DataHora >= agora &&
                    (campIds.Contains(p.CampeonatoId) ||
                     (p.TimeCasaId != null && timeIds.Contains(p.TimeCasaId.Value)) ||
                     (p.TimeVisitanteId != null && timeIds.Contains(p.TimeVisitanteId.Value))))
                .OrderBy(p => p.DataHora)
                .Take(40)
                .ToList();

            if (proximos.Count == 0)
                return new List<TimelineItemDTO>();

            var campeonatos = (await _repositoryCampeonato.ObterCampeonatosComTimes())
                .ToDictionary(c => c.Id, c => c);

            return proximos.Select(p =>
            {
                string origem;
                if (p.TimeCasaId != null && timeIds.Contains(p.TimeCasaId.Value))
                    origem = $"Seu time: {p.TimeCasa?.Nome}";
                else if (p.TimeVisitanteId != null && timeIds.Contains(p.TimeVisitanteId.Value))
                    origem = $"Seu time: {p.TimeVisitante?.Nome}";
                else
                    origem = "Campeonato favorito";

                campeonatos.TryGetValue(p.CampeonatoId, out var camp);

                return new TimelineItemDTO
                {
                    PartidaId = p.Id,
                    CampeonatoId = p.CampeonatoId,
                    CampeonatoNome = camp?.Nome ?? "Campeonato",
                    TimeCasa = p.TimeCasa?.Nome ?? "A definir",
                    TimeVisitante = p.TimeVisitante?.Nome ?? "A definir",
                    LogoCasa = p.TimeCasa?.LogoUrl,
                    LogoVisitante = p.TimeVisitante?.LogoUrl,
                    DataHora = p.DataHora,
                    Local = p.Local,
                    Origem = origem
                };
            }).ToList();
        }
    }
}
