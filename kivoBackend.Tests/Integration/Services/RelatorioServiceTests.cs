using kivoBackend.Application.DTO;
using kivoBackend.Application.Services;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using kivoBackend.Infrastructure.Data;
using kivoBackend.Tests.TestSupport;

namespace kivoBackend.Tests.Integration.Services;

/// <summary>
/// RelatorioService consulta o AppDbContext diretamente (agregações para dashboard),
/// por isso roda contra SQLite em memória em vez dos fakes de repositório.
///
/// Regras validadas aqui (decididas com o produto):
/// - Receita confirmada é atribuída à data do pagamento (DataPagamento, UTC), não à da compra.
/// - Período: dias inclusivos em America/Sao_Paulo; série diária agrupada nesse fuso.
/// - Ingressos confirmados sem DataPagamento (históricos) não ganham data: ficam fora de
///   períodos e entram no total apenas sem filtro de data.
/// - Comparecimento = check-ins ÷ confirmados de partidas elegíveis (finalizadas ou já iniciadas).
/// </summary>
public class RelatorioServiceTests
{
    /// <summary>05/10/2026 12:00 em São Paulo (15:00 UTC).</summary>
    private static readonly DateTimeOffset Agora = new(2026, 10, 5, 15, 0, 0, TimeSpan.Zero);

    private sealed class RelogioFixo(DateTimeOffset agora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => agora;
    }

    private static RelatorioService NovoServico(AppDbContext context) => new(context, new RelogioFixo(Agora));

    private static DateTime Utc(int mes, int dia, int hora = 12, int minuto = 0, int segundo = 0) =>
        new(2026, mes, dia, hora, minuto, segundo, DateTimeKind.Utc);

    /// <summary>Monta esportes, organizadores, partidas, lotes e ingressos com datas controladas.</summary>
    private sealed class Cenario
    {
        private readonly AppDbContext _context;
        public Esporte Futebol { get; } = new() { Id = Guid.NewGuid(), Nome = "Futebol", Icone = "mdi:soccer", Ativo = true };
        public Esporte Volei { get; } = new() { Id = Guid.NewGuid(), Nome = "Vôlei", Icone = "mdi:volleyball", Ativo = true };
        public Usuario Torcedor { get; }
        public Usuario UsuarioOrgCamp { get; }
        private readonly OrganizadorCampeonato _orgCamp;
        private readonly OrganizadorTime _orgTime;

        public Cenario(AppDbContext context)
        {
            _context = context;
            var enderecoCamp = new Endereco { Id = Guid.NewGuid(), Cep = "01000-000", Rua = "Rua A", Numero = "1", Cidade = "SP", Estado = "SP", Pais = "Brasil" };
            var enderecoTime = new Endereco { Id = Guid.NewGuid(), Cep = "02000-000", Rua = "Rua B", Numero = "2", Cidade = "SP", Estado = "SP", Pais = "Brasil" };
            UsuarioOrgCamp = new Usuario { Id = Guid.NewGuid(), Nome = "Org Camp", Email = "camp@test.com", Cpf = "11111111111", Telefone = "119", DataNascimento = DateTime.Today, EnumCargo = EnumCargo.OrganizadorCampeonato, Ativo = true, CriadoEm = DateTime.UtcNow };
            var usuarioTime = new Usuario { Id = Guid.NewGuid(), Nome = "Org Time", Email = "time@test.com", Cpf = "22222222222", Telefone = "119", DataNascimento = DateTime.Today, EnumCargo = EnumCargo.OrganizadorTime, Ativo = true, CriadoEm = DateTime.UtcNow };
            Torcedor = new Usuario { Id = Guid.NewGuid(), Nome = "Torcedor", Email = "torcedor@test.com", Cpf = "33333333333", Telefone = "119", DataNascimento = DateTime.Today, EnumCargo = EnumCargo.Torcedor, Ativo = true, CriadoEm = DateTime.UtcNow };
            _orgCamp = new OrganizadorCampeonato { Id = Guid.NewGuid(), UsuarioId = UsuarioOrgCamp.Id, EnderecoId = enderecoCamp.Id };
            _orgTime = new OrganizadorTime { Id = Guid.NewGuid(), UsuarioId = usuarioTime.Id, EnderecoId = enderecoTime.Id };
            context.AddRange(Futebol, Volei, enderecoCamp, enderecoTime, UsuarioOrgCamp, usuarioTime, Torcedor, _orgCamp, _orgTime);
        }

        /// <summary>Partida com DataHora em hora local de São Paulo (como o front grava) e um lote.</summary>
        public IngressoLote Partida(Esporte esporte, DateTime dataHoraLocal, bool finalizado)
        {
            var time = new Time { Id = Guid.NewGuid(), Nome = $"Time {esporte.Nome} {Guid.NewGuid():N}"[..20], Cidade = "SP", Estado = "SP", EsporteId = esporte.Id, OrganizadorTimeId = _orgTime.Id, Ativo = true };
            var campeonato = new Campeonato
            {
                Id = Guid.NewGuid(), Nome = $"Copa {esporte.Nome}", OrganizadorCampeonatoId = _orgCamp.Id, EsporteId = esporte.Id,
                DataInicio = DateTime.Today, DataFim = DateTime.Today.AddDays(10), EnumStatusCampeonato = EnumStatusCampeonato.EmAndamento
            };
            var partida = new Partida { Id = Guid.NewGuid(), CampeonatoId = campeonato.Id, TimeCasaId = time.Id, DataHora = dataHoraLocal, Finalizado = finalizado, Local = "Arena" };
            var lote = new IngressoLote { Id = Guid.NewGuid(), PartidaId = partida.Id, NomeLote = "Inteira", Preco = 50, QuantidadeTotal = 100, QuantidadeDisponivel = 100, Ativo = true };
            _context.AddRange(time, campeonato, partida, lote);
            return lote;
        }

        public void Ingresso(IngressoLote lote, decimal preco, EnumStatusIngresso status, DateTime dataCompraUtc, DateTime? dataPagamentoUtc)
        {
            _context.Add(new Ingresso
            {
                Id = Guid.NewGuid(), IngressoLoteId = lote.Id, UsuarioId = Torcedor.Id, PrecoPago = preco,
                StatusIngresso = status, DataCompra = dataCompraUtc, DataPagamento = dataPagamentoUtc
            });
        }

        public Task SalvarAsync() => _context.SaveChangesAsync();
    }

    private static FiltroRelatorioAdministrativoDTO Periodo(DateTime? inicio, DateTime? fim, Guid? esporteId = null) =>
        new() { Inicio = inicio, Fim = fim, EsporteId = esporteId };

    /// <summary>Futebol: 2 ingressos pagos de 50. Vôlei: 1 ingresso pago de 30. Todos com DataPagamento em 01/10.</summary>
    private static async Task<Cenario> SemearDuasModalidadesAsync(AppDbContext context)
    {
        var cenario = new Cenario(context);
        var loteFutebol = cenario.Partida(cenario.Futebol, new DateTime(2026, 10, 4, 16, 0, 0), finalizado: true);
        var loteVolei = cenario.Partida(cenario.Volei, new DateTime(2026, 10, 4, 18, 0, 0), finalizado: true);
        cenario.Ingresso(loteFutebol, 50, EnumStatusIngresso.Pago, Utc(10, 1), Utc(10, 1));
        cenario.Ingresso(loteFutebol, 50, EnumStatusIngresso.Pago, Utc(10, 1), Utc(10, 1));
        cenario.Ingresso(loteVolei, 30, EnumStatusIngresso.Pago, Utc(10, 1), Utc(10, 1));
        await cenario.SalvarAsync();
        return cenario;
    }

    // ─── Filtro por esporte ───────────────────────────────────────────────────

    [Fact]
    public async Task ObterAdministrativo_SemEsporte_ConsideraTodasAsModalidades()
    {
        using var db = SqliteDbContextFactory.Create();
        await using var context = db.NewContext();
        await SemearDuasModalidadesAsync(context);

        var relatorio = await NovoServico(context).ObterAdministrativoAsync(new FiltroRelatorioAdministrativoDTO());

        Assert.Null(relatorio.EsporteId);
        Assert.Equal(2, relatorio.CampeonatosTotais);
        Assert.Equal(2, relatorio.CampeonatosEmAndamento);
        Assert.Equal(2, relatorio.PartidasRealizadas);
        Assert.Equal(2, relatorio.TimesAtivos);
        Assert.Equal(130m, relatorio.ResumoFinanceiro.ReceitaConfirmada);
        Assert.Equal(3, relatorio.ResumoFinanceiro.IngressosPagos);
        Assert.Equal(2, relatorio.CampeonatosPorEsporte.Count);
    }

    [Fact]
    public async Task ObterAdministrativo_ComEsporte_FiltraCampeonatosPartidasTimesEVendas()
    {
        using var db = SqliteDbContextFactory.Create();
        await using var context = db.NewContext();
        var cenario = await SemearDuasModalidadesAsync(context);

        var relatorio = await NovoServico(context).ObterAdministrativoAsync(Periodo(null, null, cenario.Volei.Id));

        Assert.Equal(cenario.Volei.Id, relatorio.EsporteId);
        Assert.Equal(1, relatorio.CampeonatosTotais);
        Assert.Equal(1, relatorio.PartidasRealizadas);
        Assert.Equal(1, relatorio.TimesAtivos);
        Assert.Equal(30m, relatorio.ResumoFinanceiro.ReceitaConfirmada);
        var serie = Assert.Single(relatorio.VendasPorDia);
        Assert.Equal(30m, serie.Receita);
        Assert.Equal("Vôlei", Assert.Single(relatorio.CampeonatosPorEsporte).Nome);
    }

    [Fact]
    public async Task ObterAdministrativo_ComEsporte_NaoFiltraMetricasDeUsuarios()
    {
        using var db = SqliteDbContextFactory.Create();
        await using var context = db.NewContext();
        var cenario = await SemearDuasModalidadesAsync(context);

        var relatorio = await NovoServico(context).ObterAdministrativoAsync(Periodo(null, null, cenario.Volei.Id));

        // Usuário não pertence a um esporte: a base de usuários é sempre global.
        Assert.Equal(3, relatorio.UsuariosTotais);
        Assert.Equal(3, relatorio.UsuariosPorPerfil.Sum(x => x.Quantidade));
    }

    [Fact]
    public async Task ObterAdministrativo_EsporteSemDados_RetornaZerado()
    {
        using var db = SqliteDbContextFactory.Create();
        await using var context = db.NewContext();
        await SemearDuasModalidadesAsync(context);

        var relatorio = await NovoServico(context).ObterAdministrativoAsync(Periodo(null, null, Guid.NewGuid()));

        Assert.Equal(0, relatorio.CampeonatosTotais);
        Assert.Equal(0, relatorio.TimesAtivos);
        Assert.Equal(0m, relatorio.ResumoFinanceiro.ReceitaConfirmada);
        Assert.Empty(relatorio.VendasPorDia);
    }

    [Fact]
    public async Task ObterAdministrativo_EsporteEPeriodoCombinados_AplicaOsDoisRecortes()
    {
        using var db = SqliteDbContextFactory.Create();
        await using var context = db.NewContext();
        var cenario = new Cenario(context);
        var loteFutebol = cenario.Partida(cenario.Futebol, new DateTime(2026, 9, 20, 16, 0, 0), finalizado: true);
        var loteVolei = cenario.Partida(cenario.Volei, new DateTime(2026, 9, 20, 18, 0, 0), finalizado: true);
        cenario.Ingresso(loteVolei, 30, EnumStatusIngresso.Pago, Utc(9, 10), Utc(9, 10));   // vôlei, no período
        cenario.Ingresso(loteVolei, 99, EnumStatusIngresso.Pago, Utc(9, 1), Utc(9, 1));     // vôlei, fora do período
        cenario.Ingresso(loteFutebol, 50, EnumStatusIngresso.Pago, Utc(9, 10), Utc(9, 10)); // futebol, no período
        await cenario.SalvarAsync();

        var relatorio = await NovoServico(context).ObterAdministrativoAsync(
            Periodo(new DateTime(2026, 9, 5), new DateTime(2026, 9, 15), cenario.Volei.Id));

        Assert.Equal(30m, relatorio.ResumoFinanceiro.ReceitaConfirmada);
        Assert.Equal(1, relatorio.ResumoFinanceiro.IngressosPagos);
        Assert.Equal(1, relatorio.ResumoFinanceiro.IngressosEmitidos);
        Assert.Equal(new DateTime(2026, 9, 10), Assert.Single(relatorio.VendasPorDia).Data);
    }

    [Fact]
    public async Task ObterAdministrativo_FimAntesDoInicio_Lanca()
    {
        using var db = SqliteDbContextFactory.Create();
        await using var context = db.NewContext();

        await Assert.ThrowsAsync<ArgumentException>(() => NovoServico(context).ObterAdministrativoAsync(
            Periodo(new DateTime(2026, 10, 5), new DateTime(2026, 10, 4))));
    }

    // ─── Receita confirmada: data do pagamento ────────────────────────────────

    [Fact]
    public async Task ObterAdministrativo_ConfirmacaoPosterior_ReceitaContaNoDiaDoPagamento()
    {
        using var db = SqliteDbContextFactory.Create();
        await using var context = db.NewContext();
        var cenario = new Cenario(context);
        var lote = cenario.Partida(cenario.Futebol, new DateTime(2026, 9, 20, 16, 0, 0), finalizado: true);
        cenario.Ingresso(lote, 50, EnumStatusIngresso.Pago, dataCompraUtc: Utc(9, 10), dataPagamentoUtc: Utc(9, 15));
        await cenario.SalvarAsync();
        var servico = NovoServico(context);

        var diaDaCompra = await servico.ObterAdministrativoAsync(Periodo(new DateTime(2026, 9, 10), new DateTime(2026, 9, 10)));
        var diaDoPagamento = await servico.ObterAdministrativoAsync(Periodo(new DateTime(2026, 9, 15), new DateTime(2026, 9, 15)));

        Assert.Equal(0m, diaDaCompra.ResumoFinanceiro.ReceitaConfirmada);
        Assert.Empty(diaDaCompra.VendasPorDia);
        // A emissão continua no dia da compra (situação dos ingressos emitidos no período).
        Assert.Equal(1, diaDaCompra.ResumoFinanceiro.IngressosEmitidos);
        Assert.Equal(1, diaDaCompra.ResumoFinanceiro.IngressosEmitidosConfirmados);

        Assert.Equal(50m, diaDoPagamento.ResumoFinanceiro.ReceitaConfirmada);
        Assert.Equal(new DateTime(2026, 9, 15), Assert.Single(diaDoPagamento.VendasPorDia).Data);
        Assert.Equal(0, diaDoPagamento.ResumoFinanceiro.IngressosEmitidos);
    }

    [Fact]
    public async Task ObterAdministrativo_FronteiraDoDia_UsaAmericaSaoPaulo()
    {
        using var db = SqliteDbContextFactory.Create();
        await using var context = db.NewContext();
        var cenario = new Cenario(context);
        var lote = cenario.Partida(cenario.Futebol, new DateTime(2026, 9, 20, 16, 0, 0), finalizado: true);
        // 15/09 23:59:59 em São Paulo = 16/09 02:59:59 UTC.
        cenario.Ingresso(lote, 10, EnumStatusIngresso.Pago, Utc(9, 16, 2, 59, 59), Utc(9, 16, 2, 59, 59));
        // 16/09 00:00:00 em São Paulo = 16/09 03:00:00 UTC.
        cenario.Ingresso(lote, 20, EnumStatusIngresso.Pago, Utc(9, 16, 3), Utc(9, 16, 3));
        await cenario.SalvarAsync();
        var servico = NovoServico(context);

        var dia15 = await servico.ObterAdministrativoAsync(Periodo(new DateTime(2026, 9, 15), new DateTime(2026, 9, 15)));
        var dia16 = await servico.ObterAdministrativoAsync(Periodo(new DateTime(2026, 9, 16), new DateTime(2026, 9, 16)));
        var tudo = await servico.ObterAdministrativoAsync(new FiltroRelatorioAdministrativoDTO());

        Assert.Equal(10m, dia15.ResumoFinanceiro.ReceitaConfirmada);
        Assert.Equal(1, dia15.ResumoFinanceiro.IngressosEmitidos);
        Assert.Equal(20m, dia16.ResumoFinanceiro.ReceitaConfirmada);
        Assert.Equal(1, dia16.ResumoFinanceiro.IngressosEmitidos);
        Assert.Equal(
            new[] { (new DateTime(2026, 9, 15), 10m), (new DateTime(2026, 9, 16), 20m) },
            tudo.VendasPorDia.Select(x => (x.Data, x.Receita)).ToArray());
    }

    [Fact]
    public async Task ObterAdministrativo_HistoricoSemDataPagamento_NaoGanhaDataEFicaForaDoPeriodo()
    {
        using var db = SqliteDbContextFactory.Create();
        await using var context = db.NewContext();
        var cenario = new Cenario(context);
        var lote = cenario.Partida(cenario.Futebol, new DateTime(2026, 9, 25, 16, 0, 0), finalizado: true);
        cenario.Ingresso(lote, 45, EnumStatusIngresso.Pago, Utc(9, 21), dataPagamentoUtc: null); // registro anterior à coluna
        cenario.Ingresso(lote, 50, EnumStatusIngresso.Pago, Utc(9, 21), Utc(9, 21));
        await cenario.SalvarAsync();
        var servico = NovoServico(context);

        var periodo = await servico.ObterAdministrativoAsync(Periodo(new DateTime(2026, 9, 21), new DateTime(2026, 9, 21)));
        var tudo = await servico.ObterAdministrativoAsync(new FiltroRelatorioAdministrativoDTO());

        Assert.Equal(50m, periodo.ResumoFinanceiro.ReceitaConfirmada);
        Assert.Equal(1, periodo.ResumoFinanceiro.IngressosPagos);
        Assert.Equal(45m, periodo.ResumoFinanceiro.ReceitaSemDataPagamento);
        Assert.Equal(1, periodo.ResumoFinanceiro.IngressosSemDataPagamento);

        Assert.Equal(95m, tudo.ResumoFinanceiro.ReceitaConfirmada);
        Assert.Equal(2, tudo.ResumoFinanceiro.IngressosPagos);
        Assert.Equal(45m, tudo.ResumoFinanceiro.ReceitaSemDataPagamento);
        // A série diária só tem o que tem data de pagamento.
        Assert.Equal(50m, Assert.Single(tudo.VendasPorDia).Receita);
    }

    // ─── Comparecimento ───────────────────────────────────────────────────────

    [Fact]
    public async Task ObterAdministrativo_Comparecimento_IgnoraIngressosDePartidaFutura()
    {
        using var db = SqliteDbContextFactory.Create();
        await using var context = db.NewContext();
        var cenario = new Cenario(context);
        var passada = cenario.Partida(cenario.Futebol, new DateTime(2026, 10, 4, 20, 0, 0), finalizado: true);
        var iniciadaHoje = cenario.Partida(cenario.Futebol, new DateTime(2026, 10, 5, 11, 30, 0), finalizado: false);
        var futura = cenario.Partida(cenario.Futebol, new DateTime(2026, 10, 6, 20, 0, 0), finalizado: false);
        cenario.Ingresso(passada, 50, EnumStatusIngresso.Utilizado, Utc(10, 1), Utc(10, 1));
        cenario.Ingresso(passada, 50, EnumStatusIngresso.Pago, Utc(10, 1), Utc(10, 1));
        cenario.Ingresso(iniciadaHoje, 50, EnumStatusIngresso.Utilizado, Utc(10, 1), Utc(10, 1));
        cenario.Ingresso(iniciadaHoje, 50, EnumStatusIngresso.Pago, Utc(10, 1), Utc(10, 1));
        cenario.Ingresso(futura, 50, EnumStatusIngresso.Pago, Utc(10, 1), Utc(10, 1));
        cenario.Ingresso(futura, 50, EnumStatusIngresso.Pago, Utc(10, 1), Utc(10, 1));
        cenario.Ingresso(futura, 50, EnumStatusIngresso.Utilizado, Utc(10, 1), Utc(10, 1)); // check-in antecipado
        await cenario.SalvarAsync();

        var relatorio = await NovoServico(context).ObterAdministrativoAsync(new FiltroRelatorioAdministrativoDTO());

        Assert.Equal(4, relatorio.ResumoFinanceiro.IngressosElegiveisCheckIn);
        Assert.Equal(2, relatorio.ResumoFinanceiro.CheckIns);
        Assert.Equal(50m, relatorio.ResumoFinanceiro.TaxaComparecimento);
    }

    [Fact]
    public async Task ObterAdministrativo_Comparecimento_RespeitaADataDaPartidaNoPeriodo()
    {
        using var db = SqliteDbContextFactory.Create();
        await using var context = db.NewContext();
        var cenario = new Cenario(context);
        var dia3 = cenario.Partida(cenario.Futebol, new DateTime(2026, 10, 3, 20, 0, 0), finalizado: true);
        var dia4 = cenario.Partida(cenario.Futebol, new DateTime(2026, 10, 4, 20, 0, 0), finalizado: true);
        cenario.Ingresso(dia3, 50, EnumStatusIngresso.Pago, Utc(9, 1), Utc(9, 1));
        cenario.Ingresso(dia4, 50, EnumStatusIngresso.Utilizado, Utc(9, 1), Utc(9, 1));
        await cenario.SalvarAsync();

        var relatorio = await NovoServico(context).ObterAdministrativoAsync(Periodo(new DateTime(2026, 10, 4), new DateTime(2026, 10, 4)));

        Assert.Equal(1, relatorio.ResumoFinanceiro.IngressosElegiveisCheckIn);
        Assert.Equal(100m, relatorio.ResumoFinanceiro.TaxaComparecimento);
    }

    [Fact]
    public async Task ObterAdministrativo_SemPartidaElegivel_ComparecimentoSemBase()
    {
        using var db = SqliteDbContextFactory.Create();
        await using var context = db.NewContext();
        var cenario = new Cenario(context);
        var futura = cenario.Partida(cenario.Futebol, new DateTime(2026, 10, 6, 20, 0, 0), finalizado: false);
        cenario.Ingresso(futura, 50, EnumStatusIngresso.Pago, Utc(10, 1), Utc(10, 1));
        await cenario.SalvarAsync();

        var relatorio = await NovoServico(context).ObterAdministrativoAsync(new FiltroRelatorioAdministrativoDTO());

        Assert.Equal(0, relatorio.ResumoFinanceiro.IngressosElegiveisCheckIn);
        Assert.Equal(0m, relatorio.ResumoFinanceiro.TaxaComparecimento);
    }

    // ─── Mesma regra no relatório do organizador de campeonato ────────────────

    [Fact]
    public async Task ObterOrganizadorCampeonato_ConfirmacaoPosterior_ReceitaContaNoDiaDoPagamento()
    {
        using var db = SqliteDbContextFactory.Create();
        await using var context = db.NewContext();
        var cenario = new Cenario(context);
        var lote = cenario.Partida(cenario.Futebol, new DateTime(2026, 9, 20, 16, 0, 0), finalizado: true);
        cenario.Ingresso(lote, 50, EnumStatusIngresso.Pago, dataCompraUtc: Utc(9, 10), dataPagamentoUtc: Utc(9, 15));
        await cenario.SalvarAsync();
        var servico = NovoServico(context);

        var diaDaCompra = await servico.ObterOrganizadorCampeonatoAsync(cenario.UsuarioOrgCamp.Id,
            new FiltroRelatorioDTO { Inicio = new DateTime(2026, 9, 10), Fim = new DateTime(2026, 9, 10) });
        var diaDoPagamento = await servico.ObterOrganizadorCampeonatoAsync(cenario.UsuarioOrgCamp.Id,
            new FiltroRelatorioDTO { Inicio = new DateTime(2026, 9, 15), Fim = new DateTime(2026, 9, 15) });

        Assert.Equal(0m, diaDaCompra.Resumo.ReceitaConfirmada);
        Assert.Empty(diaDaCompra.VendasPorPartida);
        Assert.Equal(50m, diaDoPagamento.Resumo.ReceitaConfirmada);
        Assert.Equal(50m, Assert.Single(diaDoPagamento.VendasPorPartida).Receita);
    }
}
