using Microsoft.EntityFrameworkCore.Metadata;
using Project.Enums;

namespace Project.DTOs;

public class MeResponseDto
{
    public string? Name { get; set; }
    public required string Email { get; set; }
    public string? HomeAddress { get; set; }
    public int Points { get; set; }
    public PaymentMethod? PreferredPaymentMethod { get; set; }
}
