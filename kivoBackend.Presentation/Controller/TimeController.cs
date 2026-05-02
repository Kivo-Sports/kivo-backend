using kivoBackend.Application.DTO;
using kivoBackend.Application.Interfaces;
using kivoBackend.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace kivoBackend.Presentation.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TimeController : ControllerBase
    {
        private readonly ITimeService _timeService;
        private readonly IUsuarioService _usuarioService;
        private readonly IStorageService _storageService;

        public TimeController(ITimeService timeService, IUsuarioService usuarioService, IStorageService storageService)
        {
            _timeService = timeService;
            _usuarioService = usuarioService;
            _storageService = storageService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var times = await _timeService.ObterTodos();
                return Ok(times.Select(t => MapearParaDto(t)));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("organizador")]
        public async Task<IActionResult> GetAllOrganizador()
        {
            try
            {
                var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdString, out var userId))
                    return Unauthorized(new { message = "Usuário não identificado." });
                
                var usuario = await _usuarioService.ObterUsuarioPorId(userId);
                var todosOsTimes = await _timeService.ObterTodos();

                if (User.IsInRole("Administrador") || User.IsInRole("Admin"))
                {
                    return Ok(todosOsTimes.Select(t => MapearParaDto(t)));
                }
                var organizador = usuario.OrganizadorTime;
                if (organizador == null)
                    return BadRequest(new { message = "Perfil de Organizador de Time não encontrado." });
                
                var meusTimes = todosOsTimes.Where(t => t.OrganizadorTimeId == organizador.Id);
                return Ok(meusTimes.Select(t => MapearParaDto(t)));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var time = await _timeService.ObterPorId(id);

                if (time == null)
                {
                    return NotFound("Time não encontrado.");
                }

                return Ok(MapearParaDto(time));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        private ListarTimeDto MapearParaDto(Time t)
        {
            return new ListarTimeDto
            {
                Id = t.Id,
                Nome = t.Nome,
                Cidade = t.Cidade,
                Estado = t.Estado,
                LogoUrl = t.LogoUrl,
                Ativo = t.Ativo,
                CriadoEm = t.CriadoEm,
                OrganizadorTimeId = t.OrganizadorTimeId
            };
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromForm] CriarTimeDto dto, IFormFile? logo)
        {
            try
            {
                if (string.IsNullOrEmpty(dto.LogoUrl) && (logo == null || logo.Length == 0))
                {
                    return BadRequest(new { message = "Você deve fornecer uma URL de imagem ou fazer o upload de um arquivo." });
                }

                string? urlImage = dto.LogoUrl;

                if (logo != null && logo.Length > 0)
                {
                    using var stream = logo.OpenReadStream();
                    urlImage = await _storageService.UploadFileAsync(stream, logo.FileName, logo.ContentType);
                }

                var novoTime = new Time
                {
                    Id = Guid.NewGuid(),
                    OrganizadorTimeId = dto.OrganizadorTimeId,
                    Nome = dto.Nome,
                    Cidade = dto.Cidade,
                    Estado = dto.Estado,
                    LogoUrl = urlImage,
                    Ativo = true,
                    CriadoEm = DateTime.Now
                };

                var resultado = await _timeService.Adicionar(novoTime);
                return CreatedAtAction(nameof(GetById), new { id = resultado.Id }, MapearParaDto(resultado));
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(Guid id, [FromForm] AtualizarTimeDto dto, IFormFile? logo)
        {
            try
            {
                var time = await _timeService.ObterPorId(id);
                if (time == null) return NotFound("Time não encontrado.");

                if(logo != null && logo.Length > 0)
                {
                    using var stream = logo.OpenReadStream();
                    time.LogoUrl = await _storageService.UploadFileAsync(stream, logo.FileName, logo.ContentType);
                }

                time.Nome = dto.Nome;
                time.Cidade = dto.Cidade;
                time.Estado = dto.Estado;

                await _timeService.Atualizar(time);
                return Ok(MapearParaDto(time));
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            try
            {
                var time = await _timeService.ObterPorId(id);
                if (time == null) return NotFound("Time não encontrado.");

                time.Ativo = !time.Ativo;
                await _timeService.Atualizar(time);
                return Ok(MapearParaDto(time));
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _timeService.Remover(id);
            return NoContent();
        }
    }
}
