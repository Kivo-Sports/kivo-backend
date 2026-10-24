using kivoBackend.Application.Services;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using kivoBackend.Tests.TestSupport;

namespace kivoBackend.Tests.Unit.Domain;

public class FavoritoServiceTests
{
    private static (FavoritoService Service, InMemoryRepo<Favorito> Favoritos, InMemoryRepo<Time> Times,
        InMemoryRepo<Partida> Partidas, FakeCampeonatoRepository Campeonatos) CriarServico(
            Time[]? times = null, Campeonato? campeonato = null, Partida[]? partidas = null, Favorito[]? favoritos = null)
    {
        var favoritoRepo = new InMemoryRepo<Favorito>(favoritos ?? Array.Empty<Favorito>());
        var timeRepo = new InMemoryRepo<Time>(times ?? Array.Empty<Time>());
        var partidaRepo = new InMemoryRepo<Partida>(partidas ?? Array.Empty<Partida>());
        var campeonatoRepo = new FakeCampeonatoRepository(campeonato);

        var service = new FavoritoService(favoritoRepo, timeRepo, partidaRepo, campeonatoRepo);
        return (service, favoritoRepo, timeRepo, partidaRepo, campeonatoRepo);
    }

    private static Time NovoTime() => new() { Id = Guid.NewGuid(), Nome = "Time A", Cidade = "SP", Estado = "SP", Ativo = true };

    [Fact]
    public async Task Adicionar_TimeValido_Favorita()
    {
        var time = NovoTime();
        var (service, favoritos, _, _, _) = CriarServico(times: new[] { time });

        await service.Adicionar(Guid.NewGuid(), EnumTipoFavorito.Time, time.Id);

        Assert.Equal(1, favoritos.Count);
    }

    [Fact]
    public async Task Adicionar_ItemIdVazio_Lanca()
    {
        var (service, _, _, _, _) = CriarServico();

        await Assert.ThrowsAsync<ArgumentException>(() => service.Adicionar(Guid.NewGuid(), EnumTipoFavorito.Time, Guid.Empty));
    }

    [Fact]
    public async Task Adicionar_TimeInexistente_Lanca()
    {
        var (service, _, _, _, _) = CriarServico();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.Adicionar(Guid.NewGuid(), EnumTipoFavorito.Time, Guid.NewGuid()));
    }

    [Fact]
    public async Task Adicionar_CampeonatoInexistente_Lanca()
    {
        var (service, _, _, _, _) = CriarServico(campeonato: null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.Adicionar(Guid.NewGuid(), EnumTipoFavorito.Campeonato, Guid.NewGuid()));
    }

    [Fact]
    public async Task Adicionar_TipoInvalido_Lanca()
    {
        var (service, _, _, _, _) = CriarServico();

        await Assert.ThrowsAsync<ArgumentException>(() => service.Adicionar(Guid.NewGuid(), (EnumTipoFavorito)999, Guid.NewGuid()));
    }

    [Fact]
    public async Task Adicionar_JaFavoritado_NaoDuplica()
    {
        var time = NovoTime();
        var usuarioId = Guid.NewGuid();
        var existente = new Favorito { Id = Guid.NewGuid(), UsuarioId = usuarioId, Tipo = EnumTipoFavorito.Time, ItemId = time.Id };
        var (service, favoritos, _, _, _) = CriarServico(times: new[] { time }, favoritos: new[] { existente });

        await service.Adicionar(usuarioId, EnumTipoFavorito.Time, time.Id);

        Assert.Equal(1, favoritos.Count);
    }

    [Fact]
    public async Task Remover_Existente_Remove()
    {
        var usuarioId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var favorito = new Favorito { Id = Guid.NewGuid(), UsuarioId = usuarioId, Tipo = EnumTipoFavorito.Time, ItemId = itemId };
        var (service, favoritos, _, _, _) = CriarServico(favoritos: new[] { favorito });

        await service.Remover(usuarioId, EnumTipoFavorito.Time, itemId);

        Assert.Equal(0, favoritos.Count);
    }

    [Fact]
    public async Task Remover_Inexistente_NaoLanca()
    {
        var (service, _, _, _, _) = CriarServico();

        await service.Remover(Guid.NewGuid(), EnumTipoFavorito.Time, Guid.NewGuid());
    }

    [Fact]
    public async Task ListarPorUsuario_SemFavoritos_RetornaListasVazias()
    {
        var (service, _, _, _, _) = CriarServico();

        var resultado = await service.ListarPorUsuario(Guid.NewGuid());

        Assert.Empty(resultado.Times);
        Assert.Empty(resultado.Campeonatos);
    }

    [Fact]
    public async Task ListarPorUsuario_ComTimeECampeonatoFavoritados_RetornaAmbos()
    {
        var usuarioId = Guid.NewGuid();
        var time = NovoTime();
        var campeonato = TestEntities.Campeonato(Guid.NewGuid());
        var favoritos = new[]
        {
            new Favorito { Id = Guid.NewGuid(), UsuarioId = usuarioId, Tipo = EnumTipoFavorito.Time, ItemId = time.Id },
            new Favorito { Id = Guid.NewGuid(), UsuarioId = usuarioId, Tipo = EnumTipoFavorito.Campeonato, ItemId = campeonato.Id },
        };
        var (service, _, _, _, _) = CriarServico(times: new[] { time }, campeonato: campeonato, favoritos: favoritos);

        var resultado = await service.ListarPorUsuario(usuarioId);

        Assert.Single(resultado.Times);
        Assert.Single(resultado.Campeonatos);
    }

    [Fact]
    public async Task ObterTimeline_SemFavoritos_RetornaVazio()
    {
        var (service, _, _, _, _) = CriarServico();

        var resultado = await service.ObterTimeline(Guid.NewGuid());

        Assert.Empty(resultado);
    }

    [Fact]
    public async Task ObterTimeline_PartidaFuturaDoTimeFavorito_Aparece()
    {
        var usuarioId = Guid.NewGuid();
        var time = NovoTime();
        var partida = new Partida
        {
            Id = Guid.NewGuid(),
            CampeonatoId = Guid.NewGuid(),
            TimeCasaId = time.Id,
            TimeCasa = time,
            DataHora = DateTime.Now.AddDays(2),
            Finalizado = false,
            Local = "Estadio X"
        };
        var favoritos = new[] { new Favorito { Id = Guid.NewGuid(), UsuarioId = usuarioId, Tipo = EnumTipoFavorito.Time, ItemId = time.Id } };
        var (service, _, _, _, _) = CriarServico(times: new[] { time }, partidas: new[] { partida }, favoritos: favoritos);

        var resultado = await service.ObterTimeline(usuarioId);

        Assert.Single(resultado);
        Assert.Contains("Time A", resultado[0].Origem);
    }

    [Fact]
    public async Task ObterTimeline_PartidaFinalizadaOuPassada_NaoAparece()
    {
        var usuarioId = Guid.NewGuid();
        var time = NovoTime();
        var finalizada = new Partida { Id = Guid.NewGuid(), CampeonatoId = Guid.NewGuid(), TimeCasaId = time.Id, DataHora = DateTime.Now.AddDays(2), Finalizado = true };
        var passada = new Partida { Id = Guid.NewGuid(), CampeonatoId = Guid.NewGuid(), TimeCasaId = time.Id, DataHora = DateTime.Now.AddDays(-2), Finalizado = false };
        var favoritos = new[] { new Favorito { Id = Guid.NewGuid(), UsuarioId = usuarioId, Tipo = EnumTipoFavorito.Time, ItemId = time.Id } };
        var (service, _, _, _, _) = CriarServico(times: new[] { time }, partidas: new[] { finalizada, passada }, favoritos: favoritos);

        var resultado = await service.ObterTimeline(usuarioId);

        Assert.Empty(resultado);
    }
}
