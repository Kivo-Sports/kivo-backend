using kivoBackend.Core.Enums;
using System;

namespace kivoBackend.Core.Entities
{
    public class Favorito
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UsuarioId { get; set; }
        public EnumTipoFavorito Tipo { get; set; }
        public Guid ItemId { get; set; }
        public DateTime CriadoEm { get; set; } = DateTime.Now;
    }
}
