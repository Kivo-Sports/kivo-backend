namespace kivoBackend.Application.Exceptions
{
    /// <summary>Lançada quando a portaria tenta validar um ingresso que já liberou uma entrada.</summary>
    public class IngressoJaUtilizadoException : InvalidOperationException
    {
        public string NomeTitular { get; }
        public DateTime? DataUso { get; }

        public IngressoJaUtilizadoException(string? nomeTitular, DateTime? dataUso)
            : base("Este ingresso já foi utilizado e não libera uma nova entrada.")
        {
            NomeTitular = nomeTitular ?? string.Empty;
            DataUso = dataUso;
        }
    }
}
