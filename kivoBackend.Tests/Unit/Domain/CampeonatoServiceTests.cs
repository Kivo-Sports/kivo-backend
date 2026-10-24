using kivoBackend.Application.DTO;
using kivoBackend.Application.Services;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using kivoBackend.Tests.TestSupport;

namespace kivoBackend.Tests.Unit.Domain;

/// <summary>
/// Status-transition and lifecycle rules in CampeonatoService that aren't
/// already covered by SecurityAuthorizationTests (which focuses on ownership).
/// </summary>
public class CampeonatoServiceTests
{
    private static CampeonatoService NovoService(
        Campeonato campeonato,
        InMemoryRepo<CampeonatoTime>? convites = null,
        InMemoryTimeRepo? times = null,
        InMemoryRepo<Partida>? partidas = null,
        FakeNotificacaoService? notificacoes = null)
        => new(
            new InMemoryRepo<Campeonato>(campeonato),
            convites ?? new InMemoryRepo<CampeonatoTime>(),
            times ?? new InMemoryTimeRepo(),
            partidas ?? new InMemoryRepo<Partida>(),
            new FakeCampeonatoRepository(campeonato),
            notificacoes ?? new FakeNotificacaoService());

    // ─── AbrirInscricoes ─────────────────────────────────────────────────────

    [Fact]
    public async Task AbrirInscricoes_APartirDeRascunho_Permite()
    {
        var campeonato = TestEntities.Campeonato(Guid.NewGuid());
        campeonato.EnumStatusCampeonato = EnumStatusCampeonato.Rascunho;
        var service = NovoService(campeonato);

        await service.AbrirInscricoes(campeonato.Id);

        Assert.Equal(EnumStatusCampeonato.InscricoesAbertas, campeonato.EnumStatusCampeonato);
    }

    [Fact]
    public async Task AbrirInscricoes_QuandoJaCancelado_Rejeita()
    {
        var campeonato = TestEntities.Campeonato(Guid.NewGuid());
        campeonato.EnumStatusCampeonato = EnumStatusCampeonato.Cancelado;
        var service = NovoService(campeonato);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.AbrirInscricoes(campeonato.Id));
        Assert.Contains("a partir do status Rascunho", ex.Message);
    }

    // ─── IniciarCampeonato ───────────────────────────────────────────────────

    [Fact]
    public async Task IniciarCampeonato_SemTimesConfirmados_Rejeita()
    {
        var campeonato = TestEntities.Campeonato(Guid.NewGuid());
        campeonato.CampeonatoTimes = new List<CampeonatoTime>();
        var service = NovoService(campeonato);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.IniciarCampeonato(campeonato.Id));
        Assert.Contains("sem times confirmados", ex.Message);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(6)]
    public async Task IniciarCampeonato_MataMataComQuantidadeNaoPotenciaDeDois_Rejeita(int qtdTimes)
    {
        var campeonato = TestEntities.Campeonato(Guid.NewGuid());
        campeonato.FormatoCampeonato = EnumFormatoCampeonato.MataMata;
        campeonato.CampeonatoTimes = Enumerable.Range(0, qtdTimes)
            .Select(_ => new CampeonatoTime { TimeId = Guid.NewGuid(), EnumStatusParticipacao = EnumStatusParticipacao.Aceito })
            .ToList();
        var service = NovoService(campeonato);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.IniciarCampeonato(campeonato.Id));
        Assert.Contains("potência de 2", ex.Message);
    }

    [Fact]
    public async Task IniciarCampeonato_MataMataComQuatroTimes_Permite()
    {
        var campeonato = TestEntities.Campeonato(Guid.NewGuid());
        // EnumStatusCampeonato so fica "presa" em Rascunho/Cancelado; qualquer
        // outro valor definido cai no calculo por data — para observar
        // EmAndamento apos iniciar, a janela do campeonato precisa ja ter
        // comecado (DataInicio no passado).
        campeonato.DataInicio = DateTime.Now.AddDays(-1);
        campeonato.DataFim = DateTime.Now.AddDays(30);
        campeonato.FormatoCampeonato = EnumFormatoCampeonato.MataMata;
        campeonato.CampeonatoTimes = Enumerable.Range(0, 4)
            .Select(_ => new CampeonatoTime { TimeId = Guid.NewGuid(), EnumStatusParticipacao = EnumStatusParticipacao.Aceito })
            .ToList();
        var service = NovoService(campeonato);

        var resultado = await service.IniciarCampeonato(campeonato.Id);

        Assert.Equal(EnumStatusCampeonato.EmAndamento, resultado.EnumStatusCampeonato);
    }

    [Fact]
    public async Task IniciarCampeonato_PontosCorridosComMenosDeOitoTimes_Rejeita()
    {
        var campeonato = TestEntities.Campeonato(Guid.NewGuid());
        campeonato.FormatoCampeonato = EnumFormatoCampeonato.PontosCorridos;
        campeonato.CampeonatoTimes = Enumerable.Range(0, 7)
            .Select(_ => new CampeonatoTime { TimeId = Guid.NewGuid(), EnumStatusParticipacao = EnumStatusParticipacao.Aceito })
            .ToList();
        var service = NovoService(campeonato);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.IniciarCampeonato(campeonato.Id));
        Assert.Contains("pelo menos 8 times", ex.Message);
    }

    [Fact]
    public async Task IniciarCampeonato_ContaApenasTimesAceitos_IgnorandoPendentesERecusados()
    {
        var campeonato = TestEntities.Campeonato(Guid.NewGuid());
        campeonato.FormatoCampeonato = EnumFormatoCampeonato.MataMata;
        campeonato.CampeonatoTimes = new List<CampeonatoTime>
        {
            new() { TimeId = Guid.NewGuid(), EnumStatusParticipacao = EnumStatusParticipacao.Aceito },
            new() { TimeId = Guid.NewGuid(), EnumStatusParticipacao = EnumStatusParticipacao.Pendente },
            new() { TimeId = Guid.NewGuid(), EnumStatusParticipacao = EnumStatusParticipacao.Recusado },
        };
        // Apenas 1 aceito -> nao e potencia de 2 valida para mata-mata (ePotenciaDeDois(1) é true na verdade,
        // pois 1 = 2^0). Usamos 3 aceitos para validar que pendente/recusado NAO contam.
        campeonato.CampeonatoTimes = new List<CampeonatoTime>
        {
            new() { TimeId = Guid.NewGuid(), EnumStatusParticipacao = EnumStatusParticipacao.Aceito },
            new() { TimeId = Guid.NewGuid(), EnumStatusParticipacao = EnumStatusParticipacao.Aceito },
            new() { TimeId = Guid.NewGuid(), EnumStatusParticipacao = EnumStatusParticipacao.Aceito },
            new() { TimeId = Guid.NewGuid(), EnumStatusParticipacao = EnumStatusParticipacao.Pendente },
            new() { TimeId = Guid.NewGuid(), EnumStatusParticipacao = EnumStatusParticipacao.Recusado },
        };
        var service = NovoService(campeonato);

        // 3 aceitos nao e potencia de 2 -> deve rejeitar (prova que pendente/recusado nao elevaram para 4 ou 5).
        var ex = await Assert.ThrowsAsync<Exception>(() => service.IniciarCampeonato(campeonato.Id));
        Assert.Contains("potência de 2", ex.Message);
    }

    // ─── EditarCampeonato ────────────────────────────────────────────────────

    [Fact]
    public async Task EditarCampeonato_CampeonatoEmAndamento_OrganizadorComumNaoPodeEditar()
    {
        var campeonato = TestEntities.Campeonato(Guid.NewGuid());
        // Sai do estado "preso" Rascunho antes de mudar as datas, senao o getter
        // de EnumStatusCampeonato ignora as datas e continua reportando Rascunho.
        campeonato.EnumStatusCampeonato = EnumStatusCampeonato.InscricoesAbertas;
        campeonato.DataInicio = DateTime.Now.AddDays(-1); // status computado vira EmAndamento
        campeonato.DataFim = DateTime.Now.AddDays(30);
        var service = NovoService(campeonato);

        var ex = await Assert.ThrowsAsync<Exception>(() =>
            service.EditarCampeonato(campeonato.Id, DtoValido(), ehAdmin: false));
        Assert.Contains("já iniciou ou finalizou", ex.Message);
    }

    [Fact]
    public async Task EditarCampeonato_CampeonatoEmAndamento_AdminPodeEditar()
    {
        var campeonato = TestEntities.Campeonato(Guid.NewGuid());
        campeonato.DataInicio = DateTime.Now.AddDays(-1);
        campeonato.DataFim = DateTime.Now.AddDays(30);
        var service = NovoService(campeonato);

        var resultado = await service.EditarCampeonato(campeonato.Id, DtoValido(), ehAdmin: true);

        Assert.Equal(DtoValido().Nome, resultado.Nome);
    }

    [Fact]
    public async Task EditarCampeonato_DataFimAnteriorOuIgualADataInicio_Rejeita()
    {
        var campeonato = TestEntities.Campeonato(Guid.NewGuid());
        var service = NovoService(campeonato);
        var dto = DtoValido();
        dto.DataInicio = DateTime.Today.AddDays(10);
        dto.DataFim = DateTime.Today.AddDays(10);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.EditarCampeonato(campeonato.Id, dto, ehAdmin: false));
        Assert.Contains("posterior à data de início", ex.Message);
    }

    // ─── CancelarCampeonato / DescancelarCampeonato ──────────────────────────

    [Fact]
    public async Task CancelarCampeonato_JaFinalizado_Rejeita()
    {
        var campeonato = TestEntities.Campeonato(Guid.NewGuid());
        campeonato.EnumStatusCampeonato = EnumStatusCampeonato.InscricoesAbertas;
        campeonato.DataInicio = DateTime.Now.AddDays(-30);
        campeonato.DataFim = DateTime.Now.AddDays(-1); // status computado -> Finalizado
        var service = NovoService(campeonato);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.CancelarCampeonato(campeonato.Id));
        Assert.Contains("já foi finalizado", ex.Message);
    }

    [Fact]
    public async Task CancelarCampeonato_JaCancelado_Rejeita()
    {
        var campeonato = TestEntities.Campeonato(Guid.NewGuid());
        campeonato.EnumStatusCampeonato = EnumStatusCampeonato.Cancelado;
        var service = NovoService(campeonato);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.CancelarCampeonato(campeonato.Id));
        Assert.Contains("já está cancelado", ex.Message);
    }

    [Fact]
    public async Task CancelarCampeonato_RemoveApenasPartidasNaoFinalizadas()
    {
        var campeonato = TestEntities.Campeonato(Guid.NewGuid());
        campeonato.EnumStatusCampeonato = EnumStatusCampeonato.InscricoesAbertas;
        var partidaFinalizada = new Partida { CampeonatoId = campeonato.Id, Finalizado = true };
        var partidaPendente = new Partida { CampeonatoId = campeonato.Id, Finalizado = false };
        campeonato.Partidas = new List<Partida> { partidaFinalizada, partidaPendente };
        var service = NovoService(campeonato);

        await service.CancelarCampeonato(campeonato.Id);

        Assert.Equal(EnumStatusCampeonato.Cancelado, campeonato.EnumStatusCampeonato);
        Assert.Single(campeonato.Partidas);
        Assert.Contains(partidaFinalizada, campeonato.Partidas);
    }

    [Fact]
    public async Task DescancelarCampeonato_QuandoNaoCancelado_Rejeita()
    {
        var campeonato = TestEntities.Campeonato(Guid.NewGuid());
        campeonato.EnumStatusCampeonato = EnumStatusCampeonato.InscricoesAbertas;
        var service = NovoService(campeonato);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.DescancelarCampeonato(campeonato.Id));
        Assert.Contains("Apenas campeonatos cancelados", ex.Message);
    }

    [Fact]
    public async Task DescancelarCampeonato_QuandoCancelado_VoltaParaInscricoesAbertas()
    {
        var campeonato = TestEntities.Campeonato(Guid.NewGuid());
        campeonato.EnumStatusCampeonato = EnumStatusCampeonato.Cancelado;
        var service = NovoService(campeonato);

        await service.DescancelarCampeonato(campeonato.Id);

        Assert.Equal(EnumStatusCampeonato.InscricoesAbertas, campeonato.EnumStatusCampeonato);
    }

    // ─── AdicionarTimeAoCampeonato (convite) ─────────────────────────────────

    [Fact]
    public async Task AdicionarTimeAoCampeonato_EsporteDiferente_Rejeita()
    {
        var campeonato = TestEntities.Campeonato(Guid.NewGuid());
        var time = new Time { Id = Guid.NewGuid(), EsporteId = Guid.NewGuid(), Nome = "Time X" };
        Assert.NotEqual(campeonato.EsporteId, time.EsporteId);
        var service = NovoService(campeonato, times: new InMemoryTimeRepo(time));

        var ex = await Assert.ThrowsAsync<Exception>(() => service.AdicionarTimeAoCampeonato(campeonato.Id, time.Id));
        Assert.Contains("mesmo esporte", ex.Message);
    }

    [Fact]
    public async Task AdicionarTimeAoCampeonato_MesmoEsporte_CriaConviteENotificaOrganizador()
    {
        var campeonato = TestEntities.Campeonato(Guid.NewGuid());
        var usuarioId = Guid.NewGuid();
        var time = new Time { Id = Guid.NewGuid(), EsporteId = campeonato.EsporteId, Nome = "Time X", OrganizadorTime = new OrganizadorTime { UsuarioId = usuarioId } };
        var convites = new InMemoryRepo<CampeonatoTime>();
        var notificacoes = new FakeNotificacaoService();
        var service = NovoService(campeonato, convites: convites, times: new InMemoryTimeRepo(time), notificacoes: notificacoes);

        await service.AdicionarTimeAoCampeonato(campeonato.Id, time.Id);

        Assert.Equal(1, convites.Count);
        Assert.Contains(notificacoes.Criadas, n => n.UsuarioId == usuarioId && n.Tipo == EnumTipoNotificacao.TimeInscrito);
    }

    // ─── ObterConvitesPorOrganizador ────────────────────────────────────────

    [Fact]
    public async Task ObterConvitesPorOrganizador_FiltraPendentesDoOrganizadorTime()
    {
        var campeonato = TestEntities.Campeonato(Guid.NewGuid());
        var orgTimeId = Guid.NewGuid();
        var timeDoOrganizador = new Time { Id = Guid.NewGuid(), Nome = "A", Cidade = "SP", Estado = "SP", OrganizadorTimeId = orgTimeId };
        var outroTime = new Time { Id = Guid.NewGuid(), Nome = "B", Cidade = "SP", Estado = "SP", OrganizadorTimeId = Guid.NewGuid() };
        var pendenteDoOrganizador = new CampeonatoTime { Id = Guid.NewGuid(), CampeonatoId = campeonato.Id, TimeId = timeDoOrganizador.Id, Time = timeDoOrganizador, EnumStatusParticipacao = EnumStatusParticipacao.Pendente };
        var aceitoDoOrganizador = new CampeonatoTime { Id = Guid.NewGuid(), CampeonatoId = campeonato.Id, TimeId = timeDoOrganizador.Id, Time = timeDoOrganizador, EnumStatusParticipacao = EnumStatusParticipacao.Aceito };
        var pendenteDeOutro = new CampeonatoTime { Id = Guid.NewGuid(), CampeonatoId = campeonato.Id, TimeId = outroTime.Id, Time = outroTime, EnumStatusParticipacao = EnumStatusParticipacao.Pendente };
        var convites = new InMemoryRepo<CampeonatoTime>(pendenteDoOrganizador, aceitoDoOrganizador, pendenteDeOutro);
        var service = NovoService(campeonato, convites: convites);

        var resultado = (await service.ObterConvitesPorOrganizador(orgTimeId)).ToList();

        Assert.Single(resultado);
        Assert.Equal(pendenteDoOrganizador.Id, resultado[0].Id);
    }

    // ─── ObterTodosComTimes ──────────────────────────────────────────────────

    [Fact]
    public void ObterTodosComTimes_NaoImplementado_Lanca()
    {
        var campeonato = TestEntities.Campeonato(Guid.NewGuid());
        var service = NovoService(campeonato);

        // The method throws synchronously (not inside an async state machine), so the
        // exception surfaces on invocation rather than when awaiting the returned Task.
        Exception? capturada = null;
        try { service.ObterTodosComTimes(); }
        catch (Exception ex) { capturada = ex; }

        Assert.IsType<NotImplementedException>(capturada);
    }

    // ─── RemoverTimeDoCampeonato ─────────────────────────────────────────────

    [Fact]
    public async Task RemoverTimeDoCampeonato_VinculoExistente_Remove()
    {
        var campeonato = TestEntities.Campeonato(Guid.NewGuid());
        var timeId = Guid.NewGuid();
        var vinculo = new CampeonatoTime { Id = Guid.NewGuid(), CampeonatoId = campeonato.Id, TimeId = timeId };
        var convites = new InMemoryRepo<CampeonatoTime>(vinculo);
        var service = NovoService(campeonato, convites: convites);

        await service.RemoverTimeDoCampeonato(campeonato.Id, timeId);

        Assert.Equal(0, convites.Count);
    }

    [Fact]
    public async Task RemoverTimeDoCampeonato_SemVinculo_NaoLanca()
    {
        var campeonato = TestEntities.Campeonato(Guid.NewGuid());
        var service = NovoService(campeonato);

        await service.RemoverTimeDoCampeonato(campeonato.Id, Guid.NewGuid());
    }

    // ─── ResponderConviteCampeonato ──────────────────────────────────────────

    [Fact]
    public async Task ResponderConviteCampeonato_ConviteInexistente_Lanca()
    {
        var campeonato = TestEntities.Campeonato(Guid.NewGuid());
        var service = NovoService(campeonato);

        await Assert.ThrowsAsync<Exception>(() => service.ResponderConviteCampeonato(Guid.NewGuid(), Guid.NewGuid(), true));
    }

    [Fact]
    public async Task ResponderConviteCampeonato_OrganizadorErrado_LancaUnauthorized()
    {
        var campeonato = TestEntities.Campeonato(Guid.NewGuid());
        var time = new Time { Id = Guid.NewGuid(), Nome = "A", Cidade = "SP", Estado = "SP", OrganizadorTimeId = Guid.NewGuid() };
        var vinculo = new CampeonatoTime { Id = Guid.NewGuid(), CampeonatoId = campeonato.Id, TimeId = time.Id, Time = time, EnumStatusParticipacao = EnumStatusParticipacao.Pendente };
        var convites = new InMemoryRepo<CampeonatoTime>(vinculo);
        var service = NovoService(campeonato, convites: convites);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ResponderConviteCampeonato(vinculo.Id, Guid.NewGuid(), true));
    }

    [Fact]
    public async Task ResponderConviteCampeonato_AceitoComOrganizadorValido_NotificaOrganizadorDoCampeonato()
    {
        var usuarioOrganizadorId = Guid.NewGuid();
        var campeonato = TestEntities.Campeonato(Guid.NewGuid());
        campeonato.OrganizadorCampeonato = new OrganizadorCampeonato { UsuarioId = usuarioOrganizadorId };
        var orgTimeId = Guid.NewGuid();
        var time = new Time { Id = Guid.NewGuid(), Nome = "A", Cidade = "SP", Estado = "SP", OrganizadorTimeId = orgTimeId };
        var vinculo = new CampeonatoTime { Id = Guid.NewGuid(), CampeonatoId = campeonato.Id, TimeId = time.Id, Time = time, EnumStatusParticipacao = EnumStatusParticipacao.Pendente };
        var convites = new InMemoryRepo<CampeonatoTime>(vinculo);
        var notificacoes = new FakeNotificacaoService();
        var service = NovoService(campeonato, convites: convites, times: new InMemoryTimeRepo(time), notificacoes: notificacoes);

        await service.ResponderConviteCampeonato(vinculo.Id, orgTimeId, aceito: true);

        Assert.Equal(EnumStatusParticipacao.Aceito, vinculo.EnumStatusParticipacao);
        Assert.Contains(notificacoes.Criadas, n => n.UsuarioId == usuarioOrganizadorId);
    }

    [Fact]
    public async Task ResponderConviteCampeonato_JaRespondido_Lanca()
    {
        var campeonato = TestEntities.Campeonato(Guid.NewGuid());
        var orgTimeId = Guid.NewGuid();
        var time = new Time { Id = Guid.NewGuid(), Nome = "A", Cidade = "SP", Estado = "SP", OrganizadorTimeId = orgTimeId };
        var vinculo = new CampeonatoTime { Id = Guid.NewGuid(), CampeonatoId = campeonato.Id, TimeId = time.Id, Time = time, EnumStatusParticipacao = EnumStatusParticipacao.Aceito, RespondidoEm = DateTime.Now };
        var convites = new InMemoryRepo<CampeonatoTime>(vinculo);
        var service = NovoService(campeonato, convites: convites);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ResponderConviteCampeonato(vinculo.Id, orgTimeId, true));
    }

    // ─── IniciarCampeonato ───────────────────────────────────────────────────

    [Fact]
    public async Task IniciarCampeonato_ComTimesConfirmados_NotificaOrganizadoresDosTimes()
    {
        var campeonato = TestEntities.Campeonato(Guid.NewGuid());
        campeonato.FormatoCampeonato = EnumFormatoCampeonato.MataMata;
        // EnumStatusCampeonato is computed from the dates once out of Rascunho/Cancelado
        // (see Campeonato.cs), so DataInicio must already be underway for the post-Iniciar
        // status to read back as EmAndamento instead of InscricoesAbertas.
        campeonato.DataInicio = DateTime.Today.AddDays(-1);
        var usuarioOrgTimeId = Guid.NewGuid();
        var time = new Time { Id = Guid.NewGuid(), Nome = "A", Cidade = "SP", Estado = "SP", OrganizadorTime = new OrganizadorTime { UsuarioId = usuarioOrgTimeId } };
        campeonato.CampeonatoTimes = new List<CampeonatoTime>
        {
            new() { Id = Guid.NewGuid(), CampeonatoId = campeonato.Id, TimeId = time.Id, EnumStatusParticipacao = EnumStatusParticipacao.Aceito }
        };
        var notificacoes = new FakeNotificacaoService();
        var service = NovoService(campeonato, times: new InMemoryTimeRepo(time), notificacoes: notificacoes);

        var resultado = await service.IniciarCampeonato(campeonato.Id);

        Assert.Equal(EnumStatusCampeonato.EmAndamento, resultado.EnumStatusCampeonato);
        Assert.Contains(notificacoes.Criadas, n => n.UsuarioId == usuarioOrgTimeId);
    }

    [Fact]
    public async Task IniciarCampeonato_PontosCorridosComMenosDeOitoTimes_Lanca()
    {
        var campeonato = TestEntities.Campeonato(Guid.NewGuid());
        campeonato.FormatoCampeonato = EnumFormatoCampeonato.PontosCorridos;
        campeonato.CampeonatoTimes = Enumerable.Range(0, 3)
            .Select(_ => new CampeonatoTime { Id = Guid.NewGuid(), CampeonatoId = campeonato.Id, TimeId = Guid.NewGuid(), EnumStatusParticipacao = EnumStatusParticipacao.Aceito })
            .ToList();
        var service = NovoService(campeonato);

        await Assert.ThrowsAsync<Exception>(() => service.IniciarCampeonato(campeonato.Id));
    }

    // ─── EditarCampeonato ────────────────────────────────────────────────────

    [Fact]
    public async Task EditarCampeonato_CampeonatoInexistente_Lanca()
    {
        // FakeCampeonatoRepository ignores the id and always returns its configured entity
        // (or null), so the "not found" branch needs it configured with no campeonato at all
        // rather than going through NovoService's helper.
        var service = new CampeonatoService(
            new InMemoryRepo<Campeonato>(), new InMemoryRepo<CampeonatoTime>(), new InMemoryTimeRepo(),
            new InMemoryRepo<Partida>(), new FakeCampeonatoRepository(null));

        await Assert.ThrowsAsync<Exception>(() => service.EditarCampeonato(Guid.NewGuid(), DtoValido()));
    }

    [Fact]
    public async Task EditarCampeonato_ComLogoUrl_AtualizaLogo()
    {
        var campeonato = TestEntities.Campeonato(Guid.NewGuid());
        var service = NovoService(campeonato);
        var dto = DtoValido();
        dto.LogoUrl = "https://example.test/novo-logo.png";

        var resultado = await service.EditarCampeonato(campeonato.Id, dto);

        Assert.Equal(dto.LogoUrl, resultado.LogoUrl);
    }

    // ─── DeletarCampeonatoAdmin ──────────────────────────────────────────────

    [Fact]
    public async Task DeletarCampeonatoAdmin_RemovePartidasEVinculos()
    {
        var campeonato = TestEntities.Campeonato(Guid.NewGuid());
        var partida = new Partida { Id = Guid.NewGuid(), CampeonatoId = campeonato.Id };
        var vinculo = new CampeonatoTime { Id = Guid.NewGuid(), CampeonatoId = campeonato.Id, TimeId = Guid.NewGuid() };
        var partidas = new InMemoryRepo<Partida>(partida);
        var convites = new InMemoryRepo<CampeonatoTime>(vinculo);
        var service = NovoService(campeonato, convites: convites, partidas: partidas);

        await service.DeletarCampeonatoAdmin(campeonato.Id);

        Assert.Equal(0, partidas.Count);
        Assert.Equal(0, convites.Count);
    }

    // ─── CancelarCampeonato ──────────────────────────────────────────────────

    [Fact]
    public async Task CancelarCampeonato_ComTimesVinculados_NotificaOrganizadoresDosTimes()
    {
        var usuarioOrgTimeId = Guid.NewGuid();
        var campeonato = TestEntities.Campeonato(Guid.NewGuid());
        var time = new Time { Id = Guid.NewGuid(), Nome = "A", Cidade = "SP", Estado = "SP", OrganizadorTime = new OrganizadorTime { UsuarioId = usuarioOrgTimeId } };
        campeonato.CampeonatoTimes = new List<CampeonatoTime>
        {
            new() { Id = Guid.NewGuid(), CampeonatoId = campeonato.Id, TimeId = time.Id, EnumStatusParticipacao = EnumStatusParticipacao.Aceito }
        };
        var notificacoes = new FakeNotificacaoService();
        var service = NovoService(campeonato, times: new InMemoryTimeRepo(time), notificacoes: notificacoes);

        await service.CancelarCampeonato(campeonato.Id);

        Assert.Equal(EnumStatusCampeonato.Cancelado, campeonato.EnumStatusCampeonato);
        Assert.Contains(notificacoes.Criadas, n => n.UsuarioId == usuarioOrgTimeId);
    }

    [Fact]
    public async Task CancelarCampeonato_ComPartidasPendentes_RemovePartidasNaoFinalizadas()
    {
        var campeonato = TestEntities.Campeonato(Guid.NewGuid());
        var pendente = new Partida { Id = Guid.NewGuid(), CampeonatoId = campeonato.Id, Finalizado = false };
        var finalizada = new Partida { Id = Guid.NewGuid(), CampeonatoId = campeonato.Id, Finalizado = true };
        campeonato.Partidas = new List<Partida> { pendente, finalizada };
        var service = NovoService(campeonato);

        await service.CancelarCampeonato(campeonato.Id);

        Assert.DoesNotContain(pendente, campeonato.Partidas);
        Assert.Contains(finalizada, campeonato.Partidas);
    }

    private static EditarCampeonatoDto DtoValido() => new()
    {
        EsporteId = Guid.NewGuid(),
        Nome = "Copa Editada",
        DataInicio = DateTime.Today.AddDays(5),
        DataFim = DateTime.Today.AddDays(20),
        PontosVitoria = 3,
        PontosDerrota = 0,
        PontosEmpate = 1,
        FormatoCampeonato = EnumFormatoCampeonato.PontosCorridos,
    };
}
