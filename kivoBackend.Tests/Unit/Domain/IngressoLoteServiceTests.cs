using kivoBackend.Application.DTO;
using kivoBackend.Application.Services;
using kivoBackend.Core.Entities;
using kivoBackend.Tests.TestSupport;

namespace kivoBackend.Tests.Unit.Domain;

public class IngressoLoteServiceTests
{
    private static (IngressoLoteService Service, InMemoryRepo<IngressoLote> Lotes, InMemoryRepo<Favorito> Favoritos,
        FakeNotificacaoService Notificacoes) CriarServico(Partida partida, Campeonato campeonato, Time[]? times = null, Favorito[]? favoritos = null)
    {
        var loteRepo = new InMemoryRepo<IngressoLote>();
        var favoritoRepo = new InMemoryRepo<Favorito>(favoritos ?? Array.Empty<Favorito>());
        var partidaRepo = new InMemoryRepo<Partida>(partida);
        var notificacoes = new FakeNotificacaoService();
        var timeRepo = new InMemoryTimeRepo(times ?? Array.Empty<Time>());
        var campeonatoRepo = new FakeCampeonatoRepository(campeonato);

        var service = new IngressoLoteService(loteRepo, favoritoRepo, partidaRepo, notificacoes, timeRepo, campeonatoRepo);
        return (service, loteRepo, favoritoRepo, notificacoes);
    }

    private static (Campeonato Campeonato, Partida Partida) NovoCenario(Guid organizadorId)
    {
        var campeonato = TestEntities.Campeonato(organizadorId);
        var partida = new Partida { Id = Guid.NewGuid(), CampeonatoId = campeonato.Id, Local = "Estadio" };
        return (campeonato, partida);
    }

    [Fact]
    public async Task CriarLote_OwnerCorreto_Cria()
    {
        var organizadorId = Guid.NewGuid();
        var (campeonato, partida) = NovoCenario(organizadorId);
        var (service, lotes, _, _) = CriarServico(partida, campeonato);

        var dto = new CriarIngressoLoteDTO { PartidaId = partida.Id, NomeLote = "Pista", Preco = 50, QuantidadeTotal = 100 };

        var resultado = await service.CriarLote(dto, organizadorId, ehAdmin: false);

        Assert.Equal(1, lotes.Count);
        Assert.Equal(100, resultado.QuantidadeDisponivel);
        Assert.True(resultado.Ativo);
    }

    [Fact]
    public async Task CriarLote_OrganizadorErrado_LancaUnauthorized()
    {
        var (campeonato, partida) = NovoCenario(Guid.NewGuid());
        var (service, _, _, _) = CriarServico(partida, campeonato);

        var dto = new CriarIngressoLoteDTO { PartidaId = partida.Id, NomeLote = "Pista", Preco = 50, QuantidadeTotal = 100 };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CriarLote(dto, Guid.NewGuid(), ehAdmin: false));
    }

    [Fact]
    public async Task CriarLote_Admin_IgnoraOwnership()
    {
        var (campeonato, partida) = NovoCenario(Guid.NewGuid());
        var (service, lotes, _, _) = CriarServico(partida, campeonato);

        var dto = new CriarIngressoLoteDTO { PartidaId = partida.Id, NomeLote = "Pista", Preco = 50, QuantidadeTotal = 100 };

        await service.CriarLote(dto, Guid.NewGuid(), ehAdmin: true);

        Assert.Equal(1, lotes.Count);
    }

    [Fact]
    public async Task CriarLote_PartidaInexistente_Lanca()
    {
        var (campeonato, partida) = NovoCenario(Guid.NewGuid());
        var (service, _, _, _) = CriarServico(partida, campeonato);

        var dto = new CriarIngressoLoteDTO { PartidaId = Guid.NewGuid(), NomeLote = "Pista", Preco = 50, QuantidadeTotal = 100 };

        await Assert.ThrowsAsync<Exception>(() => service.CriarLote(dto, Guid.NewGuid(), ehAdmin: true));
    }

    [Fact]
    public async Task CriarLote_CampeonatoNaoEncontrado_Lanca()
    {
        var partida = new Partida { Id = Guid.NewGuid(), CampeonatoId = Guid.NewGuid() };
        var (service, _, _, _) = CriarServico(partida, campeonato: null!);

        var dto = new CriarIngressoLoteDTO { PartidaId = partida.Id, NomeLote = "Pista", Preco = 50, QuantidadeTotal = 100 };

        await Assert.ThrowsAsync<Exception>(() => service.CriarLote(dto, Guid.NewGuid(), ehAdmin: true));
    }

    [Fact]
    public async Task CriarLote_QuantidadeZero_Lanca()
    {
        var organizadorId = Guid.NewGuid();
        var (campeonato, partida) = NovoCenario(organizadorId);
        var (service, _, _, _) = CriarServico(partida, campeonato);

        var dto = new CriarIngressoLoteDTO { PartidaId = partida.Id, NomeLote = "Pista", Preco = 50, QuantidadeTotal = 0 };

        await Assert.ThrowsAsync<Exception>(() => service.CriarLote(dto, organizadorId, ehAdmin: false));
    }

    [Fact]
    public async Task CriarLote_PrecoZero_Lanca()
    {
        var organizadorId = Guid.NewGuid();
        var (campeonato, partida) = NovoCenario(organizadorId);
        var (service, _, _, _) = CriarServico(partida, campeonato);

        var dto = new CriarIngressoLoteDTO { PartidaId = partida.Id, NomeLote = "Pista", Preco = 0, QuantidadeTotal = 10 };

        await Assert.ThrowsAsync<Exception>(() => service.CriarLote(dto, organizadorId, ehAdmin: false));
    }

    [Fact]
    public async Task CriarLote_TimeComFavoritos_NotificaTorcedores()
    {
        var organizadorId = Guid.NewGuid();
        var campeonato = TestEntities.Campeonato(organizadorId);
        var time = new Time { Id = Guid.NewGuid(), Nome = "Time A", Cidade = "SP", Estado = "SP", Ativo = true };
        var partida = new Partida { Id = Guid.NewGuid(), CampeonatoId = campeonato.Id, TimeCasaId = time.Id, Local = "Estadio" };
        var torcedorId = Guid.NewGuid();
        var favorito = new Favorito { Id = Guid.NewGuid(), UsuarioId = torcedorId, ItemId = time.Id, Tipo = Core.Enums.EnumTipoFavorito.Time };
        var (service, _, _, notificacoes) = CriarServico(partida, campeonato, times: new[] { time }, favoritos: new[] { favorito });

        var dto = new CriarIngressoLoteDTO { PartidaId = partida.Id, NomeLote = "Pista", Preco = 50, QuantidadeTotal = 100 };
        await service.CriarLote(dto, organizadorId, ehAdmin: false);

        Assert.Contains(notificacoes.Criadas, n => n.UsuarioId == torcedorId);
    }

    [Fact]
    public async Task CriarLote_SemTimesNaPartida_NaoNotificaNinguem()
    {
        var organizadorId = Guid.NewGuid();
        var (campeonato, partida) = NovoCenario(organizadorId); // sem TimeCasaId/TimeVisitanteId
        var (service, _, _, notificacoes) = CriarServico(partida, campeonato);

        var dto = new CriarIngressoLoteDTO { PartidaId = partida.Id, NomeLote = "Pista", Preco = 50, QuantidadeTotal = 100 };
        await service.CriarLote(dto, organizadorId, ehAdmin: false);

        Assert.Empty(notificacoes.Criadas);
    }

    [Fact]
    public async Task ObterLotesPorPartida_RetornaApenasAtivosDaPartida()
    {
        var organizadorId = Guid.NewGuid();
        var (campeonato, partida) = NovoCenario(organizadorId);
        var (service, lotes, _, _) = CriarServico(partida, campeonato);
        await lotes.Adicionar(new IngressoLote { Id = Guid.NewGuid(), PartidaId = partida.Id, Ativo = true, NomeLote = "A" });
        await lotes.Adicionar(new IngressoLote { Id = Guid.NewGuid(), PartidaId = partida.Id, Ativo = false, NomeLote = "B" });
        await lotes.Adicionar(new IngressoLote { Id = Guid.NewGuid(), PartidaId = Guid.NewGuid(), Ativo = true, NomeLote = "C" });

        var resultado = await service.ObterLotesPorPartida(partida.Id);

        Assert.Single(resultado);
        Assert.Equal("A", resultado.First().NomeLote);
    }
}
