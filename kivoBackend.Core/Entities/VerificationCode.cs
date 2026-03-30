using kivoBackend.Core.Enums;
using System;

namespace kivoBackend.Core.Entities
{
    /// <summary>
    /// Entidade genérica para qualquer tipo de verificação por código/token
    /// Reutilizável para: reativação, recuperação de senha, 2FA, etc
    /// </summary>
    public class VerificationCode
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UsuarioId { get; set; }
        public string Codigo { get; set; }  // 6 dígitos ou token
        public VerificationCodeType Tipo { get; set; }
        public bool Usado { get; set; } = false;
        public DateTime ExpiraEm { get; set; }
        public DateTime CriadoEm { get; set; } = DateTime.Now;

        // Navigation
        public Usuario Usuario { get; set; }
    }
}
