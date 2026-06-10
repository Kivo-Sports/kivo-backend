using kivoBackend.Application.DTO;
using kivoBackend.Core.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace kivoBackend.Application.Interfaces
{
    public interface IFavoritoService
    {
        Task Adicionar(Guid usuarioId, EnumTipoFavorito tipo, Guid itemId);
        Task Remover(Guid usuarioId, EnumTipoFavorito tipo, Guid itemId);
        Task<FavoritosResponseDTO> ListarPorUsuario(Guid usuarioId);
        Task<List<TimelineItemDTO>> ObterTimeline(Guid usuarioId);
    }
}
