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
          _config["Email:From"] ??     "sympnet.platform@gmail.com"));

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
}