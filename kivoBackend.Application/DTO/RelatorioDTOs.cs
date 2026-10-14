namespace kivoBackend.Application.DTO;

public class FiltroRelatorioDTO
{
    public DateTime? Inicio { get; set; }
    public DateTime? Fim { get; set; }
}

public class SerieVendasDTO
{
    public DateTime Data { get; set; }
    public int Quantidade { get; set; }
    public decimal Receita { get; set; }
}

public class VendasPorPartidaDTO
{
    public Guid PartidaId { get; set; }
    public string Confronto { get; set; } = string.Empty;
    public int IngressosVendidos { get; set; }
    public int CheckIns { get; set; }
    public decimal Receita { get; set; }
}

public class OcupacaoLoteDTO
{
    public Guid LoteId { get; set; }
    public string Lote { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Vendidos { get; set; }
    public int Disponiveis { get; set; }
    public decimal OcupacaoPercentual { get; set; }
}

public class ResumoFinanceiroDTO
{
    public decimal ReceitaConfirmada { get; set; }
    public int IngressosPagos { get; set; }
    public int IngressosPendentes { get; set; }
    public int IngressosCancelados { get; set; }
    public int CheckIns { get; set; }
    public decimal TaxaComparecimento { get; set; }
}

public class RelatorioOrganizadorCampeonatoDTO
{
    public DateTime? Inicio { get; set; }
    public DateTime? Fim { get; set; }
    public ResumoFinanceiroDTO Resumo { get; set; } = new();
    public int Campeonatos { get; set; }
    public int TimesParticipantes { get; set; }
    public int PartidasRealizadas { get; set; }
    public int PartidasAgendadas { get; set; }
    public List<SerieVendasDTO> VendasPorDia { get; set; } = [];
    public List<VendasPorPartidaDTO> VendasPorPartida { get; set; } = [];
    public List<OcupacaoLoteDTO> OcupacaoPorLote { get; set; } = [];
}

public class DesempenhoTimeDTO
{
    public Guid TimeId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int Jogos { get; set; }
    public int Vitorias { get; set; }
    public int Empates { get; set; }
    public int Derrotas { get; set; }
    public int GolsPro { get; set; }
    public int GolsContra { get; set; }
    public int SaldoGols => GolsPro - GolsContra;
    public int Favoritos { get; set; }
}

public class RelatorioOrganizadorTimeDTO
{
    public DateTime? Inicio { get; set; }
    public DateTime? Fim { get; set; }
    public int Times { get; set; }
    public int CampeonatosDisputados { get; set; }
    public int JogosRealizados { get; set; }
    public int JogosAgendados { get; set; }
    public List<DesempenhoTimeDTO> DesempenhoPorTime { get; set; } = [];
}

public class ItemQuantidadeDTO
{
    public string Nome { get; set; } = string.Empty;
    public int Quantidade { get; set; }
}

public class RelatorioAdministrativoDTO
{
    public DateTime? Inicio { get; set; }
    public DateTime? Fim { get; set; }
    public int UsuariosTotais { get; set; }
    public int NovosUsuarios { get; set; }
    public int CampeonatosTotais { get; set; }
    public int CampeonatosEmAndamento { get; set; }
    public int PartidasRealizadas { get; set; }
    public int TimesAtivos { get; set; }
    public ResumoFinanceiroDTO ResumoFinanceiro { get; set; } = new();
    public List<SerieVendasDTO> VendasPorDia { get; set; } = [];
    public List<ItemQuantidadeDTO> UsuariosPorPerfil { get; set; } = [];
    public List<ItemQuantidadeDTO> CampeonatosPorEsporte { get; set; } = [];
}

public class ProximaPartidaDTO
{
    public Guid PartidaId { get; set; }
    public string Confronto { get; set; } = string.Empty;
    public DateTime? DataHora { get; set; }
    public string Local { get; set; } = string.Empty;
}

public class RelatorioTorcedorDTO
{
    public DateTime? Inicio { get; set; }
    public DateTime? Fim { get; set; }
    public decimal GastosConfirmados { get; set; }
    public int IngressosPagos { get; set; }
    public int IngressosPendentes { get; set; }
    public int IngressosUtilizados { get; set; }
    public int TimesFavoritos { get; set; }
    public int CampeonatosFavoritos { get; set; }
    public List<SerieVendasDTO> ComprasPorDia { get; set; } = [];
    public List<ProximaPartidaDTO> ProximasPartidas { get; set; } = [];
}
