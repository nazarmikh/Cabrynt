using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;


namespace Project.Services;

public interface IAuthService
{
    Task<PassengerProfile> RegisterPassengerAsync(RegisterRequestDto registerRequestDto);
    Task<string?> LoginAsync(LoginRequestDto loginRequestDto);
    Task<MeResponseDto?> GetMeAsync(ClaimsPrincipal claimsPrincipal);
    string HashPassword(string password, User user);
}

public class AuthService : IAuthService
{
    private readonly IPassengerRepository _passengerRepository;
    private readonly IPasswordHasher<User> _hasher;
    private readonly ITokenProvider _tokenProvider;

    public AuthService(IPassengerRepository passengerRepository, IPasswordHasher<User> hasher, ITokenProvider tokenProvider)
    {
        _hasher = hasher;
        _passengerRepository = passengerRepository;
        _tokenProvider = tokenProvider;
    }

    public async Task<PassengerProfile> RegisterPassengerAsync(RegisterRequestDto registerRequestDto)
    {
        var existingUser = await _passengerRepository.GetUserByEmailAsync(registerRequestDto.Email);
        if (existingUser != null)
        {
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

        return passengerProfile;

    }

    public async Task<string?> LoginAsync(LoginRequestDto loginRequestDto)
    {
        User? user = await _passengerRepository.GetUserByEmailAsync(loginRequestDto.Email);
        if (user == null)
        {
            return null;
        }

        user.LastLogin = DateTime.UtcNow;
        var checkHash = _hasher.VerifyHashedPassword(user, user.PasswordHash, loginRequestDto.Password);
        if (checkHash == PasswordVerificationResult.Failed)
        {
            return null;
        }

        if (checkHash == PasswordVerificationResult.Success || checkHash == PasswordVerificationResult.SuccessRehashNeeded)
        {
            string token = _tokenProvider.CreateToken(user);
            return token;
        }

        return null;
    }

    public async Task<MeResponseDto?> GetMeAsync(ClaimsPrincipal claimsPrincipal)
    {
        var sub = claimsPrincipal.FindFirstValue(JwtRegisteredClaimNames.Sub)
          ?? claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);

        var role = claimsPrincipal.FindFirstValue(ClaimTypes.Role);

        if (!int.TryParse(sub, out var userId))
            return null;

        if (role == "Passenger")
        {
            PassengerProfile? passenger = await _passengerRepository.GetPassengerByIdAsync(userId);
            if(passenger is null)
                return null;

            MeResponseDto response = new MeResponseDto()
            {
                Name = passenger.Name,
                Email = passenger.User.Email,
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
            
            if(admin is null)
                return null;

            MeResponseDto response = new MeResponseDto()
            {
                Name = "Admin",
                Email = admin.Email,
            };
            return response;
        }
        return null;
    }

    public string HashPassword(string password, User user)
    {
        string hashedPassword = _hasher.HashPassword(user, password);
        return hashedPassword;
    }



}

