using kivoBackend.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace kivoBackend.Application.Services
{
    /// <summary>
    /// Serviço de envio de emails usando SMTP (genérico para códigos de verificação)
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Envia email genérico com código de verificação
        /// </summary>
        public async Task EnviarEmailComCodigoAsync(
            string destinatario,
            string nomeUsuario,
            string codigo,
            string titulo,
            string mensagem)
        {
            try
            {
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderPassword = _configuration["EmailSettings:SenderPassword"];
                var senderName = _configuration["EmailSettings:SenderName"] ?? "Kivo Sports";
                var enableSSL = bool.Parse(_configuration["EmailSettings:EnableSSL"] ?? "true");

                if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(senderPassword))
                {
                    throw new InvalidOperationException("Configurações de SMTP não estão completas no appsettings.json");
                }

                var corpo = ConstruirTemplateGenerico(nomeUsuario, codigo, titulo, mensagem);

                using (var client = new SmtpClient(smtpServer, smtpPort))
                {
                    client.EnableSsl = enableSSL;
                    client.Credentials = new NetworkCredential(senderEmail, senderPassword);

                    using (var mailMessage = new MailMessage())
                    {
                        mailMessage.From = new MailAddress(senderEmail, senderName);
                        mailMessage.To.Add(destinatario);
                        mailMessage.Subject = titulo;
                        mailMessage.Body = corpo;
                        mailMessage.IsBodyHtml = true;

                        await client.SendMailAsync(mailMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao enviar email: {ex.Message}");
                throw new InvalidOperationException("Erro ao enviar email. Tente novamente mais tarde.");
            }
        }

        /// <summary>
        /// Envia email com código de reativação (método mantido para compatibilidade)
        /// </summary>
        public async Task EnviarCodigoReativacaoAsync(string destinatario, string nomeUsuario, string codigo)
        {
            await EnviarEmailComCodigoAsync(
                destinatario,
                nomeUsuario,
                codigo,
                "Código de Reativação da Sua Conta",
                "Recebemos uma solicitação para reativar sua conta. Use o código abaixo para confirmar sua identidade:");
        }

        /// <summary>
        /// Constrói um template genérico para emails com código
        /// </summary>
        private string ConstruirTemplateGenerico(string nome, string codigo, string titulo, string mensagem)
        {
            return $@"
            <!DOCTYPE html>
            <html lang='pt-BR'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #00E676; padding: 20px; text-align: center; color: white; border-radius: 5px 5px 0 0; }}
                    .content {{ background-color: #f9f9f9; padding: 20px; border: 1px solid #ddd; }}
                    .code-box {{ background-color: #fff; padding: 15px; border: 2px solid #00E676; border-radius: 5px; text-align: center; margin: 20px 0; }}
                    .code {{ font-size: 32px; font-weight: bold; letter-spacing: 5px; color: #00E676; }}
                    .footer {{ background-color: #f0f0f0; padding: 15px; text-align: center; font-size: 12px; color: #666; border-radius: 0 0 5px 5px; }}
                    .warning {{ color: #ff6b6b; font-weight: bold; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Kivo Sports</h1>
                    </div>
                    <div class='content'>
                        <p>Olá <strong>{nome}</strong>,</p>

                        <p>{mensagem}</p>

                        <div class='code-box'>
                            <p style='margin: 0; font-size: 14px; color: #666;'>Seu código de verificação:</p>
                            <div class='code'>{codigo}</div>
                        </div>

                        <p><strong>⏱ Este código expira em 5 minutos.</strong></p>

                        <p class='warning'>⚠️ Se você não solicitou isto, ignore este email. Este é um email automático, não responda.</p>

                        <p>Dúvidas? Entre em contato com nosso suporte em kivosportsuporte@gmail.com</p>
                    </div>
                    <div class='footer'>
                        <p>&copy; 2026 Kivo Sports. Todos os direitos reservados.</p>
                    </div>
                </div>
            </body>
            </html>
            ";
        }

        /// <summary>
        /// Constrói o corpo do email em HTML (método legado mantido para compatibilidade)
        /// </summary>
        private string ConstruirCorpoEmail(string nomeUsuario, string codigo)
        {
            return $@"
            <!DOCTYPE html>
            <html lang='pt-BR'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #00E676; padding: 20px; text-align: center; color: white; border-radius: 5px 5px 0 0; }}
                    .content {{ background-color: #f9f9f9; padding: 20px; border: 1px solid #ddd; }}
                    .code-box {{ background-color: #fff; padding: 15px; border: 2px solid #00E676; border-radius: 5px; text-align: center; margin: 20px 0; }}
                    .code {{ font-size: 32px; font-weight: bold; letter-spacing: 5px; color: #00E676; }}
                    .footer {{ background-color: #f0f0f0; padding: 15px; text-align: center; font-size: 12px; color: #666; border-radius: 0 0 5px 5px; }}
                    .warning {{ color: #ff6b6b; font-weight: bold; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Kivo Sports</h1>
                    </div>
                    <div class='content'>
                        <p>Olá <strong>{nomeUsuario}</strong>,</p>

                        <p>Recebemos uma solicitação para reativar sua conta no Kivo Sports. Para confirmar sua identidade e reativar a conta, use o código abaixo:</p>

                        <div class='code-box'>
                            <p style='margin: 0; font-size: 14px; color: #666;'>Seu código de reativação:</p>
                            <div class='code'>{codigo}</div>
                        </div>

                        <p><strong>⏱ Este código expira em 5 minutos.</strong></p>

                        <p class='warning'>⚠️ Se você não solicitou reativar sua conta, ignore este email. Este é um email automático, não responda.</p>

                        <p>Dúvidas? Entre em contato com nosso suporte em support@kivo.com</p>
                    </div>
                    <div class='footer'>
                        <p>&copy; 2026 Kivo Sports. Todos os direitos reservados.</p>
                    </div>
                </div>
            </body>
            </html>
            ";
        }
    }
}
