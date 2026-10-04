using kivoBackend.Application.DTO;

namespace kivoBackend.Application.Interfaces;

public interface IRelatorioService
{
    Task<RelatorioOrganizadorCampeonatoDTO> ObterOrganizadorCampeonatoAsync(Guid usuarioId, FiltroRelatorioDTO filtro);
    Task<RelatorioOrganizadorTimeDTO> ObterOrganizadorTimeAsync(Guid usuarioId, FiltroRelatorioDTO filtro);
    Task<RelatorioAdministrativoDTO> ObterAdministrativoAsync(FiltroRelatorioDTO filtro);
    Task<RelatorioTorcedorDTO> ObterTorcedorAsync(Guid usuarioId, FiltroRelatorioDTO filtro);
}
