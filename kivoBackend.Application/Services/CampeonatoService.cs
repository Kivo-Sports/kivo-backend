using kivoBackend.Application.Interfaces;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Application.Services
{
    public class CampeonatoService : ServiceGenerics<Campeonato>, ICampeonatoService    
    {
        public CampeonatoService(IRepositoryGenerics<Campeonato> repositoryGenerics, IRepositoryCampeonato RepositoryCampeonato) : base(repositoryGenerics)
        {
        }
    }
}
