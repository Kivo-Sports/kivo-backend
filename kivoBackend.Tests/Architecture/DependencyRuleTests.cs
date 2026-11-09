using System.Reflection;
using kivoBackend.Infrastructure.Data;

namespace kivoBackend.Tests.Architecture;

public class DependencyRuleTests
{
    private static readonly Assembly Application = typeof(kivoBackend.Application.Interfaces.IUsuarioService).Assembly;
    private static readonly Assembly Core = typeof(kivoBackend.Core.Entities.Usuario).Assembly;
    private static readonly Assembly Presentation = typeof(kivoBackend.Presentation.Controller.AuthController).Assembly;

    [Fact]
    public void Application_NaoReferenciaInfrastructureNemPresentation()
    {
        var referenciados = Application.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToList();

        Assert.DoesNotContain(referenciados, n => n.Contains("kivoBackend.Infrastructure"));
        Assert.DoesNotContain(referenciados, n => n.Contains("kivoBackend.Presentation"));
    }

    [Fact]
    public void Core_NaoReferenciaOutrosProjetosKivo()
    {
        var coreName = Core.GetName().Name;
        var referenciados = Core.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToList();

        Assert.DoesNotContain(referenciados, n => n.StartsWith("kivoBackend.") && n != coreName);
    }

    [Fact]
    public void Controllers_NaoInjetamRepositoriosNemDbContext()
    {
        var controllers = Presentation.GetTypes()
            .Where(t => t.Name.EndsWith("Controller") && !t.IsAbstract);

        var violacoes = controllers
            .SelectMany(t => t.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .SelectMany(c => c.GetParameters())
                .Where(p => p.ParameterType == typeof(AppDbContext)
                         || p.ParameterType.Name.StartsWith("IRepository"))
                .Select(p => $"{t.Name}({p.ParameterType.Name} {p.Name})"))
            .ToList();

        Assert.True(violacoes.Count == 0, "Controllers com dependência de dados: " + string.Join(", ", violacoes));
    }
}
