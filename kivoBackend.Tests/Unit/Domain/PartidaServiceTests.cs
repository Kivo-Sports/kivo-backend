using kivoBackend.Application.DTO;
using kivoBackend.Application.Services;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using kivoBackend.Tests.TestSupport;

namespace kivoBackend.Tests.Unit.Domain;

/// <summary>
/// Domain rules for bracket generation, round-robin scheduling, standings and
/// champion detection — the highest-value/highest-risk logic in the backend
/// (PartidaService.cs). All pure/deterministic aside from the mata-mata random
/// draw, which is asserted on structurally (pairing count, no self-pairing)
/// rather than on the exact draw.
/// </summary>
public class PartidaServiceTests
{
    private static Campeonato NovoCampeonato(
        EnumFormatoCampeonato formato,
        IEnumerable<Guid> timesAceitos,
        int pontosVitoria = 3,
        int pontosDerrota = 0,
        int pontosEmpate = 1,
        int? quantidadeTimesClassificam = null,
        DateTime? dataInicio = null,
        DateTime? dataFim = null)
    {
        var campeonato = new Campeonato
        {
            Id = Guid.NewGuid(),
            Nome = "Copa Teste",
            FormatoCampeonato = formato,
            PontosVitoria = pontosVitoria,
            PontosDerrota = pontosDerrota,
            PontosEmpate = pontosEmpate,
            QuantidadeTimesClassificam = quantidadeTimesClassificam,
            DataInicio = dataInicio ?? DateTime.Now.AddDays(-10),
            DataFim = dataFim ?? DateTime.Now.AddDays(30),
            CampeonatoTimes = timesAceitos
                .Select(t => new CampeonatoTime { TimeId = t, EnumStatusParticipacao = EnumStatusParticipacao.Aceito })
                .ToList(),
        };
        return campeonato;
    }

    private static PartidaService NovoService(
        Campeonato campeonato,
        InMemoryRepo<Partida>? partidas = null,
        FakeNotificacaoService? notificacoes = null,
        InMemoryTimeRepo? times = null)
        => new(
            partidas ?? new InMemoryRepo<Partida>(),
            new FakeCampeonatoRepository(campeonato),
            times ?? new InMemoryTimeRepo(),
            notificacoes ?? new FakeNotificacaoService());

    // ─── GerarPontosCorridos (round-robin) ──────────────────────────────────

    [Fact]
    public async Task GerarPontosCorridos_SeisTimes_CadaTimeJogaContraTodosOsOutrosUmaVez()
    {
        var times = Enumerable.Range(0, 6).Select(_ => Guid.NewGuid()).ToList();
        var campeonato = NovoCampeonato(EnumFormatoCampeonato.PontosCorridos, times);
        var partidasRepo = new InMemoryRepo<Partida>();
        var service = NovoService(campeonato, partidasRepo);

        await service.GerarPontosCorridos(campeonato.Id, new List<Guid>(times));

        // Round-robin de N times (par) gera N-1 rodadas de N/2 jogos = N*(N-1)/2 partidas totais.
        Assert.Equal(times.Count * (times.Count - 1) / 2, partidasRepo.Count);

        var confrontos = partidasRepo.Items
            .Select(p => (p.TimeCasaId!.Value, p.TimeVisitanteId!.Value))
            .ToList();

        foreach (var timeA in times)
        {
            var adversarios = confrontos
                .Where(c => c.Item1 == timeA || c.Item2 == timeA)
                .Select(c => c.Item1 == timeA ? c.Item2 : c.Item1)
                .ToList();

            // Jogou contra cada outro time exatamente uma vez.
            Assert.Equal(times.Count - 1, adversarios.Count);
            Assert.Equal(adversarios.Distinct().Count(), adversarios.Count);
            Assert.DoesNotContain(timeA, adversarios);
        }
    }

    [Fact]
    public async Task GerarPontosCorridos_NumeroImparDeTimes_UsaByeSemGerarPartidaFantasma()
    {
        var times = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToList();
        var campeonato = NovoCampeonato(EnumFormatoCampeonato.PontosCorridos, times);
        var partidasRepo = new InMemoryRepo<Partida>();
        var service = NovoService(campeonato, partidasRepo);

        await service.GerarPontosCorridos(campeonato.Id, new List<Guid>(times));

        Assert.All(partidasRepo.Items, p =>
        {
            Assert.NotEqual(Guid.Empty, p.TimeCasaId);
            Assert.NotEqual(Guid.Empty, p.TimeVisitanteId);
        });
        // Com bye, cada time joga uma rodada a menos que times.Count - 1 (a rodada do bye some para ele).
        Assert.True(partidasRepo.Count > 0);
    }

    [Fact]
    public async Task GerarTabela_MenosDeDoisTimesAceitos_Rejeita()
    {
        var timeUnico = Guid.NewGuid();
        var campeonato = NovoCampeonato(EnumFormatoCampeonato.PontosCorridos, new[] { timeUnico });
        var service = NovoService(campeonato);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.GerarTabela(campeonato.Id));
        Assert.Contains("pelo menos 2 times", ex.Message);
    }

    [Fact]
    public async Task GerarTabela_MataMataComNumeroDeTimesNaoPotenciaDeDois_Rejeita()
    {
        var times = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToList();
        var campeonato = NovoCampeonato(EnumFormatoCampeonato.MataMata, times);
        var service = NovoService(campeonato);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.GerarTabela(campeonato.Id));
        Assert.Contains("potência de 2", ex.Message);
    }

    [Fact]
    public async Task GerarTabela_MataMataComQuatroTimes_GeraDuasPartidasDeSemifinal()
    {
        var times = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToList();
        var campeonato = NovoCampeonato(EnumFormatoCampeonato.MataMata, times);
        var partidasRepo = new InMemoryRepo<Partida>();
        var service = NovoService(campeonato, partidasRepo);

        await service.GerarTabela(campeonato.Id);

        Assert.Equal(2, partidasRepo.Count);
        Assert.All(partidasRepo.Items, p => Assert.Equal(EnumFaseMataMata.Semifinais, p.Fase));
        var timesNasPartidas = partidasRepo.Items.SelectMany(p => new[] { p.TimeCasaId!.Value, p.TimeVisitanteId!.Value });
        Assert.Equal(times.OrderBy(x => x), timesNasPartidas.OrderBy(x => x));
    }

    [Theory]
    [InlineData(2, EnumFaseMataMata.Final)]
    [InlineData(4, EnumFaseMataMata.Semifinais)]
    [InlineData(8, EnumFaseMataMata.Quartas)]
    [InlineData(16, EnumFaseMataMata.Oitavas)]
    public async Task GerarMataMataInicial_DefineFaseInicialConformeQuantidadeDeTimes(int qtdTimes, EnumFaseMataMata faseEsperada)
    {
        var times = Enumerable.Range(0, qtdTimes).Select(_ => Guid.NewGuid()).ToList();
        var campeonato = NovoCampeonato(EnumFormatoCampeonato.MataMata, times);
        var partidasRepo = new InMemoryRepo<Partida>();
        var service = NovoService(campeonato, partidasRepo);

        await service.GerarMataMataInicial(campeonato.Id, new List<Guid>(times));

        Assert.Equal(qtdTimes / 2, partidasRepo.Count);
        Assert.All(partidasRepo.Items, p => Assert.Equal(faseEsperada, p.Fase));
    }

    // ─── AtualizarPlacarMataMata (bracket advancement + champion) ───────────

    [Fact]
    public async Task AtualizarPlacarMataMata_EmpateNaFinal_Rejeita()
    {
        var campeonato = NovoCampeonato(EnumFormatoCampeonato.MataMata, Array.Empty<Guid>());
        var service = NovoService(campeonato);
        var partida = new Partida
        {
            CampeonatoId = campeonato.Id,
            Fase = EnumFaseMataMata.Final,
            TimeCasaId = Guid.NewGuid(),
            TimeVisitanteId = Guid.NewGuid(),
            GolsTimeCasa = 1,
            GolsTimeVisitante = 1,
        };

        var ex = await Assert.ThrowsAsync<Exception>(() => service.AtualizarPlacarMataMata(partida));
        Assert.Contains("não pode terminar empatada", ex.Message);
    }

    [Fact]
    public async Task AtualizarPlacarMataMata_VencedorDaFinal_DefineCampeaoENotifica()
    {
        var campeonato = NovoCampeonato(EnumFormatoCampeonato.MataMata, Array.Empty<Guid>());
        var timeCasaId = Guid.NewGuid();
        var timeVisitanteId = Guid.NewGuid();
        var timeCasa = new Time { Id = timeCasaId, Nome = "Casa FC", OrganizadorTime = new OrganizadorTime { UsuarioId = Guid.NewGuid() } };
        var notificacoes = new FakeNotificacaoService();
        var service = NovoService(campeonato, times: new InMemoryTimeRepo(timeCasa), notificacoes: notificacoes);

        var final = new Partida
        {
            CampeonatoId = campeonato.Id,
            Fase = EnumFaseMataMata.Final,
            TimeCasaId = timeCasaId,
            TimeVisitanteId = timeVisitanteId,
            GolsTimeCasa = 2,
            GolsTimeVisitante = 0,
        };

        await service.AtualizarPlacarMataMata(final);

        Assert.Equal(timeCasaId, campeonato.TimeVencedorId);
        Assert.Contains(notificacoes.Criadas, n => n.UsuarioId == timeCasa.OrganizadorTime.UsuarioId && n.Tipo == EnumTipoNotificacao.CampeonatoFase);
    }

    [Fact]
    public async Task AtualizarPlacarMataMata_ParceiroDeChaveAindaNaoFinalizado_NaoAvancaFase()
    {
        var campeonato = NovoCampeonato(EnumFormatoCampeonato.MataMata, Array.Empty<Guid>());
        var partidasRepo = new InMemoryRepo<Partida>();
        var service = NovoService(campeonato, partidasRepo);

        var jogo1 = new Partida { CampeonatoId = campeonato.Id, Fase = EnumFaseMataMata.Semifinais, NumeroJogoChave = 1, TimeCasaId = Guid.NewGuid(), TimeVisitanteId = Guid.NewGuid(), GolsTimeCasa = 2, GolsTimeVisitante = 1 };
        var jogo2 = new Partida { CampeonatoId = campeonato.Id, Fase = EnumFaseMataMata.Semifinais, NumeroJogoChave = 2, TimeCasaId = Guid.NewGuid(), TimeVisitanteId = Guid.NewGuid(), Finalizado = false };
        await partidasRepo.Adicionar(jogo2);

        await service.AtualizarPlacarMataMata(jogo1);

        // Nenhuma partida da Final deve ter sido criada ainda (parceiro nao terminou).
        Assert.DoesNotContain(partidasRepo.Items, p => p.Fase == EnumFaseMataMata.Final);
    }

    [Fact]
    public async Task AtualizarPlacarMataMata_AmbosOsJogosDaChaveFinalizados_CriaPartidaDaProximaFase()
    {
        var campeonato = NovoCampeonato(EnumFormatoCampeonato.MataMata, Array.Empty<Guid>(), dataFim: DateTime.Now.AddDays(30));
        var partidasRepo = new InMemoryRepo<Partida>();
        var vencedor1 = Guid.NewGuid();
        var vencedor2 = Guid.NewGuid();
        var jogo2 = new Partida
        {
            CampeonatoId = campeonato.Id,
            Fase = EnumFaseMataMata.Semifinais,
            NumeroJogoChave = 2,
            TimeCasaId = vencedor2,
            TimeVisitanteId = Guid.NewGuid(),
            GolsTimeCasa = 3,
            GolsTimeVisitante = 1,
            Finalizado = true,
        };
        await partidasRepo.Adicionar(jogo2);
        var service = NovoService(campeonato, partidasRepo);

        var jogo1 = new Partida
        {
            CampeonatoId = campeonato.Id,
            Fase = EnumFaseMataMata.Semifinais,
            NumeroJogoChave = 1,
            TimeCasaId = vencedor1,
            TimeVisitanteId = Guid.NewGuid(),
            GolsTimeCasa = 2,
            GolsTimeVisitante = 0,
        };

        await service.AtualizarPlacarMataMata(jogo1);

        var proximaPartida = Assert.Single(partidasRepo.Items.Where(p => p.Fase == EnumFaseMataMata.Final));
        Assert.Equal(new[] { vencedor1, vencedor2 }.OrderBy(x => x), new[] { proximaPartida.TimeCasaId!.Value, proximaPartida.TimeVisitanteId!.Value }.OrderBy(x => x));
    }

    // ─── ObterClassificacaoTabela (standings / tiebreakers) ─────────────────

    [Fact]
    public async Task ObterClassificacaoTabela_OrdenaPorPontosVitoriasSaldoEGolsFeitos()
    {
        var timeA = Guid.NewGuid();
        var timeB = Guid.NewGuid();
        var timeC = Guid.NewGuid();
        var campeonato = NovoCampeonato(EnumFormatoCampeonato.PontosCorridos, new[] { timeA, timeB, timeC });
        var partidasRepo = new InMemoryRepo<Partida>(
            // A vence B 3x0 -> A: 3 pts, SG +3; B: 0 pts, SG -3
            new Partida { CampeonatoId = campeonato.Id, Rodada = 1, Finalizado = true, TimeCasaId = timeA, TimeVisitanteId = timeB, GolsTimeCasa = 3, GolsTimeVisitante = 0 },
            // B empata com C 1x1 -> B: +1 pt, SG 0; C: +1 pt, SG 0
            new Partida { CampeonatoId = campeonato.Id, Rodada = 2, Finalizado = true, TimeCasaId = timeB, TimeVisitanteId = timeC, GolsTimeCasa = 1, GolsTimeVisitante = 1 },
            // C perde de A 1x2 -> A: +3 pts (total 6), SG +1 (total +4); C: +0 (total 1), SG -1 (total -1)
            new Partida { CampeonatoId = campeonato.Id, Rodada = 3, Finalizado = true, TimeCasaId = timeC, TimeVisitanteId = timeA, GolsTimeCasa = 1, GolsTimeVisitante = 2 }
        );
        var service = NovoService(campeonato, partidasRepo);

        var tabela = await service.ObterClassificacaoTabela(campeonato.Id);

        Assert.Equal(3, tabela.Count);
        Assert.Equal(timeA, tabela[0].TimeId);
        Assert.Equal(6, tabela[0].Pontos);
        Assert.Equal(2, tabela[0].Vitorias);
        Assert.Equal(4, tabela[0].SaldoGols);
        Assert.Equal(1, tabela[0].Posicao);

        // B e C empatam em pontos (1) e saldo (0); desempate cai para GolsFeitos (ambos fizeram 1) — ordem estavel.
        Assert.Equal(1, tabela[1].Pontos);
        Assert.Equal(1, tabela[2].Pontos);
    }

    [Fact]
    public async Task ObterClassificacaoTabela_IgnoraPartidasNaoFinalizadas()
    {
        var timeA = Guid.NewGuid();
        var timeB = Guid.NewGuid();
        var campeonato = NovoCampeonato(EnumFormatoCampeonato.PontosCorridos, new[] { timeA, timeB });
        var partidasRepo = new InMemoryRepo<Partida>(
            new Partida { CampeonatoId = campeonato.Id, Rodada = 1, Finalizado = false, TimeCasaId = timeA, TimeVisitanteId = timeB, GolsTimeCasa = 5, GolsTimeVisitante = 0 }
        );
        var service = NovoService(campeonato, partidasRepo);

        var tabela = await service.ObterClassificacaoTabela(campeonato.Id);

        Assert.All(tabela, t => Assert.Equal(0, t.Pontos));
        Assert.All(tabela, t => Assert.Equal(0, t.Jogos));
    }

    // ─── VerificarFimFasePontosCorridos (round-robin -> champion / knockout cut) ─

    [Fact]
    public async Task VerificarFimFasePontosCorridos_PontosCorridosPuroComTodasFinalizadas_DefineCampeao()
    {
        var timeA = Guid.NewGuid();
        var timeB = Guid.NewGuid();
        var timeAEntity = new Time { Id = timeA, Nome = "Time A", OrganizadorTime = new OrganizadorTime { UsuarioId = Guid.NewGuid() } };
        var campeonato = NovoCampeonato(EnumFormatoCampeonato.PontosCorridos, new[] { timeA, timeB });
        var partidasRepo = new InMemoryRepo<Partida>(
            new Partida { CampeonatoId = campeonato.Id, Rodada = 1, Finalizado = true, TimeCasaId = timeA, TimeVisitanteId = timeB, GolsTimeCasa = 2, GolsTimeVisitante = 0 }
        );
        var notificacoes = new FakeNotificacaoService();
        var service = NovoService(campeonato, partidasRepo, notificacoes, new InMemoryTimeRepo(timeAEntity));

        await service.VerificarFimFasePontosCorridos(campeonato.Id);

        Assert.Equal(timeA, campeonato.TimeVencedorId);
        Assert.Contains(notificacoes.Criadas, n => n.UsuarioId == timeAEntity.OrganizadorTime.UsuarioId);
    }

    [Fact]
    public async Task VerificarFimFasePontosCorridos_NemTodasFinalizadas_NaoDefineCampeao()
    {
        var timeA = Guid.NewGuid();
        var timeB = Guid.NewGuid();
        var campeonato = NovoCampeonato(EnumFormatoCampeonato.PontosCorridos, new[] { timeA, timeB });
        var partidasRepo = new InMemoryRepo<Partida>(
            new Partida { CampeonatoId = campeonato.Id, Rodada = 1, Finalizado = false, TimeCasaId = timeA, TimeVisitanteId = timeB }
        );
        var service = NovoService(campeonato, partidasRepo);

        await service.VerificarFimFasePontosCorridos(campeonato.Id);

        Assert.Null(campeonato.TimeVencedorId);
    }

    [Fact]
    public async Task VerificarFimFasePontosCorridos_Hibrido_PromoveClassificadosParaMataMata()
    {
        var times = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToList();
        var campeonato = NovoCampeonato(EnumFormatoCampeonato.Hibrido, times, quantidadeTimesClassificam: 2);
        var partidasRepo = new InMemoryRepo<Partida>();
        // Fase de grupos completa: cada time jogou contra os outros 3 (todas finalizadas).
        for (int i = 0; i < times.Count; i++)
            for (int j = i + 1; j < times.Count; j++)
                await partidasRepo.Adicionar(new Partida
                {
                    CampeonatoId = campeonato.Id,
                    Rodada = 1,
                    Finalizado = true,
                    TimeCasaId = times[i],
                    TimeVisitanteId = times[j],
                    GolsTimeCasa = times.Count - i,
                    GolsTimeVisitante = 0,
                });
        var service = NovoService(campeonato, partidasRepo);

        await service.VerificarFimFasePontosCorridos(campeonato.Id);

        var mataMata = partidasRepo.Items.Where(p => p.Fase != EnumFaseMataMata.Nenhuma).ToList();
        Assert.Single(mataMata);
        Assert.Equal(EnumFaseMataMata.Final, mataMata[0].Fase);
        Assert.Null(campeonato.TimeVencedorId); // ainda nao ha campeao, so a final do mata-mata foi gerada
    }

    [Fact]
    public async Task VerificarFimFasePontosCorridos_Hibrido_SemQuantidadeClassificamConfigurada_Rejeita()
    {
        var times = Enumerable.Range(0, 2).Select(_ => Guid.NewGuid()).ToList();
        var campeonato = NovoCampeonato(EnumFormatoCampeonato.Hibrido, times, quantidadeTimesClassificam: 0);
        var partidasRepo = new InMemoryRepo<Partida>(
            new Partida { CampeonatoId = campeonato.Id, Rodada = 1, Finalizado = true, TimeCasaId = times[0], TimeVisitanteId = times[1], GolsTimeCasa = 1, GolsTimeVisitante = 0 }
        );
        var service = NovoService(campeonato, partidasRepo);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.VerificarFimFasePontosCorridos(campeonato.Id));
        Assert.Contains("classificados inválida", ex.Message);
    }

    // ─── AtualizarPlacarAdmin (retroactive edit guards) ─────────────────────

    [Fact]
    public async Task AtualizarPlacarAdmin_MataMataEmpate_Rejeita()
    {
        var campeonato = NovoCampeonato(EnumFormatoCampeonato.MataMata, Array.Empty<Guid>());
        var partida = new Partida { CampeonatoId = campeonato.Id, Fase = EnumFaseMataMata.Final, TimeCasaId = Guid.NewGuid(), TimeVisitanteId = Guid.NewGuid() };
        var partidasRepo = new InMemoryRepo<Partida>(partida);
        var service = NovoService(campeonato, partidasRepo);

        var ex = await Assert.ThrowsAsync<Exception>(() =>
            service.AtualizarPlacarAdmin(partida.Id, new AtualizarPlacarDTO { GolsTimeCasa = 1, GolsTimeVisitante = 1 }));
        Assert.Contains("não pode terminar empatada", ex.Message);
    }

    [Fact]
    public async Task AtualizarPlacarAdmin_ProximaFaseJaOcorreu_Rejeita()
    {
        // NumeroJogoChave=2 (nao 1) deliberadamente: o guard em
        // EditarPlacarMataMataAdmin compara `p.NumeroJogoChave != partida.NumeroJogoChave`
        // entre FASES DIFERENTES antes de checar se ja existe partida dependente.
        // Como a Final sempre tem NumeroJogoChave=1, essa comparacao so "funciona"
        // por coincidencia quando a semifinal em edicao NAO e a de numero 1 — ver
        // nota "AtualizarPlacarAdmin_ProximaFaseJaOcorreu_NumeroJogoChave1_NaoBloqueiaEdicao"
        // logo abaixo, que documenta o caso em que o guard falha silenciosamente.
        var campeonato = NovoCampeonato(EnumFormatoCampeonato.MataMata, Array.Empty<Guid>());
        var partida = new Partida { CampeonatoId = campeonato.Id, Fase = EnumFaseMataMata.Semifinais, NumeroJogoChave = 2, TimeCasaId = Guid.NewGuid(), TimeVisitanteId = Guid.NewGuid() };
        var proximaFase = new Partida { CampeonatoId = campeonato.Id, Fase = EnumFaseMataMata.Final, NumeroJogoChave = 1, Finalizado = true, TimeCasaId = Guid.NewGuid(), TimeVisitanteId = Guid.NewGuid() };
        var partidasRepo = new InMemoryRepo<Partida>(partida, proximaFase);
        var service = NovoService(campeonato, partidasRepo);

        var ex = await Assert.ThrowsAsync<Exception>(() =>
            service.AtualizarPlacarAdmin(partida.Id, new AtualizarPlacarDTO { GolsTimeCasa = 2, GolsTimeVisitante = 0 }));
        Assert.Contains("próxima fase já ocorreu", ex.Message);
    }

    [Fact]
    public async Task AtualizarPlacarAdmin_ProximaFaseJaOcorreu_NumeroJogoChave1_NaoBloqueiaEdicao()
    {
        // KNOWN GAP (see PartidaService.EditarPlacarMataMataAdmin): o guard exclui o
        // candidato a "dependente" quando `p.NumeroJogoChave == partida.NumeroJogoChave`,
        // comparando numeros de jogos de FASES DIFERENTES. Como toda Final tem
        // NumeroJogoChave=1, uma semifinal que TAMBEM seja NumeroJogoChave=1 nunca
        // aciona a protecao "proxima fase ja ocorreu" — mesmo com a Final ja
        // finalizada, a edicao passa. Documentado aqui para nao virar regressao
        // silenciosa; recomendado remover a comparacao `!=` (ou usar Rodada/Slot
        // dedicado) na proxima rodada de correcoes de PartidaService.
        var campeonato = NovoCampeonato(EnumFormatoCampeonato.MataMata, Array.Empty<Guid>());
        var partida = new Partida { CampeonatoId = campeonato.Id, Fase = EnumFaseMataMata.Semifinais, NumeroJogoChave = 1, TimeCasaId = Guid.NewGuid(), TimeVisitanteId = Guid.NewGuid() };
        var final = new Partida { CampeonatoId = campeonato.Id, Fase = EnumFaseMataMata.Final, NumeroJogoChave = 1, Finalizado = true, TimeCasaId = Guid.NewGuid(), TimeVisitanteId = Guid.NewGuid() };
        var partidasRepo = new InMemoryRepo<Partida>(partida, final);
        var service = NovoService(campeonato, partidasRepo);

        // Nao deveria ter sido permitido (a final ja aconteceu), mas hoje e.
        await service.AtualizarPlacarAdmin(partida.Id, new AtualizarPlacarDTO { GolsTimeCasa = 2, GolsTimeVisitante = 0 });

        Assert.True(partida.Finalizado);
    }

    [Fact]
    public async Task AtualizarPlacarAdmin_HibridoComMataMataJaFinalizado_ImpedeAlterarFaseDeGrupos()
    {
        var campeonato = NovoCampeonato(EnumFormatoCampeonato.Hibrido, Array.Empty<Guid>());
        var partidaGrupo = new Partida { CampeonatoId = campeonato.Id, Rodada = 1, TimeCasaId = Guid.NewGuid(), TimeVisitanteId = Guid.NewGuid() };
        var jogoMataMata = new Partida { CampeonatoId = campeonato.Id, Fase = EnumFaseMataMata.Final, Finalizado = true, TimeCasaId = Guid.NewGuid(), TimeVisitanteId = Guid.NewGuid() };
        var partidasRepo = new InMemoryRepo<Partida>(partidaGrupo, jogoMataMata);
        var service = NovoService(campeonato, partidasRepo);

        var ex = await Assert.ThrowsAsync<Exception>(() =>
            service.AtualizarPlacarAdmin(partidaGrupo.Id, new AtualizarPlacarDTO { GolsTimeCasa = 1, GolsTimeVisitante = 0 }));
        Assert.Contains("mata-mata já teve jogos finalizados", ex.Message);
    }

    [Fact]
    public async Task AtualizarPlacarAdmin_PartidaNaoEncontrada_Rejeita()
    {
        var campeonato = NovoCampeonato(EnumFormatoCampeonato.PontosCorridos, Array.Empty<Guid>());
        var service = NovoService(campeonato);

        var ex = await Assert.ThrowsAsync<Exception>(() =>
            service.AtualizarPlacarAdmin(Guid.NewGuid(), new AtualizarPlacarDTO { GolsTimeCasa = 1, GolsTimeVisitante = 0 }));
        Assert.Contains("não encontrada", ex.Message);
    }

    // ─── Agendamento: horário comercial esportivo (via efeito observável) ──

    [Fact]
    public async Task GerarPontosCorridos_PartidasSaoAgendadasNoFimDeSemana()
    {
        var times = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToList();
        var campeonato = NovoCampeonato(EnumFormatoCampeonato.PontosCorridos, times, dataInicio: DateTime.Now, dataFim: DateTime.Now.AddDays(21));
        var partidasRepo = new InMemoryRepo<Partida>();
        var service = NovoService(campeonato, partidasRepo);

        await service.GerarPontosCorridos(campeonato.Id, new List<Guid>(times));

        Assert.All(partidasRepo.Items, p =>
        {
            Assert.NotNull(p.DataHora);
            Assert.True(p.DataHora!.Value.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday);
        });
    }

    // ─── GerarTabela: demais formatos e notificação ─────────────────────────

    [Fact]
    public async Task GerarTabela_PontosCorridos_NotificaOrganizadorDoCampeonato()
    {
        var usuarioOrganizadorId = Guid.NewGuid();
        var times = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToList();
        var campeonato = NovoCampeonato(EnumFormatoCampeonato.PontosCorridos, times);
        campeonato.OrganizadorCampeonato = new OrganizadorCampeonato { UsuarioId = usuarioOrganizadorId };
        var notificacoes = new FakeNotificacaoService();
        var service = NovoService(campeonato, notificacoes: notificacoes);

        await service.GerarTabela(campeonato.Id);

        Assert.Contains(notificacoes.Criadas, n => n.UsuarioId == usuarioOrganizadorId && n.Tipo == EnumTipoNotificacao.Sistema);
    }

    [Fact]
    public async Task GerarTabela_Hibrido_GeraJogosDeFaseDeGrupos()
    {
        var times = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToList();
        var campeonato = NovoCampeonato(EnumFormatoCampeonato.Hibrido, times);
        var partidasRepo = new InMemoryRepo<Partida>();
        var service = NovoService(campeonato, partidasRepo);

        await service.GerarTabela(campeonato.Id);

        Assert.True(partidasRepo.Count > 0);
        Assert.All(partidasRepo.Items, p => Assert.Equal(EnumFaseMataMata.Nenhuma, p.Fase));
    }

    [Fact]
    public async Task GerarTabela_CampeonatoInexistente_Lanca()
    {
        var service = new PartidaService(new InMemoryRepo<Partida>(), new FakeCampeonatoRepository(null), new InMemoryTimeRepo(), new FakeNotificacaoService());

        await Assert.ThrowsAsync<Exception>(() => service.GerarTabela(Guid.NewGuid()));
    }

    // ─── ObterClassificacaoProximaFase ───────────────────────────────────────

    [Fact]
    public async Task ObterClassificacaoProximaFase_CalculaPontosVitoriasESaldo()
    {
        var timeA = Guid.NewGuid();
        var timeB = Guid.NewGuid();
        var campeonato = NovoCampeonato(EnumFormatoCampeonato.PontosCorridos, new[] { timeA, timeB });
        var partida = new Partida { CampeonatoId = campeonato.Id, Rodada = 1, Finalizado = true, TimeCasaId = timeA, TimeVisitanteId = timeB, GolsTimeCasa = 3, GolsTimeVisitante = 1 };
        var service = NovoService(campeonato, new InMemoryRepo<Partida>(partida));

        var resultado = await service.ObterClassificacaoProximaFase(campeonato.Id);

        var classificacaoA = resultado.Single(x => x.TimeId == timeA);
        var classificacaoB = resultado.Single(x => x.TimeId == timeB);
        Assert.Equal(3, classificacaoA.Pontos);
        Assert.Equal(1, classificacaoA.Vitorias);
        Assert.Equal(2, classificacaoA.SaldoGols);
        Assert.Equal(0, classificacaoB.Pontos);
        Assert.Equal(-2, classificacaoB.SaldoGols);
    }

    [Fact]
    public async Task ObterClassificacaoProximaFase_Empate_DistribuiPontosDeEmpate()
    {
        var timeA = Guid.NewGuid();
        var timeB = Guid.NewGuid();
        var campeonato = NovoCampeonato(EnumFormatoCampeonato.PontosCorridos, new[] { timeA, timeB }, pontosEmpate: 1);
        var partida = new Partida { CampeonatoId = campeonato.Id, Rodada = 1, Finalizado = true, TimeCasaId = timeA, TimeVisitanteId = timeB, GolsTimeCasa = 1, GolsTimeVisitante = 1 };
        var service = NovoService(campeonato, new InMemoryRepo<Partida>(partida));

        var resultado = await service.ObterClassificacaoProximaFase(campeonato.Id);

        Assert.All(resultado, c => Assert.Equal(1, c.Pontos));
    }

    // ─── NotificarResultadoPartida (via AtualizarPlacarAdmin) ────────────────

    [Fact]
    public async Task AtualizarPlacarAdmin_PontosCorridos_NotificaOrganizadoresDosDoisTimes()
    {
        var usuarioCasaId = Guid.NewGuid();
        var usuarioVisitanteId = Guid.NewGuid();
        var timeCasa = new Time { Id = Guid.NewGuid(), Nome = "Casa", Cidade = "SP", Estado = "SP", OrganizadorTime = new OrganizadorTime { UsuarioId = usuarioCasaId } };
        var timeVisitante = new Time { Id = Guid.NewGuid(), Nome = "Visitante", Cidade = "RJ", Estado = "RJ", OrganizadorTime = new OrganizadorTime { UsuarioId = usuarioVisitanteId } };
        var campeonato = NovoCampeonato(EnumFormatoCampeonato.PontosCorridos, new[] { timeCasa.Id, timeVisitante.Id });
        var partida = new Partida { Id = Guid.NewGuid(), CampeonatoId = campeonato.Id, TimeCasaId = timeCasa.Id, TimeVisitanteId = timeVisitante.Id, Fase = EnumFaseMataMata.Nenhuma };
        var partidasRepo = new InMemoryRepo<Partida>(partida);
        var timesRepo = new InMemoryTimeRepo(timeCasa, timeVisitante);
        var notificacoes = new FakeNotificacaoService();
        var service = NovoService(campeonato, partidasRepo, notificacoes, timesRepo);

        await service.AtualizarPlacarAdmin(partida.Id, new AtualizarPlacarDTO { GolsTimeCasa = 2, GolsTimeVisitante = 1 });

        Assert.Contains(notificacoes.Criadas, n => n.UsuarioId == usuarioCasaId);
        Assert.Contains(notificacoes.Criadas, n => n.UsuarioId == usuarioVisitanteId);
    }
}
