using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;

namespace kivoBackend.Tests.Unit.Domain;

/// <summary>
/// Campeonato.EnumStatusCampeonato is a computed property, not a plain field:
/// Rascunho/Cancelado are "sticky" (whatever was last set), but every other
/// value is re-derived from DataInicio/DataFim vs. DateTime.Now on every read.
/// This is easy to get wrong when writing service code (setting the enum to
/// EmAndamento has no lasting effect unless the dates agree), so the getter
/// itself gets a dedicated, pure test.
/// </summary>
public class CampeonatoEntityTests
{
    private static Campeonato Base() => new()
    {
        Nome = "Copa Teste",
        DataInicio = DateTime.Now.AddDays(5),
        DataFim = DateTime.Now.AddDays(20),
    };

    [Fact]
    public void Rascunho_PermaneceRascunho_IndependenteDasDatas()
    {
        var campeonato = Base();
        campeonato.DataInicio = DateTime.Now.AddDays(-30);
        campeonato.DataFim = DateTime.Now.AddDays(-10); // dentro da janela "ja finalizado"
        campeonato.EnumStatusCampeonato = EnumStatusCampeonato.Rascunho;

        Assert.Equal(EnumStatusCampeonato.Rascunho, campeonato.EnumStatusCampeonato);
    }

    [Fact]
    public void Cancelado_PermaneceCancelado_IndependenteDasDatas()
    {
        var campeonato = Base();
        campeonato.DataInicio = DateTime.Now.AddDays(-30);
        campeonato.DataFim = DateTime.Now.AddDays(30); // dentro da janela "em andamento"
        campeonato.EnumStatusCampeonato = EnumStatusCampeonato.Cancelado;

        Assert.Equal(EnumStatusCampeonato.Cancelado, campeonato.EnumStatusCampeonato);
    }

    [Fact]
    public void ForaDeRascunhoOuCancelado_AntesDaDataInicio_ReportaInscricoesAbertas()
    {
        var campeonato = Base();
        campeonato.DataInicio = DateTime.Now.AddDays(5);
        campeonato.DataFim = DateTime.Now.AddDays(20);
        campeonato.EnumStatusCampeonato = EnumStatusCampeonato.EmAndamento; // valor ignorado pelo getter

        Assert.Equal(EnumStatusCampeonato.InscricoesAbertas, campeonato.EnumStatusCampeonato);
    }

    [Fact]
    public void ForaDeRascunhoOuCancelado_EntreDataInicioEDataFim_ReportaEmAndamento()
    {
        var campeonato = Base();
        campeonato.DataInicio = DateTime.Now.AddDays(-1);
        campeonato.DataFim = DateTime.Now.AddDays(30);
        campeonato.EnumStatusCampeonato = EnumStatusCampeonato.InscricoesAbertas; // valor ignorado pelo getter

        Assert.Equal(EnumStatusCampeonato.EmAndamento, campeonato.EnumStatusCampeonato);
    }

    [Fact]
    public void ForaDeRascunhoOuCancelado_ApasDataFim_ReportaFinalizado()
    {
        var campeonato = Base();
        campeonato.DataInicio = DateTime.Now.AddDays(-30);
        campeonato.DataFim = DateTime.Now.AddDays(-1);
        campeonato.EnumStatusCampeonato = EnumStatusCampeonato.EmAndamento; // valor ignorado pelo getter

        Assert.Equal(EnumStatusCampeonato.Finalizado, campeonato.EnumStatusCampeonato);
    }

    [Fact]
    public void SairDeCanceladoParaInscricoesAbertas_VoltaAObedecerAsDatas()
    {
        // Simula DescancelarCampeonato: depois de sair de Cancelado, o status
        // volta a ser computado normalmente pelas datas.
        var campeonato = Base();
        campeonato.DataInicio = DateTime.Now.AddDays(-1);
        campeonato.DataFim = DateTime.Now.AddDays(30);
        campeonato.EnumStatusCampeonato = EnumStatusCampeonato.Cancelado;
        Assert.Equal(EnumStatusCampeonato.Cancelado, campeonato.EnumStatusCampeonato);

        campeonato.EnumStatusCampeonato = EnumStatusCampeonato.InscricoesAbertas;

        Assert.Equal(EnumStatusCampeonato.EmAndamento, campeonato.EnumStatusCampeonato);
    }
}
