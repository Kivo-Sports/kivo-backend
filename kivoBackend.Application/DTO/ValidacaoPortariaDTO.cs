namespace kivoBackend.Application.DTO
{
    /// <summary>Retorno da portaria quando o ingresso é aceito e a entrada é liberada.</summary>
    public class ValidacaoPortariaDTO
    {
        public string Message { get; set; } = string.Empty;
        public Guid IngressoId { get; set; }
        public string NomeTitular { get; set; } = string.Empty;
        /// <summary>CPF com os dígitos das pontas ocultos (***.456.789-**), para conferência com o documento.</summary>
        public string CpfTitularMascarado { get; set; } = string.Empty;
        public string NomeLote { get; set; } = string.Empty;
        public string NomePartida { get; set; } = string.Empty;
        public DateTime? DataPartida { get; set; }
        public string LocalPartida { get; set; } = string.Empty;
        public DateTime DataUso { get; set; }
    }
}
