namespace Project.DTOs;


public record LoginResponseDto
(
    int Id,
    string Email,
    Role Role
);