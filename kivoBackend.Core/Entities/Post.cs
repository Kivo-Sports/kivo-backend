namespace kivoBackend.Core.Entities
{
    /// <summary>
    /// Publicação exibida no feed. Pode ser um texto, uma imagem, ou ambos.
    /// </summary>
    public class Post
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AutorId { get; set; }
        public string? Titulo { get; set; }
        public string? Conteudo { get; set; }
        public string? ImagemUrl { get; set; }
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
        public DateTime? AtualizadoEm { get; set; }

        public Usuario Autor { get; set; } = null!;
    }
}
