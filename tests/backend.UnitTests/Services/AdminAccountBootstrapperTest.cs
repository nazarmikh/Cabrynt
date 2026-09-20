using Microsoft.AspNetCore.Identity;
using Project.Enums;
using Project.Models;
using Project.Services;

namespace backend.UnitTests.Services;

public class AdminAccountBootstrapperTest
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    [Fact]
    public void CreateOrPromote_CreatesAdminWhenNoUserExists()
    {
        var createdAt = new DateTime(2026, 9, 20, 12, 0, 0, DateTimeKind.Utc);

        var user = AdminAccountBootstrapper.CreateOrPromote(
            null,
            "admin@cabrynt.test",
            "AdminPassword123!",
            _passwordHasher,
            createdAt);

        Assert.Equal(Role.Admin, user.Role);
        Assert.Equal("admin@cabrynt.test", user.Email);
        Assert.Equal(createdAt, user.AccountCreated);
        Assert.Equal(
            PasswordVerificationResult.Success,
            _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, "AdminPassword123!"));
    }

    [Fact]
    public void CreateOrPromote_PromotesExistingUserAndResetsBootstrapPassword()
    {
        var existingUser = new User
        {
            Email = "nazar.mikhin@gmail.com",
            Role = Role.Passenger,
            PasswordHash = "old-password-hash",
            AccountCreated = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            LastLogin = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var result = AdminAccountBootstrapper.CreateOrPromote(
            existingUser,
            "nazar.mikhin@gmail.com",
            "AdminPassword123!",
            _passwordHasher,
            DateTime.UtcNow);

        Assert.Same(existingUser, result);
        Assert.Equal(Role.Admin, result.Role);
        Assert.Equal(
            PasswordVerificationResult.Success,
            _passwordHasher.VerifyHashedPassword(result, result.PasswordHash, "AdminPassword123!"));
    }
}
