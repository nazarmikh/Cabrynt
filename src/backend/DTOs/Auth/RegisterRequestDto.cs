namespace Project.DTOs;
using System.ComponentModel.DataAnnotations;
using Project.Enums;

public class RegisterRequestDto
{
    public required string Email {get;set;}
    public required string Password {get;set;}
    public required string Name {get;set;}
    public required string HomeAddress {get;set;}
    public required PaymentMethod PreferredPaymentMethod {get;set;}
}
