using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace kivoBackend.Application.DTO.Asaas
{
    public class AsaasQRCodePixResponseDTO
    {
        [JsonPropertyName("encodedImage")]
        public string EncodedImage { get; set; } = string.Empty;

        [JsonPropertyName("payload")]
        public string Payload { get; set; } = string.Empty; 

        [JsonPropertyName("expirationDate")]
        public string ExpirationDate { get; set; } = string.Empty;
    }
}
