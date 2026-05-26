using kivoBackend.Application.DTO;
using kivoBackend.Application.Interfaces;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace kivoBackend.Presentation.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class CampeonatoController : ControllerBase
    {
        private readonly ICampeonatoService _campeonatoService;
        private readonly IStorageService _storageService;

        public CampeonatoController(ICampeonatoService campeonatoService, IStorageService storageService)
        {
            _campeonatoService = campeonatoService;
            _storageService = storageService;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var campeonatos = await _campeonatoService.ObterCampeonatosComTimes();
                var retorno = campeonatos.Select(c => MapearParaDto(c));
                return Ok(retorno);
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var campeonato = await _campeonatoService.ObterCampeonatoPorId(id);
                if (campeonato == null) return NotFound("Campeonato não encontrado.");
                return Ok(MapearParaDto(campeonato));
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Post([FromForm] CriarCampeonatoDto dto, IFormFile? logo)
        {
            try
            {
                if (dto.FormatoCampeonato == EnumFormatoCampeonato.Hibrido && !dto.QuantidadeTimesClassificam.HasValue)
                {
                    return BadRequest("Para campeonatos Híbridos, você deve informar quantos times classificam.");
                }

                if (dto.FormatoCampeonato != EnumFormatoCampeonato.MataMata)
                {
                    if (!dto.PontosVitoria.HasValue || !dto.PontosDerrota.HasValue || !dto.PontosEmpate.HasValue)
                    {
                        return BadRequest("Para campeonatos de Pontos Corridos ou Híbridos, você deve informar os pontos para vitória, derrota e empate.");
                    }
                }

                string? urlImage = dto.LogoUrl;

                if (logo != null && logo.Length > 0)
                {
                    using var stream = logo.OpenReadStream();
                    urlImage = await _storageService.UploadFileAsync(stream, logo.FileName, logo.ContentType);
                }

                var novoCampeonato = new Campeonato
                {
                    Id = Guid.NewGuid(),
                    OrganizadorCampeonatoId = dto.OrganizadorCampeonatoId,
                    Nome = dto.Nome,
                    DataInicio = dto.DataInicio,
                    DataFim = dto.DataFim,
                    PontosVitoria = dto.PontosVitoria ?? 0,
                    PontosDerrota = dto.PontosDerrota ?? 0,
                    PontosEmpate = dto.PontosEmpate ?? 0,
                    LogoUrl = urlImage,
                    FormatoCampeonato = dto.FormatoCampeonato,
                    EnumStatusCampeonato = EnumStatusCampeonato.Rascunho,
                    QuantidadeTimesClassificam = dto.QuantidadeTimesClassificam ?? 0,
                    CriadoEm = DateTime.Now
                };

                var resultado = await _campeonatoService.Adicionar(novoCampeonato);
                return CreatedAtAction(nameof(GetById), new { id = resultado.Id }, MapearParaDto(resultado));
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }
        
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Put(Guid id, [FromForm] EditarCampeonatoDto dto, IFormFile? logo)
        {
            try
            {
                var campeonato = await _campeonatoService.ObterCampeonatoPorId(id);
                if (campeonato == null) return NotFound("Campeonato não encontrado.");

                if (logo != null && logo.Length > 0)
                {
                    using var stream = logo.OpenReadStream();
                    dto.LogoUrl = await _storageService.UploadFileAsync(stream, logo.FileName, logo.ContentType);
                }

                var resultado = await _campeonatoService.EditarCampeonato(id, dto);
                return Ok(MapearParaDto(resultado));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private ListarCampeonatoDto MapearParaDto(Campeonato c)
        {
            return new ListarCampeonatoDto
            {
                Id = c.Id,
                OrganizadorCampeonatoId = c.OrganizadorCampeonatoId,
                OrganizadorNome = c.OrganizadorCampeonato?.Usuario?.Nome,
                Nome = c.Nome,
                DataInicio = c.DataInicio,
                DataFim = c.DataFim,
                Status = c.EnumStatusCampeonato.ToString(),
                CriadoEm = c.CriadoEm,
                PontosVitoria = c.PontosVitoria ?? 0,
                PontosDerrota = c.PontosDerrota ?? 0,
                PontosEmpate = c.PontosEmpate ?? 0,
                LogoUrl = c.LogoUrl,
                FormatoCampeonato = c.FormatoCampeonato.ToString(),
                TotalTimes = c.CampeonatoTimes?.Count(t => t.EnumStatusParticipacao == EnumStatusParticipacao.Aceito) ?? 0,
                VencedorTimeId = c.TimeVencedorId,
                VencedorTimeNome = c.TimeVencedor?.Nome,
                VencedorTimeLogo = c.TimeVencedor?.LogoUrl,
                QuantidadeTimesClassificam = c.QuantidadeTimesClassificam ?? 0,
                Times = c.CampeonatoTimes?
                    .Where(ct => ct.EnumStatusParticipacao == EnumStatusParticipacao.Aceito)
                    .Select(ct => ct.TimeId)
                    .ToList() ?? new List<Guid>()
            };
        }

        [HttpPatch("{id}/abrir-inscricoes")]
        [Authorize]
        public async Task<IActionResult> AbrirInscricoes(Guid id)
        {
            try
            {
                await _campeonatoService.AbrirInscricoes(id);
                return Ok("Inscrições abertas com sucesso.");
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPatch("{id}/iniciar-campeonato")]
        [Authorize]
        public async Task<IActionResult> IniciarCampeonato(Guid id)
        {
            try
            {
                await _campeonatoService.IniciarCampeonato(id);
                return Ok("Campeonato iniciado com sucesso.");
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPatch("{id}/cancelar")]
        [Authorize]
        public async Task<IActionResult> Cancelar(Guid id)
        {
            try
            {
                await _campeonatoService.CancelarCampeonato(id);

                return Ok("Campeonato e suas partidas pendentes foram cancelados com sucesso.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var campeonato = await _campeonatoService.ObterCampeonatoPorId(id);
                if (campeonato == null) return NotFound("Campeonato não encontrado.");

                if (campeonato.EnumStatusCampeonato != EnumStatusCampeonato.Rascunho)
                {
                    return BadRequest("Não é possível deletar um campeonato que já saiu do rascunho. Utilize a opção de Cancelar.");
                }

                await _campeonatoService.Remover(id);
                return Ok("Campeonato excluído com sucesso.");
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPost("convidar-time")]
        [Authorize]
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
        [Authorize]
        public async Task<IActionResult> RemoverTime([FromBody] RemoverTimeCampeonatoDTO dto)
        {
            await _campeonatoService.RemoverTimeDoCampeonato(dto.CampeonatoId, dto.TimeId);
            return NoContent();
        }

        [HttpGet("convites-pendentes/{organizadorTimeId}")]
        [Authorize]
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
                    LogoUrl = x.Campeonato?.LogoUrl ?? "",
                    PontosVitoria = x.Campeonato?.PontosVitoria ?? 0,
                    PontosDerrota = x.Campeonato?.PontosDerrota ?? 0,
                    PontosEmpate = x.Campeonato?.PontosEmpate ?? 0,
                    StatusCampeonato = x.Campeonato?.EnumStatusCampeonato.ToString() ?? ""
                });

                return Ok(retorno);
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpGet("{campeonatoId}/convites")]
        [Authorize]
        public async Task<IActionResult> ObterConvitesPorCampeonato(Guid campeonatoId)
        {
            try
            {
                var convites = await _campeonatoService.ObterConvitesPorCampeonato(campeonatoId);

                var retorno = convites.Select(x => new
                {
                    ParticipacaoId = x.Id,
                    TimeId = x.TimeId,
                    NomeTime = x.Time?.Nome ?? "Time não carregado",
                    StatusParticipacao = x.EnumStatusParticipacao.ToString(),
                    ConvidadoEm = x.ConvidadoEm,
                    RespondidoEm = x.RespondidoEm
                });

                return Ok(retorno);
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }
    }
}
