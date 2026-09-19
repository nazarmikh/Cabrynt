using Azure.Identity;
using Microsoft.AspNetCore.DataProtection;

namespace Project.Services;

public sealed class DataProtectionKeyOptions
{
    public const string SectionName = "DataProtection";

    public string ApplicationName { get; set; } = "Cabrynt";
    public string KeyDirectory { get; set; } = "App_Data/data-protection-keys";
    public string BlobUri { get; set; } = string.Empty;
    public string KeyVaultKeyIdentifier { get; set; } = string.Empty;

    public bool UsesAzureKeyStore => !string.IsNullOrWhiteSpace(BlobUri)
        && !string.IsNullOrWhiteSpace(KeyVaultKeyIdentifier);
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

        var dataProtection = services
            .AddDataProtection()
            .SetApplicationName(options.ApplicationName);

        if (options.UsesAzureKeyStore)
        {
            var credential = new DefaultAzureCredential();

            return dataProtection
                .PersistKeysToAzureBlobStorage(new Uri(options.BlobUri, UriKind.Absolute), credential)
                .ProtectKeysWithAzureKeyVault(new Uri(options.KeyVaultKeyIdentifier, UriKind.Absolute), credential);
        }

        if (string.IsNullOrWhiteSpace(options.KeyDirectory))
        {
            throw new InvalidOperationException("Data protection key directory cannot be empty when Azure key storage is not configured.");
        }

        var keyDirectory = new DirectoryInfo(System.IO.Path.GetFullPath(options.KeyDirectory));
        keyDirectory.Create();

        return dataProtection
            .PersistKeysToFileSystem(keyDirectory);
    }
}
