using kivoBackend.Application.DTO.Asaas;
using kivoBackend.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace kivoBackend.Application.Services
{
    public class AsaasService : IAsaasService
    {
        private readonly HttpClient _httpClient;
        private readonly string _asaasApiKey;

        public AsaasService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;

            var apiKey = Environment.GetEnvironmentVariable("ASAAS_API_KEY")
                         ?? configuration["Asaas:ApiKey"]
                         ?? string.Empty;

            var baseUrl = Environment.GetEnvironmentVariable("ASAAS_BASE_URL")
                          ?? configuration["Asaas:BaseUrl"]
                          ?? "https://sandbox.asaas.com/api/v3";

            _asaasApiKey = apiKey.Trim().Trim('"');

            if (string.IsNullOrWhiteSpace(_asaasApiKey) || _asaasApiKey.Contains("SUA_CHAVE_AQUI"))
            {
                throw new Exception("Configuração ausente: A chave ASAAS_API_KEY não foi encontrada no .env nem no appsettings.json.");
            }

            _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("access_token", _asaasApiKey);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "KivoSports/1.0");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<string> ObterOuCriarClienteAsync(string nome, string cpf, string email)
        {
            var cpfLimpo = Regex.Replace(cpf ?? "", @"[^\d]", "");

            if (!ValidarCpfAlgoritmo(cpfLimpo))
            {
                cpfLimpo = "44161113038";
            }

            try
            {
                var searchResponse = await _httpClient.GetAsync($"customers?cpfCnpj={cpfLimpo}");
                if (searchResponse.IsSuccessStatusCode)
                {
                    var searchContent = await searchResponse.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(searchContent);
                    if (doc.RootElement.TryGetProperty("data", out var data) && data.GetArrayLength() > 0)
                    {
                        return data[0].GetProperty("id").GetString()!;
                    }
                }
            }
            catch
            {
                // Se falhar a busca, segue para tentar criar
            }

            var novoCliente = new
            {
                name = !string.IsNullOrWhiteSpace(nome) ? nome : "Torcedor Kivo",
                cpfCnpj = cpfLimpo,
                email = !string.IsNullOrWhiteSpace(email) ? email : "torcedor@kivo.com"
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(novoCliente), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("customers", jsonContent);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Status: {response.StatusCode} - Erro Asaas: {responseBody}");
            }

            using var resDoc = JsonDocument.Parse(responseBody);
            return resDoc.RootElement.GetProperty("id").GetString()!;
        }

        private bool ValidarCpfAlgoritmo(string cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf) || cpf.Length != 11)
                return false;

            bool todosIguais = true;
            for (int i = 1; i < 11 && todosIguais; i++)
                if (cpf[i] != cpf[0])
                    todosIguais = false;

            if (todosIguais)
                return false;

            int[] multiplicador1 = new int[9] { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplicador2 = new int[10] { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

            string tempCpf = cpf.Substring(0, 9);
            int soma = 0;

            for (int i = 0; i < 9; i++)
                soma += int.Parse(tempCpf[i].ToString()) * multiplicador1[i];

            int resto = soma % 11;
            resto = resto < 2 ? 0 : 11 - resto;

            string digito = resto.ToString();
            tempCpf = tempCpf + digito;
            soma = 0;

            for (int i = 0; i < 10; i++)
                soma += int.Parse(tempCpf[i].ToString()) * multiplicador2[i];

            resto = soma % 11;
            resto = resto < 2 ? 0 : 11 - resto;

            digito = digito + resto.ToString();

            return cpf.EndsWith(digito);
        }

        public async Task<AsaasCobrancaResponseDTO> CriarCobrancaPixAsync(string customerId, decimal valor, string descricao, string externalReference)
        {
            var payload = new AsaasCriarCobrancaRequestDTO
            {
                Customer = customerId,
                BillingType = "PIX",
                Value = valor,
                DueDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd"),
                Description = descricao,
                ExternalReference = externalReference
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("payments", jsonContent);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Status: {response.StatusCode} - Erro Asaas Cobrança: {responseBody}");
            }

            return JsonSerializer.Deserialize<AsaasCobrancaResponseDTO>(responseBody)!;
        }

        public async Task<AsaasQRCodePixResponseDTO> ObterQrCodePixAsync(string paymentId)
        {
            var response = await _httpClient.GetAsync($"payments/{paymentId}/pixQrCode");
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Status: {response.StatusCode} - Erro Asaas QR Code: {responseBody}");
            }

            return JsonSerializer.Deserialize<AsaasQRCodePixResponseDTO>(responseBody)!;
        }

        public async Task<string> ConsultarStatusCobrancaAsync(string paymentId)
        {
            var response = await _httpClient.GetAsync($"payments/{paymentId}");
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Erro ao consultar cobrança no Asaas: {responseBody}");
            }

            using var doc = JsonDocument.Parse(responseBody);
            return doc.RootElement.GetProperty("status").GetString() ?? "PENDING";
        }
    }
}