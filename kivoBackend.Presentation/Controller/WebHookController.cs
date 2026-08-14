using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using kivoBackend.Application.DTO.Asaas;
using kivoBackend.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace kivoBackend.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class WebhookController : ControllerBase
    {
        private readonly IIngressoService _ingressoService;

        public WebhookController(IIngressoService ingressoService)
        {
            _ingressoService = ingressoService;
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
            try
            {
                using var reader = new StreamReader(Request.Body);
                var jsonString = await reader.ReadToEndAsync();

                Console.WriteLine("========================================");
                Console.WriteLine($"[WEBHOOK RECEBIDO]: {jsonString}");
                Console.WriteLine("========================================");

                if (string.IsNullOrWhiteSpace(jsonString))
                {
                    return Ok();
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var payload = JsonSerializer.Deserialize<AsaasWebHookPayloadDTO>(jsonString, options);

                if (payload?.Payment != null && !string.IsNullOrWhiteSpace(payload.Payment.Id))
                {
                    var atualizou = await _ingressoService.ProcessarWebhookAsaasAsync(payload.Payment.Id, payload.Event);
                    Console.WriteLine($"[BANCO ATUALIZADO]: {atualizou}");
                }

                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOG WEBHOOK]: {ex.Message}");
                return Ok();
            }
        }
    }
}