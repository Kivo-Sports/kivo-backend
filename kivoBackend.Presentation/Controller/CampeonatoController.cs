using kivoBackend.Application.DTO;
using kivoBackend.Application.Interfaces;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using Microsoft.AspNetCore.Mvc;

namespace kivoBackend.Presentation.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class CampeonatoController : ControllerBase
    {
        private readonly ICampeonatoService _campeonatoService;

        public CampeonatoController(ICampeonatoService campeonatoService)
        {
            _campeonatoService = campeonatoService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var campeonatos = await _campeonatoService.ObterTodosComTimes();
                var retorno = campeonatos.Select(c => MapearParaDto(c));
                return Ok(retorno);
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var campeonato = await _campeonatoService.ObterPorId(id);
                if (campeonato == null) return NotFound("Campeonato não encontrado.");
                return Ok(MapearParaDto(campeonato));
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CriarCampeonatoDto dto)
        {
            try
            {
                var novoCampeonato = new Campeonato
                {
                    Id = Guid.NewGuid(),
                    OrganizadorCampeonatoId = dto.OrganizadorCampeonatoId,
                    Nome = dto.Nome,
                    DataInicio = dto.DataInicio,
                    DataFim = dto.DataFim,
                    PontosVitoria = dto.PontosVitoria,
                    PontosDerrota = dto.PontosDerrota,
                    PontosEmpate = dto.PontosEmpate,
                    EnumStatusCampeonato = EnumStatusCampeonato.Rascunho,
                    CriadoEm = DateTime.Now
                };

                var resultado = await _campeonatoService.Adicionar(novoCampeonato);
                return CreatedAtAction(nameof(GetById), new { id = resultado.Id }, MapearParaDto(resultado));
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        private ListarCampeonatoDto MapearParaDto(Campeonato c)
        {
            return new ListarCampeonatoDto
            {
                Id = c.Id,
                OrganizadorCampeonatoId = c.OrganizadorCampeonatoId,
                Nome = c.Nome,
                DataInicio = c.DataInicio,
                DataFim = c.DataFim,
                Status = c.EnumStatusCampeonato.ToString(),
                CriadoEm = c.CriadoEm,
                TotalTimes = c.CampeonatoTimes?.Count ?? 0,
                PontosVitoria = c.PontosVitoria,
                PontosDerrota = c.PontosDerrota,
                PontosEmpate = c.PontosEmpate,
                TimeIds = c.CampeonatoTimes?
                    .Where(ct => ct.EnumStatusParticipacao != EnumStatusParticipacao.Recusado)
                    .Select(ct => ct.TimeId)
                    .ToList() ?? new List<Guid>()
            };
        }

        [HttpPatch("{id}/abrir-inscricoes")]
        public async Task<IActionResult> AbrirInscricoes(Guid id)
        {
            try
            {
                await _campeonatoService.AbrirInscricoes(id);
                return Ok("Inscrições abertas com sucesso.");
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPatch("{id}/cancelar")]
        public async Task<IActionResult> Cancelar(Guid id)
        {
            try
            {
                var campeonato = await _campeonatoService.ObterPorId(id);
                if (campeonato == null)
                    return NotFound("Campeonato não encontrado.");

                campeonato.EnumStatusCampeonato = EnumStatusCampeonato.Cancelado;
                await _campeonatoService.Atualizar(campeonato);
                return Ok("Campeonato cancelado com sucesso.");
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var campeonato = await _campeonatoService.ObterPorId(id);
                if(campeonato == null)
                    return NotFound("Campeonato não encontrado.");

                 campeonato.EnumStatusCampeonato = EnumStatusCampeonato.Cancelado;
                await _campeonatoService.Atualizar(campeonato);

                return Ok("Campeonato Cancelado com sucesso");
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost("convidar-time")]
        public async Task<IActionResult> ConvidarTime([FromBody] ConvidarTimeDTO dto)
        {
            await _campeonatoService.AdicionarTimeAoCampeonato(dto.CampeonatoId, dto.TimeId);
            return Ok("Convite enviado com sucesso.");
        }

        [HttpPatch("responder-convite/{participacaoId}")]
        public async Task<IActionResult> Responder(Guid participacaoId, [FromBody] ResponderConviteDTO dto)
        {
            await _campeonatoService.ResponderConviteCampeonato(participacaoId, dto.OrganizadorTimeId, dto.Aceito);
            return Ok("Resposta processada.");
        }

        [HttpDelete("remover-time")]
        public async Task<IActionResult> RemoverTime([FromBody] RemoverTimeCampeonatoDTO dto)
        {
            await _campeonatoService.RemoverTimeDoCampeonato(dto.CampeonatoId, dto.TimeId);
            return NoContent();
        }

        [HttpGet("convites-pendentes/{organizadorTimeId}")]
        public async Task<IActionResult> ObterConvitesPendentes(Guid organizadorTimeId)
        {
            try
            {
                var convites = await _campeonatoService.ObterConvitesPorOrganizador(organizadorTimeId);

                var retorno = convites.Select(x => new ListarConviteDTO
                {
                    ParticipacaoId = x.Id,
                    CampeonatoId = x.CampeonatoId,
                    NomeCampeonato = x.Campeonato?.Nome ?? "Campeonato não carregado",
                    NomeTime = x.Time?.Nome ?? "Time não carregado",
                    ConvidadoEm = x.ConvidadoEm,
                    DataInicio = x.Campeonato?.DataInicio ?? DateTime.MinValue,
                    DataFim = x.Campeonato?.DataFim ?? DateTime.MinValue,
                    PontosVitoria = x.Campeonato?.PontosVitoria ?? 0,
                    PontosDerrota = x.Campeonato?.PontosDerrota ?? 0,
                    PontosEmpate = x.Campeonato?.PontosEmpate ?? 0,
                    StatusCampeonato = x.Campeonato?.EnumStatusCampeonato.ToString() ?? ""
                });

                return Ok(retorno);
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }
    }
}
