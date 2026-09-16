using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Project.Services;

namespace backend.UnitTests.Services;

public class DataProtectionConfigurationTest : IDisposable
{
    private readonly string _keyDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    [Fact]
    public void AddCabryntDataProtection_PersistsKeysAcrossServiceProviders()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DataProtection:ApplicationName"] = "Cabrynt.Test",
                ["DataProtection:KeyDirectory"] = _keyDirectory
            })
            .Build();

        string protectedValue;
        using (var firstServices = BuildServiceProvider(configuration))
        {
            protectedValue = firstServices
                .GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("cookie-smoke-test")
                .Protect("cookie-session");
        }

        using var secondServices = BuildServiceProvider(configuration);
        var unprotectedValue = secondServices
            .GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("cookie-smoke-test")
            .Unprotect(protectedValue);

        Assert.Equal("cookie-session", unprotectedValue);
    }

    public void Dispose()
    {
        if (Directory.Exists(_keyDirectory))
        {
            Directory.Delete(_keyDirectory, recursive: true);
        }
    }

    private static ServiceProvider BuildServiceProvider(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddCabryntDataProtection(configuration);
        return services.BuildServiceProvider();
    }
}
