using Microsoft.Extensions.Options;

namespace HrPayrollSystem.Services;

public class EmailOptions
{
    public bool DevMode { get; set; } = true;             // true = write .eml files to disk
    public string DevFolder { get; set; } = "DevEmails";
    public string SmtpHost { get; set; } = string.Empty;  // e.g. smtp.gmail.com
    public int SmtpPort { get; set; } = 587;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderPassword { get; set; } = string.Empty;
    public string SenderName { get; set; } = "HR Payroll System";
}

public class SmtpEmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailOptions> options, ILogger<SmtpEmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendPayslipEmailAsync(string toEmail, string employeeName,
        int year, int month, decimal netPay)
    {
        var subject = $"Your Payslip — {new DateTime(year, month, 1):MMMM yyyy}";
        var body =
            $"<p>Hi {employeeName},</p>" +
            $"<p>Your payslip for <strong>{new DateTime(year, month, 1):MMMM yyyy}</strong> is ready.</p>" +
            $"<p><strong>Net Pay: {netPay:N2}</strong></p>" +
            $"<p>Log in to the portal to view and download the full PDF payslip.</p>" +
            $"<p>— HR Payroll System (automated)</p>";

        if (_options.DevMode)
        {
            // ===== DEV: write .eml file (opens in any mail client / Outlook) =====
            Directory.CreateDirectory(_options.DevFolder);
            var safeTo = toEmail.Replace("@", "_at_");
            var path = Path.Combine(_options.DevFolder,
                $"{DateTime.Now:yyyyMMdd_HHmmss}_{safeTo}.eml");

            var eml =
                $"From: {_options.SenderName} <{_options.SenderEmail}>\r\n" +
                $"To: <{toEmail}>\r\n" +
                $"Subject: {subject}\r\n" +
                "MIME-Version: 1.0\r\n" +
                "Content-Type: text/html; charset=utf-8\r\n" +
                $"\r\n{body}";

            await File.WriteAllTextAsync(path, eml);
            _logger.LogInformation("DEV email written: {Path} → {To}", path, toEmail);
            return;
        }

        // ===== PRODUCTION: real SMTP =====
        using var client = new System.Net.Mail.SmtpClient(_options.SmtpHost, _options.SmtpPort)
        {
            Credentials = new System.Net.NetworkCredential(_options.SenderEmail, _options.SenderPassword),
            EnableSsl = true
        };

        var mail = new System.Net.Mail.MailMessage(
            $"{_options.SenderName} <{_options.SenderEmail}>", toEmail, subject, body)
        {
            IsBodyHtml = true
        };

        await client.SendMailAsync(mail);
        _logger.LogInformation("Email sent to {To}", toEmail);
    }
}