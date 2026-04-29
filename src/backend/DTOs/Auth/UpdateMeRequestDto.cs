using Project.Enums;

namespace Project.DTOs;

public class UpdateMeRequestDto
{
    public required string Name { get; set; }
    public required string HomeAddress { get; set; }
    public required PaymentMethod PreferredPaymentMethod { get; set; }
}
