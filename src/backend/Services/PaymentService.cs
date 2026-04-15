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

    public PaymentService(IPaymentRepository paymentRepository, IRideRepository rideRepository, IPriceService priceService, IPassengerRepository passengerRepository)
    {
        _paymentRepository = paymentRepository;
        _rideRepository = rideRepository;
        _priceService = priceService;
        _passengerRepository = passengerRepository;
    }

    public async Task<CreatePaymentResponseDto?> CreatePaymentAsync(CreatePaymentRequestDto request)
    {
        Ride? ride = await _rideRepository.GetRideByIdAsync(request.RideId);
        if (ride is null)
        {
            throw new InvalidOperationException("Ride not found");
        }

        if (ride.RideStatus is not RideStatus.Completed)
        {
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
        
        CreatePaymentResponseDto response = new CreatePaymentResponseDto()
        {
            Id = payment.Id,
            PayAmount = payment.PayAmount,
            CreatedAt = payment.PaymentDate
        };

        return response;
    }
}
