using kivoBackend.Application.DTO;
using kivoBackend.Application.Interfaces;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using kivoBackend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace kivoBackend.Application.Services;

/// <summary>
/// Gera dados já agregados para os dashboards, sem expor compradores ou CPFs.
///
/// Regras de período (decisão de produto): Inicio/Fim são dias inclusivos em America/Sao_Paulo.
/// Campos gravados em UTC (DataCompra, DataPagamento) são comparados com os limites convertidos para UTC;
/// campos gravados em hora local (Partida.DataHora, Usuario.CriadoEm) são comparados direto com o dia local.
/// </summary>
public class RelatorioService : IRelatorioService
{
    private static readonly TimeZoneInfo FusoRelatorio = ObterFusoSaoPaulo();

    private readonly AppDbContext _context;
    private readonly TimeProvider _relogio;

    public RelatorioService(AppDbContext context, TimeProvider? relogio = null)
    {
        _context = context;
        _relogio = relogio ?? TimeProvider.System;
    }

    /// <summary>"Agora" em hora local de São Paulo, comparável com Partida.DataHora.</summary>
    private DateTime AgoraLocal => TimeZoneInfo.ConvertTimeFromUtc(_relogio.GetUtcNow().UtcDateTime, FusoRelatorio);

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
        var periodo = CriarPeriodo(filtro);
        var agora = AgoraLocal;
        var financeiro = await CarregarFinanceiroAsync(partidas, periodo, agora);

        return new RelatorioOrganizadorCampeonatoDTO
        {
            Inicio = filtro.Inicio,
            Fim = filtro.Fim,
            Resumo = CriarResumo(financeiro, periodo),
            Campeonatos = campeonatos.Count,
            TimesParticipantes = participacoes.Select(x => x.TimeId).Distinct().Count(),
            PartidasRealizadas = partidas.Count(x => x.Finalizado && NoPeriodo(x.DataHora, periodo)),
            PartidasAgendadas = partidas.Count(x => !x.Finalizado && x.DataHora.HasValue && x.DataHora >= agora && NoPeriodo(x.DataHora, periodo)),
            VendasPorDia = CriarSerieVendas(financeiro.ConfirmadosComData, x => x.DataPagamento!.Value),
            VendasPorPartida = CriarVendasPorPartida(financeiro.ConfirmadosNoRecorte(periodo), financeiro.Lotes, partidas, times),
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

        var periodo = CriarPeriodo(filtro);
        var agora = AgoraLocal;

        return new RelatorioOrganizadorTimeDTO
        {
            Inicio = filtro.Inicio,
            Fim = filtro.Fim,
            Times = times.Count,
            CampeonatosDisputados = participacoes.Select(x => x.CampeonatoId).Distinct().Count(),
            JogosRealizados = partidas.Count(x => x.Finalizado && NoPeriodo(x.DataHora, periodo)),
            JogosAgendados = partidas.Count(x => !x.Finalizado && x.DataHora.HasValue && x.DataHora >= agora && NoPeriodo(x.DataHora, periodo)),
            DesempenhoPorTime = times.Select(time => CriarDesempenho(time, partidas, favoritos, periodo)).ToList()
        };
    }

    public async Task<RelatorioAdministrativoDTO> ObterAdministrativoAsync(FiltroRelatorioAdministrativoDTO filtro)
    {
        ValidarPeriodo(filtro);
        // Usuários não pertencem a um esporte, então a base de usuários é sempre global.
        var usuarios = await _context.Usuarios.AsNoTracking().ToListAsync();

        var campeonatosQuery = _context.Campeonatos.AsNoTracking().Include(x => x.Esporte).AsQueryable();
        var timesQuery = _context.Times.AsNoTracking().AsQueryable();
        if (filtro.EsporteId.HasValue)
        {
            campeonatosQuery = campeonatosQuery.Where(x => x.EsporteId == filtro.EsporteId.Value);
            timesQuery = timesQuery.Where(x => x.EsporteId == filtro.EsporteId.Value);
        }

        var campeonatos = await campeonatosQuery.ToListAsync();
        var campeonatoIds = campeonatos.Select(x => x.Id).ToHashSet();
        var partidas = await _context.Partidas.AsNoTracking().Where(x => campeonatoIds.Contains(x.CampeonatoId)).ToListAsync();
        var times = await timesQuery.ToListAsync();
        var periodo = CriarPeriodo(filtro);
        var financeiro = await CarregarFinanceiroAsync(partidas, periodo, AgoraLocal);

        return new RelatorioAdministrativoDTO
        {
            Inicio = filtro.Inicio,
            Fim = filtro.Fim,
            EsporteId = filtro.EsporteId,
            UsuariosTotais = usuarios.Count,
            // CriadoEm é gravado em hora local (DateTime.Now), por isso compara direto com o dia local.
            NovosUsuarios = usuarios.Count(x => NoPeriodo(x.CriadoEm, periodo)),
            // Situação atual (não depende do período): status/ativo não têm histórico de datas.
            CampeonatosTotais = campeonatos.Count,
            CampeonatosEmAndamento = campeonatos.Count(x => x.EnumStatusCampeonato == EnumStatusCampeonato.EmAndamento),
            PartidasRealizadas = partidas.Count(x => x.Finalizado && NoPeriodo(x.DataHora, periodo)),
            TimesAtivos = times.Count(x => x.Ativo),
            ResumoFinanceiro = CriarResumo(financeiro, periodo),
            VendasPorDia = CriarSerieVendas(financeiro.ConfirmadosComData, x => x.DataPagamento!.Value),
            UsuariosPorPerfil = usuarios.GroupBy(x => x.EnumCargo.ToString()).Select(x => new ItemQuantidadeDTO { Nome = x.Key, Quantidade = x.Count() }).OrderBy(x => x.Nome).ToList(),
            CampeonatosPorEsporte = campeonatos.GroupBy(x => x.Esporte?.Nome ?? "Sem esporte").Select(x => new ItemQuantidadeDTO { Nome = x.Key, Quantidade = x.Count() }).OrderByDescending(x => x.Quantidade).ToList()
        };
    }

    public async Task<RelatorioTorcedorDTO> ObterTorcedorAsync(Guid usuarioId, FiltroRelatorioDTO filtro)
    {
        ValidarPeriodo(filtro);
        var torcedorExiste = await _context.Torcedores.AsNoTracking().AnyAsync(x => x.UsuarioId == usuarioId);
        if (!torcedorExiste) throw new UnauthorizedAccessException("Perfil de torcedor não encontrado.");

        // Histórico de compras do torcedor: continua pela data da compra; só os limites passam a ser em São Paulo.
        var periodo = CriarPeriodo(filtro);
        var query = _context.Ingressos.AsNoTracking().Where(x => x.UsuarioId == usuarioId);
        if (periodo.InicioUtc.HasValue) query = query.Where(x => x.DataCompra >= periodo.InicioUtc.Value);
        if (periodo.FimUtcExclusivo.HasValue) query = query.Where(x => x.DataCompra < periodo.FimUtcExclusivo.Value);
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
            ComprasPorDia = CriarSerieVendas(confirmados, x => x.DataCompra),
            ProximasPartidas = partidas.Where(x => x.DataHora.HasValue && x.DataHora >= AgoraLocal && !x.Finalizado)
                .OrderBy(x => x.DataHora).Select(x => new ProximaPartidaDTO
                {
                    PartidaId = x.Id,
                    Confronto = $"{NomeTime(x.TimeCasaId, nomesTimes, "Time casa")} x {NomeTime(x.TimeVisitanteId, nomesTimes, "Time visitante")}",
                    DataHora = x.DataHora,
                    Local = x.Local
                }).ToList()
        };
    }

    /// <summary>
    /// Recortes financeiros de um conjunto de partidas:
    /// - ConfirmadosComData: pagos/utilizados com DataPagamento no período (ou todos com data, sem filtro);
    /// - SemDataPagamento: pagos/utilizados anteriores à coluna DataPagamento — nunca recebem uma data;
    /// - Emitidos: ingressos com DataCompra no período, em qualquer situação;
    /// - Elegiveis: pagos/utilizados de partidas elegíveis para check-in (finalizadas ou já iniciadas) com data no período.
    /// </summary>
    private sealed record DadosFinanceiros(
        List<IngressoLote> Lotes,
        List<Ingresso> ConfirmadosComData,
        List<Ingresso> SemDataPagamento,
        List<Ingresso> Emitidos,
        List<Ingresso> Elegiveis)
    {
        /// <summary>Sem filtro de data, a receita inclui os confirmados sem data; com filtro, eles ficam de fora.</summary>
        public List<Ingresso> ConfirmadosNoRecorte(Periodo periodo) =>
            periodo.Limitado ? ConfirmadosComData : ConfirmadosComData.Concat(SemDataPagamento).ToList();
    }

    private async Task<DadosFinanceiros> CarregarFinanceiroAsync(IReadOnlyCollection<Partida> partidas, Periodo periodo, DateTime agoraLocal)
    {
        var partidaIds = partidas.Select(x => x.Id).ToHashSet();
        var lotes = await _context.IngressoLotes.AsNoTracking().Where(x => partidaIds.Contains(x.PartidaId)).ToListAsync();
        var loteIds = lotes.Select(x => x.Id).ToHashSet();
        var doRecorte = _context.Ingressos.AsNoTracking().Where(x => loteIds.Contains(x.IngressoLoteId));
        var confirmados = doRecorte.Where(x => x.StatusIngresso == EnumStatusIngresso.Pago || x.StatusIngresso == EnumStatusIngresso.Utilizado);

        var comDataQuery = confirmados.Where(x => x.DataPagamento != null);
        if (periodo.InicioUtc.HasValue) comDataQuery = comDataQuery.Where(x => x.DataPagamento >= periodo.InicioUtc.Value);
        if (periodo.FimUtcExclusivo.HasValue) comDataQuery = comDataQuery.Where(x => x.DataPagamento < periodo.FimUtcExclusivo.Value);

        var emitidosQuery = doRecorte;
        if (periodo.InicioUtc.HasValue) emitidosQuery = emitidosQuery.Where(x => x.DataCompra >= periodo.InicioUtc.Value);
        if (periodo.FimUtcExclusivo.HasValue) emitidosQuery = emitidosQuery.Where(x => x.DataCompra < periodo.FimUtcExclusivo.Value);

        var partidasElegiveis = partidas
            .Where(x => (x.Finalizado || x.DataHora <= agoraLocal) && NoPeriodo(x.DataHora, periodo))
            .Select(x => x.Id).ToHashSet();
        var lotesElegiveis = lotes.Where(x => partidasElegiveis.Contains(x.PartidaId)).Select(x => x.Id).ToHashSet();

        return new DadosFinanceiros(
            lotes,
            await comDataQuery.ToListAsync(),
            await confirmados.Where(x => x.DataPagamento == null).ToListAsync(),
            await emitidosQuery.ToListAsync(),
            await confirmados.Where(x => lotesElegiveis.Contains(x.IngressoLoteId)).ToListAsync());
    }

    private static bool Confirmado(Ingresso ingresso) =>
        ingresso.StatusIngresso is EnumStatusIngresso.Pago or EnumStatusIngresso.Utilizado;

    private static ResumoFinanceiroDTO CriarResumo(DadosFinanceiros dados, Periodo periodo)
    {
        var confirmados = dados.ConfirmadosNoRecorte(periodo);
        var checkIns = dados.Elegiveis.Count(x => x.StatusIngresso == EnumStatusIngresso.Utilizado);
        return new ResumoFinanceiroDTO
        {
            ReceitaConfirmada = confirmados.Sum(x => x.PrecoPago),
            IngressosPagos = confirmados.Count,
            ReceitaSemDataPagamento = dados.SemDataPagamento.Sum(x => x.PrecoPago),
            IngressosSemDataPagamento = dados.SemDataPagamento.Count,
            IngressosEmitidos = dados.Emitidos.Count,
            IngressosEmitidosConfirmados = dados.Emitidos.Count(Confirmado),
            IngressosPendentes = dados.Emitidos.Count(x => x.StatusIngresso == EnumStatusIngresso.Pendente),
            IngressosCancelados = dados.Emitidos.Count(x => x.StatusIngresso == EnumStatusIngresso.Cancelado),
            CheckIns = checkIns,
            IngressosElegiveisCheckIn = dados.Elegiveis.Count,
            TaxaComparecimento = dados.Elegiveis.Count == 0 ? 0 : Math.Round(checkIns * 100m / dados.Elegiveis.Count, 2)
        };
    }

    /// <summary>Agrupa por dia de São Paulo a data UTC escolhida (pagamento para receita, compra para histórico do torcedor).</summary>
    private static List<SerieVendasDTO> CriarSerieVendas(IEnumerable<Ingresso> ingressos, Func<Ingresso, DateTime> dataUtc) => ingressos
        .Where(Confirmado)
        .GroupBy(x => ParaLocal(dataUtc(x)).Date).OrderBy(x => x.Key)
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
            // `ingressos` já são só os confirmados do recorte (pela data do pagamento).
            return new VendasPorPartidaDTO { PartidaId = partida.Id, Confronto = $"{casa} x {visitante}", IngressosVendidos = g.Count(), CheckIns = g.Count(x => x.StatusIngresso == EnumStatusIngresso.Utilizado), Receita = g.Sum(x => x.PrecoPago) };
        }).OrderByDescending(x => x.Receita).ToList();
    }

    private static List<OcupacaoLoteDTO> CriarOcupacao(IEnumerable<IngressoLote> lotes) => lotes.Select(lote =>
    {
        // A disponibilidade é o estoque operacional atual, portanto não deve mudar ao filtrar vendas por período.
        var vendidos = Math.Max(0, lote.QuantidadeTotal - lote.QuantidadeDisponivel);
        return new OcupacaoLoteDTO { LoteId = lote.Id, Lote = lote.NomeLote, Total = lote.QuantidadeTotal, Vendidos = vendidos, Disponiveis = lote.QuantidadeDisponivel, OcupacaoPercentual = lote.QuantidadeTotal == 0 ? 0 : Math.Round(vendidos * 100m / lote.QuantidadeTotal, 2) };
    }).OrderByDescending(x => x.OcupacaoPercentual).ToList();

    private static DesempenhoTimeDTO CriarDesempenho(Time time, IEnumerable<Partida> partidas, IEnumerable<Favorito> favoritos, Periodo periodo)
    {
        var jogos = partidas.Where(x => x.Finalizado && NoPeriodo(x.DataHora, periodo) && (x.TimeCasaId == time.Id || x.TimeVisitanteId == time.Id)).ToList();
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

    /// <summary>Período em dias de São Paulo, nos dois referenciais usados pelo banco.</summary>
    private readonly record struct Periodo(DateTime? InicioLocal, DateTime? FimLocalExclusivo, DateTime? InicioUtc, DateTime? FimUtcExclusivo)
    {
        public bool Limitado => InicioLocal.HasValue || FimLocalExclusivo.HasValue;
    }

    private static Periodo CriarPeriodo(FiltroRelatorioDTO filtro)
    {
        DateTime? inicio = filtro.Inicio.HasValue ? DateTime.SpecifyKind(filtro.Inicio.Value.Date, DateTimeKind.Unspecified) : null;
        DateTime? fimExclusivo = filtro.Fim.HasValue ? DateTime.SpecifyKind(filtro.Fim.Value.Date.AddDays(1), DateTimeKind.Unspecified) : null;
        return new Periodo(
            inicio,
            fimExclusivo,
            inicio.HasValue ? TimeZoneInfo.ConvertTimeToUtc(inicio.Value, FusoRelatorio) : null,
            fimExclusivo.HasValue ? TimeZoneInfo.ConvertTimeToUtc(fimExclusivo.Value, FusoRelatorio) : null);
    }

    private static DateTime ParaLocal(DateTime dataUtc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(dataUtc, DateTimeKind.Utc), FusoRelatorio);

    /// <summary>Para campos gravados em hora local de São Paulo (Partida.DataHora, Usuario.CriadoEm).</summary>
    private static bool NoPeriodo(DateTime? dataLocal, Periodo periodo) => dataLocal.HasValue && NoPeriodo(dataLocal.Value, periodo);
    private static bool NoPeriodo(DateTime dataLocal, Periodo periodo) =>
        (!periodo.InicioLocal.HasValue || dataLocal >= periodo.InicioLocal.Value) &&
        (!periodo.FimLocalExclusivo.HasValue || dataLocal < periodo.FimLocalExclusivo.Value);

    private static TimeZoneInfo ObterFusoSaoPaulo()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            // Imagens sem tzdata: o Brasil não tem horário de verão desde 2019, então UTC-3 fixo é equivalente.
            return TimeZoneInfo.CreateCustomTimeZone("America/Sao_Paulo", TimeSpan.FromHours(-3), "America/Sao_Paulo", "America/Sao_Paulo");
        }
    }
    private static void ValidarPeriodo(FiltroRelatorioDTO filtro)
    {
        if (filtro.Inicio.HasValue && filtro.Fim.HasValue && filtro.Fim < filtro.Inicio)
            throw new ArgumentException("A data final deve ser posterior ou igual à data inicial.");
    }
}
