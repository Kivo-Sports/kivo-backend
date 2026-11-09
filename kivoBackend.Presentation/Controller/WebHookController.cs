using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using kivoBackend.Application.DTO.Asaas;
using kivoBackend.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace kivoBackend.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class WebhookController : ControllerBase
    {
        // Cabeçalho que o Asaas envia com o "Token de autenticação" configurado no painel do webhook
        private const string HeaderTokenAsaas = "asaas-access-token";

        private readonly IIngressoService _ingressoService;
        private readonly IConfiguration _configuration;

        public WebhookController(IIngressoService ingressoService, IConfiguration configuration)
        {
            _ingressoService = ingressoService;
            _configuration = configuration;
        }

        [HttpGet("asaas")]
        [HttpHead("asaas")]
        public IActionResult PingAsaas()
        {
            return Ok(new { message = "Webhook Kivo ativo e operante!" });
        }

        [HttpPost("asaas")]
        public async Task<IActionResult> ReceberWebhookAsaas()
        {
            if (!TokenAsaasValido())
            {
                Console.WriteLine("[WEBHOOK RECUSADO]: token asaas-access-token ausente ou inválido.");
                return Unauthorized();
            }

            string jsonString;
            AsaasWebHookPayloadDTO? payload;
            try
            {
                using var reader = new StreamReader(Request.Body);
                jsonString = await reader.ReadToEndAsync();

                Console.WriteLine("========================================");
                Console.WriteLine($"[WEBHOOK RECEBIDO]: {jsonString}");
                Console.WriteLine("========================================");

                if (string.IsNullOrWhiteSpace(jsonString))
                {
                    return Ok();
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                payload = JsonSerializer.Deserialize<AsaasWebHookPayloadDTO>(jsonString, options);
            }
            catch (JsonException ex)
            {
                // Payload malformado não melhora com reenvio: confirma o recebimento para o Asaas não insistir
                Console.WriteLine($"[LOG WEBHOOK]: payload inválido: {ex.Message}");
                return Ok();
            }

            if (payload?.Payment == null || string.IsNullOrWhiteSpace(payload.Payment.Id))
            {
                return Ok();
            }

            try
            {
                var atualizou = await _ingressoService.ProcessarWebhookAsaasAsync(payload.Payment.Id, payload.Event);
                Console.WriteLine($"[BANCO ATUALIZADO]: {atualizou}");
                return Ok();
            }
            catch (Exception ex)
            {
                // Falha ao processar (ex.: Asaas fora do ar na confirmação): 500 faz o Asaas reenviar depois
                Console.WriteLine($"[LOG WEBHOOK]: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // Sem ASAAS_WEBHOOK_TOKEN configurado (ex.: desenvolvimento local) o webhook aceita qualquer chamada;
        // mesmo assim o IngressoService só altera ingressos depois de confirmar o status no Asaas.
        private bool TokenAsaasValido()
        {
            var tokenEsperado = _configuration["ASAAS_WEBHOOK_TOKEN"];
            if (string.IsNullOrWhiteSpace(tokenEsperado))
            {
                return true;
            }

            var tokenRecebido = Request.Headers[HeaderTokenAsaas].ToString();
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(tokenRecebido),
                Encoding.UTF8.GetBytes(tokenEsperado.Trim()));
        }
    }
}
