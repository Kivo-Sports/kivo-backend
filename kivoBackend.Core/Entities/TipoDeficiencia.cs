using System;

namespace kivoBackend.Core.Entities
{
    public class TipoDeficiencia
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Descricao { get; set; } = string.Empty;
    }

    public static class TipoDeficienciaSeed
    {
        public static readonly Guid NenhumaId = Guid.Parse("d1a1b2c3-0001-4000-8000-000000000001");
        public static readonly Guid AuditivaId = Guid.Parse("d1a1b2c3-0002-4000-8000-000000000002");
        public static readonly Guid CadeiranteId = Guid.Parse("d1a1b2c3-0003-4000-8000-000000000003");
    }
}
