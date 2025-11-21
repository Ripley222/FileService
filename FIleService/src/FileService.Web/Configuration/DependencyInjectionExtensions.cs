using Serilog;
using Serilog.Exceptions;

namespace FileService.Web.Configuration;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddConfiguration(
        this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddSerilogLogging(configuration)
            .AddOpenApiSpec();
    }
    
    private static IServiceCollection AddOpenApiSpec(this IServiceCollection services)
    {
        return services
            .AddOpenApi()
            .AddSwaggerGen();
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