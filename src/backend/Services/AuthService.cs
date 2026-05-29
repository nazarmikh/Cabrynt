using Microsoft.AspNetCore.Identity;
using System.Security.Claims;


namespace Project.Services;

public interface IAuthService
{
    Task<PassengerProfile> RegisterPassengerAsync(RegisterRequestDto registerRequestDto);
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginRequestDto);
    Task<MeResponseDto?> GetMeAsync(ClaimsPrincipal claimsPrincipal);
    Task<MeResponseDto?> UpdateMeAsync(ClaimsPrincipal claimsPrincipal, UpdateMeRequestDto request);
    string HashPassword(string password, User user);
}

public class AuthService : IAuthService
{
    private readonly IPassengerRepository _passengerRepository;
    private readonly IPasswordHasher<User> _hasher;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IPassengerRepository passengerRepository, IPasswordHasher<User> hasher, ILogger<AuthService> logger)
    {
        _hasher = hasher;
        _passengerRepository = passengerRepository;
        _logger = logger;
    }

    public async Task<PassengerProfile> RegisterPassengerAsync(RegisterRequestDto registerRequestDto)
    {
        var existingUser = await _passengerRepository.GetUserByEmailAsync(registerRequestDto.Email);
        if (existingUser != null)
        {
            _logger.LogWarning("Passenger registration rejected because email {Email} already exists", registerRequestDto.Email);
            throw new InvalidOperationException("Email is already registered");
        }

        var user = new User
        {
            Email = registerRequestDto.Email,
            Role = Enums.Role.Passenger,
            LastLogin = DateTime.UtcNow,
            AccountCreated = DateTime.UtcNow,
            PasswordHash = string.Empty
        };

        string hashedPassword = HashPassword(registerRequestDto.Password, user);
        user.PasswordHash = hashedPassword;

        await _passengerRepository.AddUserAsync(user);


        PassengerProfile passengerProfile = new PassengerProfile()
        {
            User = user,
            Name = registerRequestDto.Name,
            HomeAddress = registerRequestDto.HomeAddress,
            PreferredPaymentMethod = registerRequestDto.PreferredPaymentMethod
        };


        await _passengerRepository.AddPassengerAsync(passengerProfile);
        await _passengerRepository.SaveChangesAsync();

        _logger.LogInformation("Passenger {PassengerUserId} registered with email {Email}", user.Id, user.Email);

        return passengerProfile;

    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginRequestDto)
    {
        User? user = await _passengerRepository.GetUserByEmailAsync(loginRequestDto.Email);
        if (user == null)
        {
            _logger.LogWarning("Login failed because email {Email} was not found", loginRequestDto.Email);
            return null;
        }

        var checkHash = _hasher.VerifyHashedPassword(user, user.PasswordHash, loginRequestDto.Password);
        if (checkHash == PasswordVerificationResult.Failed)
        {
            _logger.LogWarning("Login failed for user {UserId} with email {Email} because password verification failed", user.Id, user.Email);
            return null;
        }

        if (checkHash == PasswordVerificationResult.Success || checkHash == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.LastLogin = DateTime.UtcNow;
            await _passengerRepository.SaveChangesAsync();
            _logger.LogInformation("User {UserId} with role {Role} logged in successfully", user.Id, user.Role);
            LoginResponseDto response = new LoginResponseDto(user.Id, user.Email, user.Role);
            return response;
        }

        return null;
    }

    public async Task<MeResponseDto?> GetMeAsync(ClaimsPrincipal claimsPrincipal)
    {
        var sub = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);

        var role = claimsPrincipal.FindFirstValue(ClaimTypes.Role);

        if (!int.TryParse(sub, out var userId))
            return null;

        if (role == "Passenger")
        {
            PassengerProfile? passenger = await _passengerRepository.GetPassengerByIdAsync(userId);
            if (passenger is null)
                return null;

            MeResponseDto response = new MeResponseDto()
            {
                Name = passenger.Name,
                Email = passenger.User.Email,
                Role = passenger.User.Role,
                HomeAddress = passenger.HomeAddress,
                Points = passenger.Points,
                PreferredPaymentMethod = passenger.PreferredPaymentMethod
            };

            return response;
        }
        else if (role == "Admin")
        {
            // Handle admin-specific logic
            User? admin = await _passengerRepository.GetUserByIdAsync(userId);

            if (admin is null)
                return null;

            MeResponseDto response = new MeResponseDto()
            {
                Name = "Admin",
                Email = admin.Email,
                Role = admin.Role,
            };
            return response;
        }
        return null;
    }

    public async Task<MeResponseDto?> UpdateMeAsync(ClaimsPrincipal claimsPrincipal, UpdateMeRequestDto request)
    {
        var sub = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);

        var role = claimsPrincipal.FindFirstValue(ClaimTypes.Role);

        if (!int.TryParse(sub, out var userId) || role != "Passenger")
            return null;

        PassengerProfile? passenger = await _passengerRepository.GetPassengerByIdAsync(userId);
        if (passenger is null)
            return null;

        passenger.Name = request.Name;
        passenger.HomeAddress = request.HomeAddress;
        passenger.PreferredPaymentMethod = request.PreferredPaymentMethod;

        await _passengerRepository.SaveChangesAsync();

        _logger.LogInformation("Passenger {PassengerUserId} updated profile details", passenger.UserId);

        return new MeResponseDto
        {
            Name = passenger.Name,
            Email = passenger.User.Email,
            Role = passenger.User.Role,
            HomeAddress = passenger.HomeAddress,
            Points = passenger.Points,
            PreferredPaymentMethod = passenger.PreferredPaymentMethod
        };
    }

    public string HashPassword(string password, User user)
    {
        string hashedPassword = _hasher.HashPassword(user, password);
        return hashedPassword;
    }



}
