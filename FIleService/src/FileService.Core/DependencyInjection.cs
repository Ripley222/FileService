using FileService.Core.Endpoints;
using FileService.Core.Features;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace FileService.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;
        
        services.AddEndpoints(assembly);

        services.AddScoped<UploadFileHandler>();
        services.AddScoped<DownloadFileHandler>();
        services.AddScoped<GeneratePresignedUrlHandler>();
        services.AddScoped<GeneratePresignedUrlsHandler>();
        services.AddScoped<GenerateUploadUrlHandler>();
        services.AddScoped<DeleteFileHandler>();

        services.AddValidatorsFromAssembly(assembly);
        
        return services;
    }
}