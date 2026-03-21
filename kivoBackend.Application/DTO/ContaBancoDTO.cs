using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Application.DTO
{
    public class ContaBancoDto
    {
        public string Banco { get; set; }
        public string Agencia { get; set; }
        public string Conta { get; set; }
        public string ChavePix { get; set; }
    }
}
