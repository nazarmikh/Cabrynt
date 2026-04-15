namespace Project.DTOs;

public class CreatePaymentResponseDto
{
    public int Id { get; set; }
    public decimal PayAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}