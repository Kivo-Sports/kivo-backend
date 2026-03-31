namespace kivoBackend.Core.Enums
{
    public enum VerificationCodeType
    {
        AccountReactivation = 1,    // Reativar conta
        PasswordReset = 2,          // Recuperar senha
        PasswordRedefinition = 3,   // Redefinir senha
        TwoFactorAuth = 4,          // 2FA
        EmailConfirmation = 5       // Confirmação de email (futuro)
    }
}
