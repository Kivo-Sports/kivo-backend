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
    public class TimeService : ServiceGenerics<Time>, ITimeService
    {
        // Passa o IRepositoryTime para a base para que ObterTodos/ObterPorId tragam o Esporte (Include).
        public TimeService(IRepositoryTime timeRepository) : base(timeRepository)
        {
        }
    }
}
