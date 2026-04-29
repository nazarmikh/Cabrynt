using System.Security.Claims;

namespace Project.Services;

public interface IPaymentService
{
    Task<CreatePaymentResponseDto?> CreatePaymentAsync(CreatePaymentRequestDto request);
}

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IRideRepository _rideRepository;
    private readonly IPriceService _priceService;
    private readonly IPassengerRepository _passengerRepository;
    private readonly IInvoiceService _invoiceService;
    private readonly IEmailService _emailService;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(IPaymentRepository paymentRepository, IRideRepository rideRepository, IPriceService priceService, IPassengerRepository passengerRepository, IInvoiceService invoiceService, IEmailService emailService, ILogger<PaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _rideRepository = rideRepository;
        _priceService = priceService;
        _passengerRepository = passengerRepository;
        _invoiceService = invoiceService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<CreatePaymentResponseDto?> CreatePaymentAsync(CreatePaymentRequestDto request)
    {
        Ride? ride = await _rideRepository.GetRideByIdAsync(request.RideId);
        if (ride is null)
        {
            _logger.LogWarning("Payment creation rejected because ride {RideId} was not found", request.RideId);
            throw new InvalidOperationException("Ride not found");
        }

        if (ride.RideStatus is not RideStatus.Completed)
        {
            _logger.LogWarning("Payment creation skipped because ride {RideId} is in status {RideStatus}", ride.Id, ride.RideStatus);
            return null;
        }

        var (finalPrice, recalculatedPoints) = _priceService.GetFinalPrice(
                ride.Distance,
                ride.Duration,
                ride.Vehicle?.VehicleType ?? ride.PreferredVehicleType,
                ride.RequestTime,
                ride.PassengerProfile.Points,
                ride.DiscountCode);

        Payment payment = new Payment
        {
            PayAmount = finalPrice,
            Currency = Currency.EUR, // Add currency choise to ride request after because the payment method is called by vehicle
            TransactionReference = $"TXN-{Guid.NewGuid():N}",
            PaymentDate = DateTime.UtcNow,
            Ride = ride,
            TransactionStatus = TransactionStatus.Successful
        };

        await _paymentRepository.AddPaymentAsync(payment);
        await _passengerRepository.UpdatePassengerLoyaltyPointsByIdAsync(ride.PassengerProfile.UserId, recalculatedPoints);
        await _paymentRepository.SaveChangesAsync();
        _logger.LogInformation(
            "Payment {PaymentId} created for ride {RideId} with amount {Amount} {Currency} and transaction reference {TransactionReference}",
            payment.Id,
            ride.Id,
            payment.PayAmount,
            payment.Currency,
            payment.TransactionReference);

        var invoicePath = await _invoiceService.GenerateInvoicePdfAsync(payment);
        _logger.LogInformation("Invoice generated for payment {PaymentId} at {InvoicePath}", payment.Id, invoicePath);

        await _emailService.SendInvoiceEmailAsync(payment, invoicePath);
        _logger.LogInformation("Invoice email sent for payment {PaymentId} to passenger {PassengerUserId}", payment.Id, ride.PassengerProfile.UserId);

        CreatePaymentResponseDto response = new CreatePaymentResponseDto()
        {
            Id = payment.Id,
            PayAmount = payment.PayAmount,
            CreatedAt = payment.PaymentDate
        };

        return response;
    }
}
