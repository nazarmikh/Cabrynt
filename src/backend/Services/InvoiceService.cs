using System.Text;

namespace Project.Services;

public interface IInvoiceService
{
    Task<string> GenerateInvoicePdfAsync(Payment payment);
}

public class InvoiceService : IInvoiceService
{
    private const decimal VatMultiplier = 1.21m;
    private readonly IWebHostEnvironment _environment;

    public InvoiceService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> GenerateInvoicePdfAsync(Payment payment)
    {
        var invoicesDirectory = System.IO.Path.Combine(_environment.ContentRootPath, "GeneratedInvoices");
        Directory.CreateDirectory(invoicesDirectory);

        var fileName = $"invoice-{payment.TransactionReference}.pdf";
        var fullPath = System.IO.Path.Combine(invoicesDirectory, fileName);

        var preVatAmount = Math.Round(payment.PayAmount / VatMultiplier, 2, MidpointRounding.AwayFromZero);
        var vatAmount = payment.PayAmount - preVatAmount;

        var lines = new List<string>
        {
            "Cabrynt Invoice",
            $"Invoice Number: {payment.Id}",
            $"Transaction Reference: {payment.TransactionReference}",
            $"Payment Status: {payment.TransactionStatus}",
            $"Payment Date (UTC): {payment.PaymentDate:yyyy-MM-dd HH:mm:ss}",
            $"Passenger: {payment.Ride.PassengerProfile.Name}",
            $"Email: {payment.Ride.PassengerProfile.User.Email}",
            $"Route: {payment.Ride.DepartureLocation} -> {payment.Ride.DestinationLocation}",
            $"Ride Request Time (UTC): {payment.Ride.RequestTime:yyyy-MM-dd HH:mm:ss}",
            $"Vehicle Type: {payment.Ride.Vehicle?.VehicleType ?? payment.Ride.PreferredVehicleType}",
            $"Distance (km): {payment.Ride.Distance:F2}",
            $"Duration (min): {payment.Ride.Duration:F2}",
            $"Subtotal excl. VAT: {preVatAmount:F2} {payment.Currency}",
            $"VAT (21%): {vatAmount:F2} {payment.Currency}",
            $"Total incl. VAT: {payment.PayAmount:F2} {payment.Currency}"
        };

        var pdfBytes = BuildPdf(lines);
        await File.WriteAllBytesAsync(fullPath, pdfBytes);

        return fullPath;
    }

    private static byte[] BuildPdf(IReadOnlyList<string> lines)
    {
        var contentBuilder = new StringBuilder();
        contentBuilder.AppendLine("BT");
        contentBuilder.AppendLine("/F1 22 Tf");
        contentBuilder.AppendLine("50 790 Td");
        contentBuilder.AppendLine($"({EscapePdfText(lines[0])}) Tj");
        contentBuilder.AppendLine("/F1 12 Tf");

        for (var i = 1; i < lines.Count; i++)
        {
            contentBuilder.AppendLine("0 -20 Td");
            contentBuilder.AppendLine($"({EscapePdfText(lines[i])}) Tj");
        }

        contentBuilder.AppendLine("ET");

        var contentStream = contentBuilder.ToString();
        var contentLength = Encoding.ASCII.GetByteCount(contentStream);

        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Count 1 /Kids [3 0 R] >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {contentLength} >>\nstream\n{contentStream}endstream"
        };

        var pdfBuilder = new StringBuilder();
        pdfBuilder.AppendLine("%PDF-1.4");

        var offsets = new List<int>();

        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(pdfBuilder.ToString()));
            pdfBuilder.AppendLine($"{i + 1} 0 obj");
            pdfBuilder.AppendLine(objects[i]);
            pdfBuilder.AppendLine("endobj");
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(pdfBuilder.ToString());

        pdfBuilder.AppendLine("xref");
        pdfBuilder.AppendLine($"0 {objects.Count + 1}");
        pdfBuilder.AppendLine("0000000000 65535 f ");

        foreach (var offset in offsets)
        {
            pdfBuilder.AppendLine($"{offset:D10} 00000 n ");
        }

        pdfBuilder.AppendLine("trailer");
        pdfBuilder.AppendLine($"<< /Size {objects.Count + 1} /Root 1 0 R >>");
        pdfBuilder.AppendLine("startxref");
        pdfBuilder.AppendLine($"{xrefOffset}");
        pdfBuilder.Append("%%EOF");

        return Encoding.ASCII.GetBytes(pdfBuilder.ToString());
    }

    private static string EscapePdfText(string value)
    {
        var escaped = value
            .Replace("\\", "\\\\")
            .Replace("(", "\\(")
            .Replace(")", "\\)");

        var sanitized = new StringBuilder(escaped.Length);

        foreach (var character in escaped)
        {
            sanitized.Append(character <= 126 ? character : '?');
        }

        return sanitized.ToString();
    }
}
