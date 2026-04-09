namespace Project.DTOs;
using System.ComponentModel.DataAnnotations;

public class RegisterRequestDto
{
    public required string Email {get;set;}
    public required string Password {get;set;}
    public required string Name {get;set;}
    public required string HomeAddress {get;set;}
    public required string PreferredPaymentMethod {get;set;}
}