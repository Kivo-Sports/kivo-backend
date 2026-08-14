using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace kivoBackend.Application.DTO.Asaas
{
    public class AsaasWebHookPayloadDTO
    {
        [JsonPropertyName("event")]
        public string Event { get; set; } = string.Empty; 

        [JsonPropertyName("payment")]
        public AsaasWebHookPaymentDTO Payment { get; set; } = null!;
    }
}
