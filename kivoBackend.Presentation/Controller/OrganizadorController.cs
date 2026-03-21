using kivoBackend.Application.Interfaces;
using kivoBackend.Core.Entities;
using Microsoft.AspNetCore.Mvc;

namespace kivoBackend.Presentation.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrganizadorController : ControllerBase
    {

        private readonly IServiceGenerics<OrganizadorTime> _service;

        public OrganizadorController(IServiceGenerics<OrganizadorTime> service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrganizadorTime>>> GetAll()
        {
            var organizadores = await _service.ObterTodos();
            return Ok(organizadores);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<OrganizadorTime>> GetById(Guid id)
        {
            var organizador = await _service.ObterPorId(id);
            if (organizador == null)
            {
                return NotFound();
            }
            return Ok(organizador);
        }

        [HttpPost]
        public async Task<ActionResult<OrganizadorTime>> Add(OrganizadorTime organizador)
        {
            var newOrganizador = await _service.Adicionar(organizador);
            return CreatedAtAction(nameof(GetById), new { id = newOrganizador.Id }, newOrganizador);
        }

        [HttpPut("id")]
        public async Task<IActionResult> Update(Guid id, OrganizadorTime organizador)
        {
            if (id != organizador.Id)
            {
                return BadRequest();
            }
            await _service.Atualizar(organizador);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Remove(Guid id)
        {
            await _service.Remover(id);
            return NoContent();
        }
    }   
}
