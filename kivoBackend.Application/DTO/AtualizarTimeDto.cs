namespace kivoBackend.Application.DTO
{
    public class AtualizarTimeDto
    {
        public Guid EsporteId { get; set; }
        public string Nome { get; set; }
        public string Cidade { get; set; }
        public string Estado { get; set; }
        public string? LogoUrl { get; set; }
    }
}
