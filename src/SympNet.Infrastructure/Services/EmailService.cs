using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace SympNet.Infrastructure.Services;

public class EmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    // ─────────────────────────────────────────────────────────────
    //  Shared SMTP sender (DRY — one place to configure SMTP)
    // ─────────────────────────────────────────────────────────────
    private async Task SendAsync(MailMessage mail)
    {
        var host = _config["Email:SmtpHost"]!;
        var port = int.Parse(_config["Email:SmtpPort"]!);
        var username = _config["Email:Username"]!;
        var password = _config["Email:Password"]!;

        using var smtp = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(username, password),
            EnableSsl = true
        };
        await smtp.SendMailAsync(mail);
    }

    private MailMessage BaseMessage(string to, string subject, string htmlBody)
    {
        var from = _config["Email:From"]!;
        var mail = new MailMessage
        {
            From = new MailAddress(from, "SympNet"),
            Subject = subject,
            IsBodyHtml = true,
            Body = WrapInLayout(htmlBody)
        };
        mail.To.Add(to);
        return mail;
    }

    // ─────────────────────────────────────────────────────────────
    //  1. Password reset email  (FIX: was SendResetPasswordEmailAsync)
    // ─────────────────────────────────────────────────────────────
    public async Task SendPasswordResetEmailAsync(string toEmail, string rawToken)
    {
        // Build the reset link — adjust the base URL to your frontend
        var frontendBase = _config["App:FrontendUrl"] ?? "https://sympnet.app";
        var resetLink = $"{frontendBase}/reset-password?token={rawToken}";

        var body = $"""
            <h2 style="color:#0B2D2C;font-size:20px;margin:0 0 12px">
              Réinitialisation du mot de passe
            </h2>
            <p style="color:#5E8584;font-size:14px;line-height:1.7;margin:0 0 24px">
              Vous avez demandé la réinitialisation de votre mot de passe.<br/>
              Cliquez sur le bouton ci-dessous. Ce lien est valable <strong>15 minutes</strong>.
            </p>
            <div style="text-align:center;margin:28px 0">
              <a href="{resetLink}"
                 style="background:linear-gradient(135deg,#0D6E6A,#3ABFB8);color:#fff;
                        padding:14px 36px;border-radius:10px;text-decoration:none;
                        font-weight:700;font-size:15px;display:inline-block">
                Réinitialiser mon mot de passe
              </a>
            </div>
            <p style="color:#9DBDBC;font-size:12px;line-height:1.6;margin:0">
              Si vous n'avez pas demandé cette réinitialisation, ignorez cet email.<br/>
              Lien direct : <a href="{resetLink}" style="color:#1A9E97">{resetLink}</a>
            </p>
            """;

        var mail = BaseMessage(
            toEmail,
            "Réinitialisation de votre mot de passe SympNet",
            body);

        await SendAsync(mail);
    }

    // ─────────────────────────────────────────────────────────────
    //  2. Doctor credentials email  (FIX: was missing entirely)
    // ─────────────────────────────────────────────────────────────
    public async Task SendDoctorCredentialsAsync(string toEmail, string firstName, string tempPassword)
    {
        var loginUrl = _config["App:FrontendUrl"] ?? "https://sympnet.app";

        var body = $"""
            <h2 style="color:#0B2D2C;font-size:20px;margin:0 0 12px">
              Bienvenue, Dr. {firstName} !
            </h2>
            <p style="color:#5E8584;font-size:14px;line-height:1.7;margin:0 0 16px">
              Votre compte médecin SympNet a été créé par l'administrateur.<br/>
              Voici vos identifiants de connexion temporaires :
            </p>
            <table style="background:#F0FBFA;border-radius:10px;padding:16px 24px;
                          border:1px solid #DFF0EF;margin-bottom:24px;width:100%">
              <tr>
                <td style="color:#5E8584;font-size:13px;padding:4px 0">Email</td>
                <td style="color:#0B2D2C;font-weight:700;font-size:13px">{toEmail}</td>
              </tr>
              <tr>
                <td style="color:#5E8584;font-size:13px;padding:4px 0">Mot de passe temporaire</td>
                <td style="color:#0B2D2C;font-weight:700;font-size:13px;
                           letter-spacing:2px;font-family:monospace">{tempPassword}</td>
              </tr>
            </table>
            <p style="color:#e05a00;font-size:13px;font-weight:600;margin:0 0 20px">
              ⚠️ Veuillez changer votre mot de passe dès votre première connexion.
            </p>
            <div style="text-align:center;margin:20px 0">
              <a href="{loginUrl}/login"
                 style="background:linear-gradient(135deg,#0D6E6A,#3ABFB8);color:#fff;
                        padding:14px 36px;border-radius:10px;text-decoration:none;
                        font-weight:700;font-size:15px;display:inline-block">
                Se connecter
              </a>
            </div>
            """;

        var mail = BaseMessage(
            toEmail,
            "Vos identifiants SympNet — Compte Médecin",
            body);

        await SendAsync(mail);
    }

    // ─────────────────────────────────────────────────────────────
    //  Shared HTML layout wrapper (header + footer)
    // ─────────────────────────────────────────────────────────────
    private static string WrapInLayout(string innerHtml) => $"""
        <div style="font-family:'Instrument Sans',Arial,sans-serif;max-width:520px;
                    margin:0 auto;background:#fff;border-radius:16px;overflow:hidden;
                    border:1px solid #DFF0EF">
          <div style="background:linear-gradient(135deg,#084E4B,#1A9E97);
                      padding:36px 40px;text-align:center">
            <h1 style="color:#fff;font-size:26px;margin:0;letter-spacing:-0.5px">SympNet</h1>
            <p style="color:rgba(255,255,255,0.6);font-size:12px;margin:4px 0 0">
              Système Intelligent de Support Décisionnel Clinique
            </p>
          </div>
          <div style="padding:36px 40px">
            {innerHtml}
          </div>
          <div style="background:#F0FBFA;padding:18px 40px;text-align:center;
                      border-top:1px solid #DFF0EF">
            <p style="color:#9DBDBC;font-size:12px;margin:0">
              © 2026 SympNet — Tous droits réservés
            </p>
          </div>
        </div>
        """;
}