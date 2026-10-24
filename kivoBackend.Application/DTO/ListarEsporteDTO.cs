using System;

namespace kivoBackend.Application.DTO
{
    public class ListarEsporteDto
    {
        public Guid Id { get; set; }
        public string Nome { get; set; }
        public string Icone { get; set; }
        public bool Ativo { get; set; }
        public DateTime CriadoEm { get; set; }
    }
}
