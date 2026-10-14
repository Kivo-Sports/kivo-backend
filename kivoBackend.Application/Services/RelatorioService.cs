using kivoBackend.Application.DTO;
using kivoBackend.Application.Interfaces;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using kivoBackend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace kivoBackend.Application.Services;

/// <summary>Gera dados já agregados para os dashboards, sem expor compradores ou CPFs.</summary>
public class RelatorioService : IRelatorioService
{
    private readonly AppDbContext _context;

    public RelatorioService(AppDbContext context) => _context = context;

    public async Task<RelatorioOrganizadorCampeonatoDTO> ObterOrganizadorCampeonatoAsync(Guid usuarioId, FiltroRelatorioDTO filtro)
    {
        ValidarPeriodo(filtro);
        var organizadorId = await _context.OrganizadoresCampeonato.AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();
        if (!organizadorId.HasValue) throw new UnauthorizedAccessException("Perfil de organizador de campeonato não encontrado.");

        var campeonatos = await _context.Campeonatos.AsNoTracking()
            .Where(x => x.OrganizadorCampeonatoId == organizadorId.Value).ToListAsync();
        var campeonatoIds = campeonatos.Select(x => x.Id).ToHashSet();
        var partidas = await _context.Partidas.AsNoTracking().Where(x => campeonatoIds.Contains(x.CampeonatoId)).ToListAsync();
        var times = await _context.Times.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Nome);
        var participacoes = await _context.CampeonatoTimes.AsNoTracking()
            .Where(x => campeonatoIds.Contains(x.CampeonatoId) && x.EnumStatusParticipacao == EnumStatusParticipacao.Aceito).ToListAsync();
        var financeiro = await CarregarFinanceiroAsync(partidas.Select(x => x.Id), filtro);

        return new RelatorioOrganizadorCampeonatoDTO
        {
            Inicio = filtro.Inicio,
            Fim = filtro.Fim,
            Resumo = CriarResumo(financeiro.Ingressos),
            Campeonatos = campeonatos.Count,
            TimesParticipantes = participacoes.Select(x => x.TimeId).Distinct().Count(),
            PartidasRealizadas = partidas.Count(x => x.Finalizado && NoPeriodo(x.DataHora, filtro)),
            PartidasAgendadas = partidas.Count(x => !x.Finalizado && x.DataHora.HasValue && x.DataHora >= DateTime.UtcNow && NoPeriodo(x.DataHora, filtro)),
            VendasPorDia = CriarSerieVendas(financeiro.Ingressos),
            VendasPorPartida = CriarVendasPorPartida(financeiro.Ingressos, financeiro.Lotes, partidas, times),
            OcupacaoPorLote = CriarOcupacao(financeiro.Lotes)
        };
    }

    public async Task<RelatorioOrganizadorTimeDTO> ObterOrganizadorTimeAsync(Guid usuarioId, FiltroRelatorioDTO filtro)
    {
        ValidarPeriodo(filtro);
        var organizadorId = await _context.OrganizadoresTime.AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();
        if (!organizadorId.HasValue) throw new UnauthorizedAccessException("Perfil de organizador de time não encontrado.");

        var times = await _context.Times.AsNoTracking().Where(x => x.OrganizadorTimeId == organizadorId.Value).ToListAsync();
        var timeIds = times.Select(x => x.Id).ToHashSet();
        var partidas = await _context.Partidas.AsNoTracking()
            .Where(x => x.TimeCasaId.HasValue && timeIds.Contains(x.TimeCasaId.Value) || x.TimeVisitanteId.HasValue && timeIds.Contains(x.TimeVisitanteId.Value)).ToListAsync();
        var participacoes = await _context.CampeonatoTimes.AsNoTracking()
            .Where(x => timeIds.Contains(x.TimeId) && x.EnumStatusParticipacao == EnumStatusParticipacao.Aceito).ToListAsync();
        var favoritos = await _context.Favoritos.AsNoTracking()
            .Where(x => x.Tipo == EnumTipoFavorito.Time && timeIds.Contains(x.ItemId)).ToListAsync();

        return new RelatorioOrganizadorTimeDTO
        {
            Inicio = filtro.Inicio,
            Fim = filtro.Fim,
            Times = times.Count,
            CampeonatosDisputados = participacoes.Select(x => x.CampeonatoId).Distinct().Count(),
            JogosRealizados = partidas.Count(x => x.Finalizado && NoPeriodo(x.DataHora, filtro)),
            JogosAgendados = partidas.Count(x => !x.Finalizado && x.DataHora.HasValue && x.DataHora >= DateTime.UtcNow && NoPeriodo(x.DataHora, filtro)),
            DesempenhoPorTime = times.Select(time => CriarDesempenho(time, partidas, favoritos, filtro)).ToList()
        };
    }

    public async Task<RelatorioAdministrativoDTO> ObterAdministrativoAsync(FiltroRelatorioDTO filtro)
    {
        ValidarPeriodo(filtro);
        var usuarios = await _context.Usuarios.AsNoTracking().ToListAsync();
        var campeonatos = await _context.Campeonatos.AsNoTracking().Include(x => x.Esporte).ToListAsync();
        var partidas = await _context.Partidas.AsNoTracking().ToListAsync();
        var times = await _context.Times.AsNoTracking().ToListAsync();
        var financeiro = await CarregarFinanceiroAsync(partidas.Select(x => x.Id), filtro);

        return new RelatorioAdministrativoDTO
        {
            Inicio = filtro.Inicio,
            Fim = filtro.Fim,
            UsuariosTotais = usuarios.Count,
            NovosUsuarios = usuarios.Count(x => NoPeriodo(x.CriadoEm, filtro)),
            CampeonatosTotais = campeonatos.Count,
            CampeonatosEmAndamento = campeonatos.Count(x => x.EnumStatusCampeonato == EnumStatusCampeonato.EmAndamento),
            PartidasRealizadas = partidas.Count(x => x.Finalizado && NoPeriodo(x.DataHora, filtro)),
            TimesAtivos = times.Count(x => x.Ativo),
            ResumoFinanceiro = CriarResumo(financeiro.Ingressos),
            VendasPorDia = CriarSerieVendas(financeiro.Ingressos),
            UsuariosPorPerfil = usuarios.GroupBy(x => x.EnumCargo.ToString()).Select(x => new ItemQuantidadeDTO { Nome = x.Key, Quantidade = x.Count() }).OrderBy(x => x.Nome).ToList(),
            CampeonatosPorEsporte = campeonatos.GroupBy(x => x.Esporte?.Nome ?? "Sem esporte").Select(x => new ItemQuantidadeDTO { Nome = x.Key, Quantidade = x.Count() }).OrderByDescending(x => x.Quantidade).ToList()
        };
    }

    public async Task<RelatorioTorcedorDTO> ObterTorcedorAsync(Guid usuarioId, FiltroRelatorioDTO filtro)
    {
        ValidarPeriodo(filtro);
        var torcedorExiste = await _context.Torcedores.AsNoTracking().AnyAsync(x => x.UsuarioId == usuarioId);
        if (!torcedorExiste) throw new UnauthorizedAccessException("Perfil de torcedor não encontrado.");

        var query = _context.Ingressos.AsNoTracking().Where(x => x.UsuarioId == usuarioId);
        if (filtro.Inicio.HasValue) query = query.Where(x => x.DataCompra >= filtro.Inicio.Value.Date);
        if (filtro.Fim.HasValue) query = query.Where(x => x.DataCompra < filtro.Fim.Value.Date.AddDays(1));
        var ingressos = await query.ToListAsync();
        var loteIds = ingressos.Select(x => x.IngressoLoteId).Distinct().ToHashSet();
        var lotes = await _context.IngressoLotes.AsNoTracking().Where(x => loteIds.Contains(x.Id)).ToListAsync();
        var partidaIds = lotes.Select(x => x.PartidaId).Distinct().ToHashSet();
        var partidas = await _context.Partidas.AsNoTracking().Where(x => partidaIds.Contains(x.Id)).ToListAsync();
        var timeIds = partidas.Where(x => x.TimeCasaId.HasValue || x.TimeVisitanteId.HasValue)
            .SelectMany(x => new[] { x.TimeCasaId, x.TimeVisitanteId }.Where(id => id.HasValue).Select(id => id!.Value)).ToHashSet();
        var nomesTimes = await _context.Times.AsNoTracking().Where(x => timeIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Nome);
        var favoritos = await _context.Favoritos.AsNoTracking().Where(x => x.UsuarioId == usuarioId).ToListAsync();
        var confirmados = ingressos.Where(x => x.StatusIngresso is EnumStatusIngresso.Pago or EnumStatusIngresso.Utilizado).ToList();

        return new RelatorioTorcedorDTO
        {
            Inicio = filtro.Inicio,
            Fim = filtro.Fim,
            GastosConfirmados = confirmados.Sum(x => x.PrecoPago),
            IngressosPagos = confirmados.Count,
            IngressosPendentes = ingressos.Count(x => x.StatusIngresso == EnumStatusIngresso.Pendente),
            IngressosUtilizados = ingressos.Count(x => x.StatusIngresso == EnumStatusIngresso.Utilizado),
            TimesFavoritos = favoritos.Count(x => x.Tipo == EnumTipoFavorito.Time),
            CampeonatosFavoritos = favoritos.Count(x => x.Tipo == EnumTipoFavorito.Campeonato),
            ComprasPorDia = CriarSerieVendas(ingressos),
            ProximasPartidas = partidas.Where(x => x.DataHora.HasValue && x.DataHora >= DateTime.UtcNow && !x.Finalizado)
                .OrderBy(x => x.DataHora).Select(x => new ProximaPartidaDTO
                {
                    PartidaId = x.Id,
                    Confronto = $"{NomeTime(x.TimeCasaId, nomesTimes, "Time casa")} x {NomeTime(x.TimeVisitanteId, nomesTimes, "Time visitante")}",
                    DataHora = x.DataHora,
                    Local = x.Local
                }).ToList()
        };
    }

    private async Task<(List<Ingresso> Ingressos, List<IngressoLote> Lotes)> CarregarFinanceiroAsync(IEnumerable<Guid> partidaIds, FiltroRelatorioDTO filtro)
    {
        var ids = partidaIds.ToHashSet();
        var lotes = await _context.IngressoLotes.AsNoTracking().Where(x => ids.Contains(x.PartidaId)).ToListAsync();
        var loteIds = lotes.Select(x => x.Id).ToHashSet();
        var query = _context.Ingressos.AsNoTracking().Where(x => loteIds.Contains(x.IngressoLoteId));
        if (filtro.Inicio.HasValue) query = query.Where(x => x.DataCompra >= filtro.Inicio.Value.Date);
        if (filtro.Fim.HasValue) query = query.Where(x => x.DataCompra < filtro.Fim.Value.Date.AddDays(1));
        var ingressos = await query.ToListAsync();
        return (ingressos, lotes);
    }

    private static ResumoFinanceiroDTO CriarResumo(IEnumerable<Ingresso> ingressos)
    {
        var lista = ingressos.ToList();
        var pagos = lista.Where(x => x.StatusIngresso is EnumStatusIngresso.Pago or EnumStatusIngresso.Utilizado).ToList();
        var checkIns = lista.Count(x => x.StatusIngresso == EnumStatusIngresso.Utilizado);
        return new ResumoFinanceiroDTO
        {
            ReceitaConfirmada = pagos.Sum(x => x.PrecoPago),
            IngressosPagos = pagos.Count,
            IngressosPendentes = lista.Count(x => x.StatusIngresso == EnumStatusIngresso.Pendente),
            IngressosCancelados = lista.Count(x => x.StatusIngresso == EnumStatusIngresso.Cancelado),
            CheckIns = checkIns,
            TaxaComparecimento = pagos.Count == 0 ? 0 : Math.Round(checkIns * 100m / pagos.Count, 2)
        };
    }

    private static List<SerieVendasDTO> CriarSerieVendas(IEnumerable<Ingresso> ingressos) => ingressos
        .Where(x => x.StatusIngresso is EnumStatusIngresso.Pago or EnumStatusIngresso.Utilizado)
        .GroupBy(x => x.DataCompra.Date).OrderBy(x => x.Key)
        .Select(x => new SerieVendasDTO { Data = x.Key, Quantidade = x.Count(), Receita = x.Sum(y => y.PrecoPago) }).ToList();

    private static List<VendasPorPartidaDTO> CriarVendasPorPartida(IEnumerable<Ingresso> ingressos, IEnumerable<IngressoLote> lotes, IEnumerable<Partida> partidas, IReadOnlyDictionary<Guid, string> times)
    {
        var lotesPorId = lotes.ToDictionary(x => x.Id);
        var partidasPorId = partidas.ToDictionary(x => x.Id);
        return ingressos.GroupBy(x => lotesPorId[x.IngressoLoteId].PartidaId).Select(g =>
        {
            var partida = partidasPorId[g.Key];
            var casa = partida.TimeCasaId.HasValue && times.TryGetValue(partida.TimeCasaId.Value, out var nomeCasa) ? nomeCasa : "Time casa";
            var visitante = partida.TimeVisitanteId.HasValue && times.TryGetValue(partida.TimeVisitanteId.Value, out var nomeVisitante) ? nomeVisitante : "Time visitante";
            var confirmados = g.Where(x => x.StatusIngresso is EnumStatusIngresso.Pago or EnumStatusIngresso.Utilizado).ToList();
            return new VendasPorPartidaDTO { PartidaId = partida.Id, Confronto = $"{casa} x {visitante}", IngressosVendidos = confirmados.Count, CheckIns = g.Count(x => x.StatusIngresso == EnumStatusIngresso.Utilizado), Receita = confirmados.Sum(x => x.PrecoPago) };
        }).OrderByDescending(x => x.Receita).ToList();
    }

    private static List<OcupacaoLoteDTO> CriarOcupacao(IEnumerable<IngressoLote> lotes) => lotes.Select(lote =>
    {
        // A disponibilidade é o estoque operacional atual, portanto não deve mudar ao filtrar vendas por período.
        var vendidos = Math.Max(0, lote.QuantidadeTotal - lote.QuantidadeDisponivel);
        return new OcupacaoLoteDTO { LoteId = lote.Id, Lote = lote.NomeLote, Total = lote.QuantidadeTotal, Vendidos = vendidos, Disponiveis = lote.QuantidadeDisponivel, OcupacaoPercentual = lote.QuantidadeTotal == 0 ? 0 : Math.Round(vendidos * 100m / lote.QuantidadeTotal, 2) };
    }).OrderByDescending(x => x.OcupacaoPercentual).ToList();

    private static DesempenhoTimeDTO CriarDesempenho(Time time, IEnumerable<Partida> partidas, IEnumerable<Favorito> favoritos, FiltroRelatorioDTO filtro)
    {
        var jogos = partidas.Where(x => x.Finalizado && NoPeriodo(x.DataHora, filtro) && (x.TimeCasaId == time.Id || x.TimeVisitanteId == time.Id)).ToList();
        var dto = new DesempenhoTimeDTO { TimeId = time.Id, Nome = time.Nome, Jogos = jogos.Count, Favoritos = favoritos.Count(x => x.ItemId == time.Id) };
        foreach (var jogo in jogos)
        {
            var ehCasa = jogo.TimeCasaId == time.Id;
            var pro = ehCasa ? jogo.GolsTimeCasa : jogo.GolsTimeVisitante;
            var contra = ehCasa ? jogo.GolsTimeVisitante : jogo.GolsTimeCasa;
            dto.GolsPro += pro; dto.GolsContra += contra;
            if (pro > contra) dto.Vitorias++; else if (pro == contra) dto.Empates++; else dto.Derrotas++;
        }
        return dto;
    }

    private static string NomeTime(Guid? timeId, IReadOnlyDictionary<Guid, string> nomes, string padrao) =>
        timeId.HasValue && nomes.TryGetValue(timeId.Value, out var nome) ? nome : padrao;

    private static bool NoPeriodo(DateTime? data, FiltroRelatorioDTO filtro) => data.HasValue && NoPeriodo(data.Value, filtro);
    private static bool NoPeriodo(DateTime data, FiltroRelatorioDTO filtro) => (!filtro.Inicio.HasValue || data >= filtro.Inicio.Value.Date) && (!filtro.Fim.HasValue || data < filtro.Fim.Value.Date.AddDays(1));
    private static void ValidarPeriodo(FiltroRelatorioDTO filtro)
    {
        if (filtro.Inicio.HasValue && filtro.Fim.HasValue && filtro.Fim < filtro.Inicio)
            throw new ArgumentException("A data final deve ser posterior ou igual à data inicial.");
    }
}
