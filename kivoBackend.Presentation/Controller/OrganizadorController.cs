using kivoBackend.Application.Interfaces;
using kivoBackend.Core.Entities;
using Microsoft.AspNetCore.Mvc;

namespace kivoBackend.Presentation.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrganizadorController : ControllerBase
    {

        private readonly IServiceGenerics<Organizador> _service;

        public OrganizadorController(IServiceGenerics<Organizador> service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Organizador>>> GetAll()
        {
            var organizadores = await _service.GetAll();
            return Ok(organizadores);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Organizador>> GetById(Guid id)
        {
            var organizador = await _service.GetById(id);
            if (organizador == null)
            {
                return NotFound();
            }
            return Ok(organizador);
        }

        [HttpPost]
        public async Task<ActionResult<Organizador>> Add(Organizador organizador)
        {
            var newOrganizador = await _service.Add(organizador);
            return CreatedAtAction(nameof(GetById), new { id = newOrganizador.Id }, newOrganizador);
        }

        [HttpPut("id")]
        public async Task<IActionResult> Update(Guid id, Organizador organizador)
        {
            if (id != organizador.Id)
            {
                return BadRequest();
            }
            await _service.Update(organizador);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Remove(Guid id)
        {
            await _service.Remove(id);
            return NoContent();
        }
    }   
}
