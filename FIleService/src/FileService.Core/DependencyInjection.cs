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
        services.AddScoped<MultipartUploadHandler>();
        services.AddScoped<DownloadFileHandler>();
        services.AddScoped<GetMediaAssetInfoHandler>();
        services.AddScoped<GeneratePresignedUrlHandler>();
        services.AddScoped<GetMediaAssetsInfoHandler>();
        services.AddScoped<GenerateUploadUrlHandler>();
        services.AddScoped<DeleteFileHandler>();

        services.AddValidatorsFromAssembly(assembly);
        
        return services;
    }
}