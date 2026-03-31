using kivoBackend.Application.DTO;
using kivoBackend.Application.Interfaces;
using kivoBackend.Application.Services;
using kivoBackend.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace kivoBackend.Presentation.Controller
{
    [ApiController]
    [Route("api/auth")] 
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUsuarioService _usuarioService;
        private readonly TokenService _tokenService;

        public AuthController(UserManager<IdentityUser> userManager, IUsuarioService usuarioService, TokenService tokenService)
        {
            _userManager = userManager;
            _usuarioService = usuarioService;
            _tokenService = tokenService;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDto)
        {
            IdentityUser? identityUser = null;
            Usuario? usuarioKivo = null;

            if (loginDto.Identificador.Contains("@"))
            {
                identityUser = await _userManager.FindByEmailAsync(loginDto.Identificador);
                if (identityUser != null)
                    usuarioKivo = await _usuarioService.ObterUsuarioPorEmail(identityUser.Email!);
            }
            else
            {
                usuarioKivo = await _usuarioService.ObterUsuarioPorCpf(loginDto.Identificador);
                if (usuarioKivo != null)
                    identityUser = await _userManager.FindByEmailAsync(usuarioKivo.Email!);
            }

            if (identityUser == null || usuarioKivo == null)
                return Unauthorized(new { message = "Usuário não encontrado ou inválido." });

            if (!usuarioKivo.Ativo)
                return Unauthorized(new { message = "Esta conta está desativada." });

            var loginValido = await _userManager.CheckPasswordAsync(identityUser, loginDto.Senha);
            if (!loginValido)
                return Unauthorized(new { message = "Senha incorreta." });

            var token = await _tokenService.GenerateToken(identityUser, usuarioKivo);

            return Ok(new
            {
                token = token,
                usuario = new
                {
                    usuarioKivo.Nome,
                    Cargo = usuarioKivo.EnumCargo.ToString(),
                    usuarioKivo.Id
                }
            });
        }

        /// <summary>
        /// Endpoint para solicitar envio de código de reativação por email
        /// </summary>
        [AllowAnonymous]
        [HttpPost("enviar-codigo-reativacao")]
        public async Task<IActionResult> EnviarCodigoReativacao([FromBody] EnviarCodigoReativacaoDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { message = "Email inválido" });

                await _usuarioService.GerarCodigoReativacao(dto.Email);

                return Ok(new { message = "Código de reativação enviado com sucesso para o email fornecido" });
            }
            catch (KeyNotFoundException)
            {
                return BadRequest(new { message = "Usuário não encontrado" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erro ao enviar código: {ex.Message}" });
            }
        }

        /// <summary>
        /// Endpoint para confirmar reativação de conta com código de 6 dígitos
        /// </summary>
        [AllowAnonymous]
        [HttpPost("confirmar-reativacao")]
        public async Task<IActionResult> ConfirmarReativacao([FromBody] ConfirmarReativacaoDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { message = "Dados inválidos" });

                await _usuarioService.ConfirmarReativacao(dto.Email, dto.Codigo);

                return Ok(new { message = "Conta reativada com sucesso! Você já pode fazer login" });
            }
            catch (KeyNotFoundException)
            {
                return BadRequest(new { message = "Usuário não encontrado" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erro ao confirmar reativação: {ex.Message}" });
            }
        }
    }
}