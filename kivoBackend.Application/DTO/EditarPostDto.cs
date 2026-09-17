namespace kivoBackend.Application.DTO
{
    public class EditarPostDto
    {
        public string? Titulo { get; set; }
        public string? Conteudo { get; set; }
        // Quando verdadeiro, remove a imagem atual caso nenhuma nova seja enviada.
        public bool RemoverImagem { get; set; }
    }
}
