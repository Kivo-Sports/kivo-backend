using kivoBackend.Core.Enums;
using System;
using System.Collections.Generic;

namespace kivoBackend.Application.DTO
{
    public class FavoritoRequestDTO
    {
        public EnumTipoFavorito Tipo { get; set; }
        public Guid ItemId { get; set; }
    }

    public class FavoritoTimeDTO
    {
        public Guid Id { get; set; }
        public string Nome { get; set; }
        public string? LogoUrl { get; set; }
        public string Cidade { get; set; }
        public string Estado { get; set; }
        public string? EsporteNome { get; set; }
        public string? EsporteIcone { get; set; }
    }

    public class FavoritoCampeonatoDTO
    {
        public Guid Id { get; set; }
        public string Nome { get; set; }
        public string? LogoUrl { get; set; }
        public string Status { get; set; }
        public string? EsporteNome { get; set; }
        public string? EsporteIcone { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
    }

    public class FavoritosResponseDTO
    {
        public List<FavoritoTimeDTO> Times { get; set; } = new();
        public List<FavoritoCampeonatoDTO> Campeonatos { get; set; } = new();
    }

    public class TimelineItemDTO
    {
        public Guid PartidaId { get; set; }
        public Guid CampeonatoId { get; set; }
        public string CampeonatoNome { get; set; }
        public string TimeCasa { get; set; }
        public string TimeVisitante { get; set; }
        public string? LogoCasa { get; set; }
        public string? LogoVisitante { get; set; }
        public DateTime? DataHora { get; set; }
        public string? Local { get; set; }
        public string Origem { get; set; }
    }
}
