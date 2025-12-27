using Serilog;
using Serilog.Exceptions;

namespace FileService.Web.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddConfiguration(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSerilogLogging(configuration);
        services.AddOpenApiSpec();

        return services;
    }
    
    private static IServiceCollection AddOpenApiSpec(this IServiceCollection services)
    {
        services.AddOpenApi();
        services.AddSwaggerGen();

        return services;
    }
    
    private static IServiceCollection AddSerilogLogging(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSerilog((sp, lc) => lc
            .ReadFrom.Configuration(configuration)
            .ReadFrom.Services(sp)
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .Enrich.WithProperty("ServiceName", "FileService")
        );
        
        return services;
    }
}