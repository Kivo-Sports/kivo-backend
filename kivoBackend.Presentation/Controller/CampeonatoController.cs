using kivoBackend.Application.DTO;
using kivoBackend.Application.Interfaces;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using kivoBackend.Presentation.Auth;
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
        private readonly IUsuarioService _usuarioService;
        private readonly ICurrentUserService _currentUser;

        public CampeonatoController(ICampeonatoService campeonatoService, IStorageService storageService, IUsuarioService usuarioService, ICurrentUserService currentUser)
        {
            _campeonatoService = campeonatoService;
            _storageService = storageService;
            _usuarioService = usuarioService;
            _currentUser = currentUser;
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

                if (dto.EsporteId == Guid.Empty)
                {
                    return BadRequest("Selecione um esporte para o campeonato.");
                }

                var organizadorCampeonatoId = await ObterOrganizadorCampeonatoIdParaCriacao(dto.OrganizadorCampeonatoId);
                if (organizadorCampeonatoId == null) return Forbid();

                var novoCampeonato = new Campeonato
                {
                    Id = Guid.NewGuid(),
                    OrganizadorCampeonatoId = organizadorCampeonatoId.Value,
                    EsporteId = dto.EsporteId,
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

                if (!await PodeAlterarCampeonato(campeonato)) return Forbid();

                if (logo != null && logo.Length > 0)
                {
                    using var stream = logo.OpenReadStream();
                    dto.LogoUrl = await _storageService.UploadFileAsync(stream, logo.FileName, logo.ContentType);
                }

                var ehAdmin = User.IsInRole("Administrador");
                var resultado = await _campeonatoService.EditarCampeonato(id, dto, ehAdmin);
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
                EsporteId = c.EsporteId,
                EsporteNome = c.Esporte?.Nome,
                EsporteIcone = c.Esporte?.Icone,
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
                if (!await PodeAlterarCampeonato(id)) return Forbid();

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
                if (!await PodeAlterarCampeonato(id)) return Forbid();

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
                if (!await PodeAlterarCampeonato(id)) return Forbid();

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

                // Admin pode excluir em qualquer etapa (remove partidas e vínculos em cascata).
                if (_currentUser.IsAdmin)
                {
                    await _campeonatoService.DeletarCampeonatoAdmin(id);
                    return Ok("Campeonato excluído com sucesso.");
                }

                if (!await PodeAlterarCampeonato(campeonato)) return Forbid();

                if (campeonato.EnumStatusCampeonato != EnumStatusCampeonato.Rascunho)
                {
                    return BadRequest("Não é possível deletar um campeonato que já saiu do rascunho. Utilize a opção de Cancelar.");
                }

                await _campeonatoService.Remover(id);
                return Ok("Campeonato excluído com sucesso.");
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPatch("{id}/descancelar")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Descancelar(Guid id)
        {
            try
            {
                await _campeonatoService.DescancelarCampeonato(id);
                return Ok("Campeonato reativado com sucesso.");
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPatch("{id}/reatribuir")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Reatribuir(Guid id, [FromBody] ReatribuirCampeonatoDTO dto)
        {
            try
            {
                await _campeonatoService.ReatribuirCampeonato(id, dto.NovoOrganizadorCampeonatoId);
                return Ok("Campeonato reatribuído com sucesso.");
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPost("convidar-time")]
        [Authorize]
        public async Task<IActionResult> ConvidarTime([FromBody] ConvidarTimeDTO dto)
        {
            try
            {
                if (!await PodeAlterarCampeonato(dto.CampeonatoId)) return Forbid();

                await _campeonatoService.AdicionarTimeAoCampeonato(dto.CampeonatoId, dto.TimeId);
                return Ok("Convite enviado com sucesso.");
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPatch("responder-convite/{participacaoId}")]
        [Authorize(Roles = "OrganizadorTime")]
        public async Task<IActionResult> Responder(Guid participacaoId, [FromBody] ResponderConviteDTO dto)
        {
            try
            {
                var organizadorTimeId = await ObterOrganizadorTimeIdAtual();
                if (organizadorTimeId == null) return Forbid();

                await _campeonatoService.ResponderConviteCampeonato(participacaoId, organizadorTimeId.Value, dto.Aceito);
                return Ok("Resposta processada.");
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpDelete("remover-time")]
        [Authorize]
        public async Task<IActionResult> RemoverTime([FromBody] RemoverTimeCampeonatoDTO dto)
        {
            try
            {
                if (!await PodeAlterarCampeonato(dto.CampeonatoId)) return Forbid();

                await _campeonatoService.RemoverTimeDoCampeonato(dto.CampeonatoId, dto.TimeId);
                return NoContent();
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpGet("convites-pendentes/{organizadorTimeId}")]
        [Authorize]
        public async Task<IActionResult> ObterConvitesPendentes(Guid organizadorTimeId)
        {
            try
            {
                if (!_currentUser.IsAdmin)
                {
                    var organizadorAtual = await ObterOrganizadorTimeIdAtual();
                    if (organizadorAtual == null || organizadorAtual.Value != organizadorTimeId)
                        return Forbid();
                }

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
                if (!await PodeAlterarCampeonato(campeonatoId)) return Forbid();

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

        private async Task<Guid?> ObterOrganizadorCampeonatoIdParaCriacao(Guid organizadorCampeonatoIdDoDto)
        {
            if (_currentUser.IsAdmin)
                return organizadorCampeonatoIdDoDto == Guid.Empty ? null : organizadorCampeonatoIdDoDto;

            if (!_currentUser.UserId.HasValue)
                return null;

            var usuario = await _usuarioService.ObterUsuarioPorId(_currentUser.UserId.Value);
            return usuario.OrganizadorCampeonato?.Id;
        }

        private async Task<Guid?> ObterOrganizadorTimeIdAtual()
        {
            if (!_currentUser.UserId.HasValue)
                return null;

            var usuario = await _usuarioService.ObterUsuarioPorId(_currentUser.UserId.Value);
            return usuario.OrganizadorTime?.Id;
        }

        private async Task<bool> PodeAlterarCampeonato(Guid campeonatoId)
        {
            var campeonato = await _campeonatoService.ObterCampeonatoPorId(campeonatoId);
            return await PodeAlterarCampeonato(campeonato);
        }

        private async Task<bool> PodeAlterarCampeonato(Campeonato campeonato)
        {
            if (_currentUser.IsAdmin)
                return true;

            if (!_currentUser.UserId.HasValue)
                return false;

            var usuario = await _usuarioService.ObterUsuarioPorId(_currentUser.UserId.Value);
            return usuario.OrganizadorCampeonato?.Id == campeonato.OrganizadorCampeonatoId;
        }
    }
}
