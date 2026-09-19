using Microsoft.AspNetCore.HttpOverrides;

namespace Project.Services;

public static class HostingConfiguration
{
    public static bool UsesForwardedHeaders(IConfiguration configuration) =>
        configuration.GetValue<bool>("ReverseProxy:UseForwardedHeaders");

    public static bool ShouldApplyMigrations(IConfiguration configuration, IHostEnvironment environment) =>
        environment.IsDevelopment()
        || configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup");

    public static bool ShouldExitAfterMigrations(IConfiguration configuration, IHostEnvironment environment) =>
        ShouldApplyMigrations(configuration, environment)
        && configuration.GetValue<bool>("Database:ExitAfterMigrations");

    public static void AddCabryntForwardedHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        if (!UsesForwardedHeaders(configuration))
        {
            return;
        }

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
            options.ForwardLimit = 1;
        });
    }
}
