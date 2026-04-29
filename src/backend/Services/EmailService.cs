using System.Net;
using System.Net.Mail;

namespace Project.Services;

public interface IEmailService
{
    Task SendInvoiceEmailAsync(Payment payment, string invoicePath);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, IWebHostEnvironment environment, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public async Task SendInvoiceEmailAsync(Payment payment, string invoicePath)
    {
        var fromAddress = _configuration["Email:FromAddress"]
            ?? "no-reply@novadrive.com";

        var pickupDirectory = _configuration["Email:PickupDirectory"];
        var subject = $"Nova Drive invoice {payment.TransactionReference}";
        var body = BuildEmailBody(payment);
        var recipient = payment.Ride.PassengerProfile.User.Email;

        _logger.LogInformation(
            "Preparing invoice email for payment {PaymentId} from {FromAddress} to {Recipient}. Pickup directory configured: {HasPickupDirectory}. SMTP host configured: {HasSmtpHost}",
            payment.Id,
            fromAddress,
            recipient,
            !string.IsNullOrWhiteSpace(pickupDirectory),
            !string.IsNullOrWhiteSpace(_configuration["Email:SmtpHost"]));

        using var message = new MailMessage(fromAddress, recipient)
        {
            Subject = subject,
            Body = body
        };

        message.Attachments.Add(new Attachment(invoicePath));

        using var smtpClient = CreateSmtpClient(pickupDirectory);

        try
        {
            await smtpClient.SendMailAsync(message);
            _logger.LogInformation(
                "SMTP send completed for payment {PaymentId} to {Recipient} using delivery method {DeliveryMethod}",
                payment.Id,
                recipient,
                smtpClient.DeliveryMethod);
        }
        catch (SmtpException ex)
        {
            _logger.LogError(
                ex,
                "SMTP send failed for payment {PaymentId} from {FromAddress} to {Recipient}. Host: {Host}, Port: {Port}, SSL: {EnableSsl}, DeliveryMethod: {DeliveryMethod}",
                payment.Id,
                fromAddress,
                recipient,
                smtpClient.Host,
                smtpClient.Port,
                smtpClient.EnableSsl,
                smtpClient.DeliveryMethod);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected email failure for payment {PaymentId} from {FromAddress} to {Recipient}",
                payment.Id,
                fromAddress,
                recipient);
            throw;
        }
    }

    private SmtpClient CreateSmtpClient(string? pickupDirectory)
    {
        if (string.IsNullOrWhiteSpace(pickupDirectory)
            && string.IsNullOrWhiteSpace(_configuration["Email:SmtpHost"]))
        {
            pickupDirectory = "GeneratedEmails";
        }

        if (!string.IsNullOrWhiteSpace(pickupDirectory))
        {
            var fullPickupDirectory = System.IO.Path.IsPathRooted(pickupDirectory)
                ? pickupDirectory
                : System.IO.Path.Combine(_environment.ContentRootPath, pickupDirectory);

            Directory.CreateDirectory(fullPickupDirectory);

            _logger.LogInformation("Email service is using pickup directory mode at {PickupDirectory}", fullPickupDirectory);

            return new SmtpClient
            {
                DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                PickupDirectoryLocation = fullPickupDirectory
            };
        }

        var host = _configuration["Email:SmtpHost"]
            ?? throw new InvalidOperationException("Missing Email:SmtpHost configuration.");
        var port = int.TryParse(_configuration["Email:SmtpPort"], out var parsedPort)
            ? parsedPort
            : 25;
        var useSsl = bool.TryParse(_configuration["Email:UseSsl"], out var parsedUseSsl)
            && parsedUseSsl;
        var username = _configuration["Email:SmtpUsername"];
        var password = _configuration["Email:SmtpPassword"];

        _logger.LogInformation(
            "Email service is using SMTP mode. Host: {Host}, Port: {Port}, SSL: {UseSsl}, Username configured: {HasUsername}",
            host,
            port,
            useSsl,
            !string.IsNullOrWhiteSpace(username));

        var client = new SmtpClient(host, port)
        {
            EnableSsl = useSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        if (!string.IsNullOrWhiteSpace(username))
        {
            client.Credentials = new NetworkCredential(username, password);
        }

        return client;
    }

    private static string BuildEmailBody(Payment payment)
    {
        return $@"Hello {payment.Ride.PassengerProfile.Name},

Your Nova Drive ride has been completed and your invoice is attached as a PDF.

Transaction reference: {payment.TransactionReference}
Amount paid: {payment.PayAmount:F2} {payment.Currency}
Route: {payment.Ride.DepartureLocation} -> {payment.Ride.DestinationLocation}
Payment date (UTC): {payment.PaymentDate:yyyy-MM-dd HH:mm:ss}

Thank you for riding with Nova Drive.";
    }
}
