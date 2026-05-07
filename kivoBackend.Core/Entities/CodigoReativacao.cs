using System;

namespace kivoBackend.Core.Entities
{
    /// <summary>
    /// Entidade para armazenar códigos de reativação de conta.
    /// Quando um usuário desativa uma conta, pode usar este código para reativá-la.
    /// </summary>
    public class CodigoReativacao
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UsuarioId { get; set; }
        public string Codigo { get; set; } // 6 dígitos
        public bool Usado { get; set; } = false;
        public DateTime ExpiraEm { get; set; }
        public DateTime CriadoEm { get; set; } = DateTime.Now;

        // Navigation
        public Usuario Usuario { get; set; }
    }
}
