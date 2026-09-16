using Microsoft.AspNetCore.DataProtection;

namespace Project.Services;

public sealed class DataProtectionKeyOptions
{
    public const string SectionName = "DataProtection";

    public string ApplicationName { get; set; } = "Cabrynt";
    public string KeyDirectory { get; set; } = "App_Data/data-protection-keys";
}

public static class DataProtectionConfiguration
{
    public static IDataProtectionBuilder AddCabryntDataProtection(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DataProtectionKeyOptions>(
            configuration.GetSection(DataProtectionKeyOptions.SectionName));

        var options = configuration
            .GetSection(DataProtectionKeyOptions.SectionName)
            .Get<DataProtectionKeyOptions>()
            ?? new DataProtectionKeyOptions();

        if (string.IsNullOrWhiteSpace(options.ApplicationName))
        {
            throw new InvalidOperationException("Data protection application name cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(options.KeyDirectory))
        {
            throw new InvalidOperationException("Data protection key directory cannot be empty.");
        }

        var keyDirectory = new DirectoryInfo(System.IO.Path.GetFullPath(options.KeyDirectory));
        keyDirectory.Create();

        return services
            .AddDataProtection()
            .SetApplicationName(options.ApplicationName)
            .PersistKeysToFileSystem(keyDirectory);
    }
}
