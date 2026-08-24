using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace kivoBackend.Application.DTO.Asaas
{
    public class AsaasCriarCobrancaRequestDTO
    {
        [JsonPropertyName("customer")]
        public string Customer { get; set; } = string.Empty;

        [JsonPropertyName("billingType")]
        public string BillingType { get; set; } = "PIX";

        [JsonPropertyName("value")]
        public decimal Value { get; set; }

        [JsonPropertyName("dueDate")]
        public string DueDate { get; set; } = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("externalReference")]
        public string ExternalReference { get; set; } = string.Empty;
    }
}
