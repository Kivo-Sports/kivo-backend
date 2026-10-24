using kivoBackend.Application.DTO;
using kivoBackend.Application.Services;
using kivoBackend.Core.Entities;
using kivoBackend.Presentation.Controller;
using kivoBackend.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;

namespace kivoBackend.Tests.Unit.Auth;

public class EsporteControllerTests
{
    private static Esporte NovoEsporte(bool ativo = true) => new() { Id = Guid.NewGuid(), Nome = "Futebol", Icone = "mdi:soccer", Ativo = ativo, CriadoEm = DateTime.Now };

    [Fact]
    public async Task GetAll_RetornaOrdenadoPorNome()
    {
        var b = new Esporte { Id = Guid.NewGuid(), Nome = "Basquete", Icone = "mdi:basketball" };
        var f = new Esporte { Id = Guid.NewGuid(), Nome = "Futebol", Icone = "mdi:soccer" };
        var controller = new EsporteController(new ServiceGenerics<Esporte>(new InMemoryRepo<Esporte>(f, b)));

        var result = Assert.IsType<OkObjectResult>(await controller.GetAll());
        var lista = Assert.IsAssignableFrom<IEnumerable<ListarEsporteDto>>(result.Value).ToList();
        Assert.Equal("Basquete", lista[0].Nome);
    }

    [Fact]
    public async Task GetById_Inexistente_RetornaNotFound()
    {
        var controller = new EsporteController(new ServiceGenerics<Esporte>(new InMemoryRepo<Esporte>()));

        Assert.IsType<NotFoundObjectResult>(await controller.GetById(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetById_Existente_RetornaOk()
    {
        var esporte = NovoEsporte();
        var controller = new EsporteController(new ServiceGenerics<Esporte>(new InMemoryRepo<Esporte>(esporte)));

        var result = Assert.IsType<OkObjectResult>(await controller.GetById(esporte.Id));
        Assert.Equal(esporte.Id, Assert.IsType<ListarEsporteDto>(result.Value).Id);
    }

    [Fact]
    public async Task Post_NomeVazio_RetornaBadRequest()
    {
        var controller = new EsporteController(new ServiceGenerics<Esporte>(new InMemoryRepo<Esporte>()));

        Assert.IsType<BadRequestObjectResult>(await controller.Post(new CriarEsporteDto { Nome = "", Icone = "mdi:x" }));
    }

    [Fact]
    public async Task Post_IconeVazio_RetornaBadRequest()
    {
        var controller = new EsporteController(new ServiceGenerics<Esporte>(new InMemoryRepo<Esporte>()));

        Assert.IsType<BadRequestObjectResult>(await controller.Post(new CriarEsporteDto { Nome = "Vôlei", Icone = "" }));
    }

    [Fact]
    public async Task Post_DadosValidos_RetornaCreated()
    {
        var repo = new InMemoryRepo<Esporte>();
        var controller = new EsporteController(new ServiceGenerics<Esporte>(repo));

        var result = Assert.IsType<CreatedAtActionResult>(await controller.Post(new CriarEsporteDto { Nome = " Vôlei ", Icone = " mdi:volleyball " }));
        var dto = Assert.IsType<ListarEsporteDto>(result.Value);
        Assert.Equal("Vôlei", dto.Nome);
        Assert.True(dto.Ativo);
        Assert.Equal(1, repo.Count);
    }

    [Fact]
    public async Task Put_Inexistente_RetornaNotFound()
    {
        var controller = new EsporteController(new ServiceGenerics<Esporte>(new InMemoryRepo<Esporte>()));

        Assert.IsType<NotFoundObjectResult>(await controller.Put(Guid.NewGuid(), new EditarEsporteDto { Nome = "X", Icone = "y" }));
    }

    [Fact]
    public async Task Put_NomeVazio_RetornaBadRequest()
    {
        var esporte = NovoEsporte();
        var controller = new EsporteController(new ServiceGenerics<Esporte>(new InMemoryRepo<Esporte>(esporte)));

        Assert.IsType<BadRequestObjectResult>(await controller.Put(esporte.Id, new EditarEsporteDto { Nome = "", Icone = "x" }));
    }

    [Fact]
    public async Task Put_DadosValidos_Atualiza()
    {
        var esporte = NovoEsporte();
        var controller = new EsporteController(new ServiceGenerics<Esporte>(new InMemoryRepo<Esporte>(esporte)));

        var result = Assert.IsType<OkObjectResult>(await controller.Put(esporte.Id, new EditarEsporteDto { Nome = "Futsal", Icone = "mdi:futsal", Ativo = false }));
        var dto = Assert.IsType<ListarEsporteDto>(result.Value);
        Assert.Equal("Futsal", dto.Nome);
        Assert.False(dto.Ativo);
    }

    [Fact]
    public async Task ToggleStatus_Inexistente_RetornaNotFound()
    {
        var controller = new EsporteController(new ServiceGenerics<Esporte>(new InMemoryRepo<Esporte>()));

        Assert.IsType<NotFoundObjectResult>(await controller.ToggleStatus(Guid.NewGuid()));
    }

    [Fact]
    public async Task ToggleStatus_Existente_InverteAtivo()
    {
        var esporte = NovoEsporte(ativo: true);
        var controller = new EsporteController(new ServiceGenerics<Esporte>(new InMemoryRepo<Esporte>(esporte)));

        var result = Assert.IsType<OkObjectResult>(await controller.ToggleStatus(esporte.Id));
        Assert.False(Assert.IsType<ListarEsporteDto>(result.Value).Ativo);
    }

    [Fact]
    public async Task Delete_Inexistente_RetornaNotFound()
    {
        var controller = new EsporteController(new ServiceGenerics<Esporte>(new InMemoryRepo<Esporte>()));

        Assert.IsType<NotFoundObjectResult>(await controller.Delete(Guid.NewGuid()));
    }

    [Fact]
    public async Task Delete_SemVinculos_RemoveComSucesso()
    {
        var esporte = NovoEsporte();
        var controller = new EsporteController(new ServiceGenerics<Esporte>(new InMemoryRepo<Esporte>(esporte)));

        Assert.IsType<NoContentResult>(await controller.Delete(esporte.Id));
    }

    [Fact]
    public async Task Delete_ComVinculos_RetornaBadRequestComMensagemAmigavel()
    {
        var esporte = NovoEsporte();
        var repo = new ThrowingOnRemoveRepo(esporte);
        var controller = new EsporteController(new ServiceGenerics<Esporte>(repo));

        var result = Assert.IsType<BadRequestObjectResult>(await controller.Delete(esporte.Id));
        Assert.Contains("Desative-o", result.Value!.ToString());
    }

    /// <summary>Simulates the FK-restrict DbUpdateException a real EsporteRepository would
    /// throw when the sport is still referenced by a Time/Campeonato.</summary>
    private sealed class ThrowingOnRemoveRepo : EmptyRepo<Esporte>
    {
        private readonly Esporte _esporte;
        public ThrowingOnRemoveRepo(Esporte esporte) => _esporte = esporte;
        public override Task<Esporte?> ObterPorId(Guid id) => Task.FromResult(id == _esporte.Id ? _esporte : null);
        public override Task Remover(Guid id) => throw new InvalidOperationException("FK constraint violated.");
    }
}
