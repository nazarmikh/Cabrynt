using Microsoft.AspNetCore.Identity;
using Project.Enums;
using Project.Models;

namespace Project.Services;

public static class AdminAccountBootstrapper
{
    public static User CreateOrPromote(
        User? existingUser,
        string email,
        string password,
        IPasswordHasher<User> passwordHasher,
        DateTime utcNow)
    {
        var adminUser = existingUser ?? new User
        {
            Email = email,
            PasswordHash = string.Empty,
            LastLogin = utcNow,
            AccountCreated = utcNow
        };

        adminUser.Role = Role.Admin;
        adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, password);

        return adminUser;
    }
}
