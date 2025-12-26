using FileService.Core.Endpoints;
using Serilog;
using Shared.Framework.Extensions;

namespace FileService.Web.Configuration;

public static class AppExtensions
{
    public static IApplicationBuilder Configure(this WebApplication app)
    {
        app.UseExceptionMiddleware();
        app.UseSerilogRequestLogging();
        app.UseSwagger();
        app.UseSwaggerUI();
        
        var apiGroup = app.MapGroup("/file-service").WithOpenApi();
        app.MapEndpoints(apiGroup);

        return app;
    }
}