using FileService.Core.Database;
using FileService.Core.Repositories;
using FileService.Infrastructure.Postgres.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FileService.Infrastructure.Postgres;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructurePostgres(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContextPool<FileServiceDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString(Constants.DATABASE_SECTION_NAME);
            var hostEnvironment = sp.GetRequiredService<IHostEnvironment>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            
            options.UseNpgsql(connectionString);

            if (hostEnvironment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }

            options.UseLoggerFactory(loggerFactory);
        });

        services.AddScoped<IMediaRepository, MediaRepository>();
        services.AddScoped<IReadDbContext, FileServiceDbContext>();

        return services;
    }
}