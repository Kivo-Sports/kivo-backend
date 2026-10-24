using kivoBackend.Application.Services;
using kivoBackend.Core.Entities;
using kivoBackend.Tests.TestSupport;

namespace kivoBackend.Tests.Unit.Domain;

/// <summary>
/// TimeService is a thin ServiceGenerics&lt;Time&gt; passthrough — the real ownership rules
/// live in TimeController (see TimeControllerTests). This just covers the CRUD delegation.
/// </summary>
public class TimeServiceTests
{
    [Fact]
    public async Task Adicionar_DelegaParaRepositorio()
    {
        var repo = new InMemoryTimeRepo();
        var service = new TimeService(repo);
        var time = new Time { Id = Guid.NewGuid(), Nome = "A", Cidade = "SP", Estado = "SP" };

        var resultado = await service.Adicionar(time);

        Assert.Equal(time.Id, resultado.Id);
        Assert.Equal(1, repo.Count);
    }

    [Fact]
    public async Task ObterPorId_Existente_Retorna()
    {
        var time = new Time { Id = Guid.NewGuid(), Nome = "A", Cidade = "SP", Estado = "SP" };
        var service = new TimeService(new InMemoryTimeRepo(time));

        Assert.Equal(time.Id, (await service.ObterPorId(time.Id))!.Id);
    }

    [Fact]
    public async Task ObterPorId_Inexistente_RetornaNull()
    {
        var service = new TimeService(new InMemoryTimeRepo());

        Assert.Null(await service.ObterPorId(Guid.NewGuid()));
    }

    [Fact]
    public async Task ObterTodos_RetornaTodosOsTimes()
    {
        var t1 = new Time { Id = Guid.NewGuid(), Nome = "A", Cidade = "SP", Estado = "SP" };
        var t2 = new Time { Id = Guid.NewGuid(), Nome = "B", Cidade = "RJ", Estado = "RJ" };
        var service = new TimeService(new InMemoryTimeRepo(t1, t2));

        Assert.Equal(2, (await service.ObterTodos()).Count());
    }

    [Fact]
    public async Task Atualizar_DelegaParaRepositorio()
    {
        var time = new Time { Id = Guid.NewGuid(), Nome = "A", Cidade = "SP", Estado = "SP" };
        var repo = new InMemoryTimeRepo(time);
        var service = new TimeService(repo);

        time.Nome = "B";
        await service.Atualizar(time);

        Assert.Equal("B", (await repo.ObterPorId(time.Id))!.Nome);
    }

    [Fact]
    public async Task Remover_DelegaParaRepositorio()
    {
        var time = new Time { Id = Guid.NewGuid(), Nome = "A", Cidade = "SP", Estado = "SP" };
        var repo = new InMemoryTimeRepo(time);
        var service = new TimeService(repo);

        await service.Remover(time.Id);

        Assert.Equal(0, repo.Count);
    }
}
