using Microsoft.AspNetCore.Routing;

namespace FileService.Core.Endpoints;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}