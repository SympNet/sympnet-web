using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace SympNet.Infrastructure.Services;

public class EmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendDoctorCredentialsAsync(
        string toEmail,
        string firstName,
        string tempPassword)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            "SympNet Platform",
            _config["Email:From"] ?? "sympnet.platform@gmail.com"));
        message.To.Add(new MailboxAddress(
            firstName,
            toEmail ?? string.Empty));
        message.Subject = "Welcome to SympNet - Your Login Credentials";
        message.Body = new TextPart("html")
        {
            Text = $@"
                <h2>Welcome to SympNet, Dr. {firstName}!</h2>
                <p>Your account has been created by the administrator.</p>
                <p><strong>Email:</strong> {toEmail}</p>
                <p><strong>Temporary Password:</strong> {tempPassword}</p>
                <p>Please login and change your password immediately.</p>
                <br/>
                <p>Best regards,<br/>SympNet Team</p>
            "
        };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(
            _config["Email:SmtpHost"],
            int.Parse(_config["Email:SmtpPort"]!),
            SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(
            _config["Email:Username"],
            _config["Email:Password"]);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken)
    {
        var resetLink = $"http://localhost:5237/reset-password?token={resetToken}";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("SympNet Platform", _config["Email:From"]));
        message.To.Add(new MailboxAddress(toEmail, toEmail));
        message.Subject = "SympNet — Réinitialisation de votre mot de passe";
        message.Body = new TextPart("html")
        {
            Text = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='UTF-8'/>
</head>
<body style='margin:0;padding:0;background-color:#f0f4f8;font-family:Arial,sans-serif;'>
  <table width='100%' cellpadding='0' cellspacing='0'>
    <tr>
      <td align='center' style='padding:40px 0;'>
        <table width='500' cellpadding='0' cellspacing='0' style='background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 4px 20px rgba(0,0,0,0.08);'>

          <!-- Header -->
          <tr>
            <td align='center' style='background:linear-gradient(135deg,#1a6b5e,#2d9e8a);padding:40px 30px;'>
              <div style='font-size:36px;margin-bottom:8px;'>🌿</div>
              <h1 style='color:#ffffff;margin:0;font-size:26px;font-weight:bold;letter-spacing:1px;'>SympNet</h1>
              <p style='color:rgba(255,255,255,0.8);margin:6px 0 0;font-size:13px;'>Système Intelligent de Support Décisionnel Clinique</p>
            </td>
          </tr>

          <!-- Body -->
          <tr>
            <td style='padding:40px 40px 30px;'>
              <h2 style='color:#1a1a2e;font-size:22px;margin:0 0 16px;'>Réinitialisation de mot de passe</h2>
              <p style='color:#444;font-size:15px;line-height:1.6;margin:0 0 12px;'>
                Bonjour <strong>Administrateur</strong>,
              </p>
              <p style='color:#444;font-size:15px;line-height:1.6;margin:0 0 28px;'>
                Nous avons reçu une demande de réinitialisation du mot de passe associé à votre compte SympNet.
              </p>

              <!-- Button -->
              <table width='100%' cellpadding='0' cellspacing='0'>
                <tr>
                  <td align='center' style='padding-bottom:30px;'>
                    <a href='{resetLink}' style='background:linear-gradient(135deg,#1a6b5e,#2d9e8a);color:#ffffff;text-decoration:none;padding:16px 40px;border-radius:8px;font-size:16px;font-weight:bold;display:inline-block;'>
                      🔑 Réinitialiser mon mot de passe
                    </a>
                  </td>
                </tr>
              </table>

              <!-- Info Box -->
              <table width='100%' cellpadding='0' cellspacing='0'>
                <tr>
                  <td style='background:#fffbeb;border-left:4px solid #f59e0b;border-radius:6px;padding:16px 20px;'>
                    <p style='color:#92400e;font-weight:bold;margin:0 0 8px;font-size:14px;'>⚠️ Informations importantes</p>
                    <ul style='color:#78350f;font-size:13px;margin:0;padding-left:18px;line-height:2;'>
                      <li>Ce lien est valable <strong>15 minutes</strong> uniquement</li>
                      <li>Il ne peut être utilisé <strong>qu'une seule fois</strong></li>
                      <li>Si vous n'avez pas fait cette demande, ignorez cet email</li>
                    </ul>
                  </td>
                </tr>
              </table>

              <!-- Fallback link -->
              <p style='color:#888;font-size:12px;margin:24px 0 0;line-height:1.6;'>
                Si le bouton ne fonctionne pas, copiez ce lien dans votre navigateur :<br/>
                <a href='{resetLink}' style='color:#2d9e8a;word-break:break-all;'>{resetLink}</a>
              </p>
            </td>
          </tr>

          <!-- Footer -->
          <tr>
            <td align='center' style='background:#f8fafc;padding:20px;border-top:1px solid #e2e8f0;'>
              <p style='color:#94a3b8;font-size:12px;margin:0;'>
                © 2026 SympNet · TEK-UP University<br/>
                Cet email a été envoyé automatiquement, merci de ne pas y répondre.
              </p>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>"
        };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_config["Email:SmtpHost"],
            int.Parse(_config["Email:SmtpPort"]!), SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_config["Email:Username"], _config["Email:Password"]);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);
    }
}