namespace Project.Services;

public static class CorsOriginConfiguration
{
    public static string[] GetConfiguredOrigins(IConfiguration configuration)
    {
        var configuredOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>();

        if (configuredOrigins is { Length: > 0 })
        {
            return configuredOrigins
                .Where(origin => !string.IsNullOrWhiteSpace(origin))
                .Select(origin => origin.Trim().TrimEnd('/'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        var commaSeparatedOrigins = configuration["Cors:AllowedOrigins"];
        if (string.IsNullOrWhiteSpace(commaSeparatedOrigins))
        {
            return [];
        }

        return commaSeparatedOrigins
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(origin => origin.TrimEnd('/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}

public static class ProductionConfigurationValidator
{
    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        if (!environment.IsProduction())
        {
            return;
        }

        var errors = new List<string>();

        RequireConfiguredValue(configuration.GetConnectionString("Postgres"), "ConnectionStrings:Postgres", errors);
        RequireConfiguredValue(configuration["Admin:Password"], "Admin:Password", errors);
        RequireConfiguredValue(configuration["DataProtection:ApplicationName"], "DataProtection:ApplicationName", errors);
        RequireConfiguredValue(configuration["DataProtection:KeyDirectory"], "DataProtection:KeyDirectory", errors);

        ValidateCorsOrigins(CorsOriginConfiguration.GetConfiguredOrigins(configuration), errors);
        ValidateOptionalModelConfiguration(configuration, errors);

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Production configuration is invalid: {string.Join(" ", errors)}");
        }
    }

    private static void ValidateCorsOrigins(IReadOnlyCollection<string> origins, ICollection<string> errors)
    {
        if (origins.Count == 0)
        {
            errors.Add("Cors:AllowedOrigins must contain at least one HTTPS frontend origin.");
            return;
        }

        foreach (var origin in origins)
        {
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                || uri.IsLoopback
                || uri.AbsolutePath != "/"
                || !string.IsNullOrEmpty(uri.Query)
                || !string.IsNullOrEmpty(uri.Fragment))
            {
                errors.Add($"Cors:AllowedOrigins contains an invalid production origin: '{origin}'.");
            }
        }
    }

    private static void ValidateOptionalModelConfiguration(IConfiguration configuration, ICollection<string> errors)
    {
        if (!configuration.GetValue<bool>("TripDurationModel:Enabled"))
        {
            return;
        }

        RequireAbsoluteUri(configuration["Routing:OsrmBaseUrl"], "Routing:OsrmBaseUrl", errors);
        RequireHttpsUri(configuration["TripDurationModel:ModelArtifactUrl"], "TripDurationModel:ModelArtifactUrl", errors);
        RequireHttpsUri(configuration["TripDurationModel:MetadataUrl"], "TripDurationModel:MetadataUrl", errors);
        RequireConfiguredValue(configuration["TripDurationModel:ExpectedVersion"], "TripDurationModel:ExpectedVersion", errors);
    }

    private static void RequireConfiguredValue(string? value, string key, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Contains("change-me", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"{key} must be set to a non-placeholder value.");
        }
    }

    private static void RequireAbsoluteUri(string? value, string key, ICollection<string> errors)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out _))
        {
            errors.Add($"{key} must be an absolute URL when model inference is enabled.");
        }
    }

    private static void RequireHttpsUri(string? value, string key, ICollection<string> errors)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"{key} must be an HTTPS URL when model inference is enabled.");
        }
    }
}
