using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Application.DTO
{
    public class ListarTimeDto
    {
        public Guid Id { get; set; }
        public string Nome { get; set; }
        public string Cidade { get; set; }
        public string Estado { get; set; }
        public string LogoUrl { get; set; }
        public bool Ativo { get; set; }
        public DateTime CriadoEm { get; set; }
        public Guid OrganizadorTimeId { get; set; }
    }
}
