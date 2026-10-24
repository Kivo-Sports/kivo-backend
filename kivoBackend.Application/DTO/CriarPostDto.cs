using kivoBackend.Core.Enums;

namespace kivoBackend.Application.DTO
{
    public class CriarPostDto
    {
        public EnumTipoAutorPost TipoAutorExibicao { get; set; }
        public Guid? EntidadeAutorId { get; set; }
        public string? Titulo { get; set; }
        public string? Conteudo { get; set; }
    }
}
