using kivoBackend.Core.Enums;

namespace kivoBackend.Application.DTO
{
    public class ListarPostDto
    {
        public Guid Id { get; set; }
        public Guid AutorId { get; set; }
        public string AutorNome { get; set; } = string.Empty;
        public string? AutorImagemUrl { get; set; }
        public EnumTipoAutorPost TipoAutorExibicao { get; set; }
        public Guid? EntidadeAutorId { get; set; }
        public string? Titulo { get; set; }
        public string? Conteudo { get; set; }
        public string? ImagemUrl { get; set; }
        public DateTime CriadoEm { get; set; }
        public DateTime? AtualizadoEm { get; set; }
    }
}
