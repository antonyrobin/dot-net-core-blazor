using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BlazorApp.App.Health
{
    public static class MongoHealthEndpointExtensions
    {
        public static IEndpointRouteBuilder MapMongoHealthEndpoints(
        this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints
                .MapGroup("/api/health")
                .WithTags("Health");   // good for Swagger / OpenAPI

            group.MapGet("/mongo-ping", HandleMongoPingAsync)
                .WithName("MongoPing")
                //.WithOpenApi()         // if you use Swagger
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status503ServiceUnavailable);

            // You can add more health endpoints later, e.g.:
            // group.MapGet("/ready", HandleReadinessAsync);
            // group.MapGet("/live",  HandleLivenessAsync);

            group.MapGet("/mongo-ping2", Ping)
                .WithName("MongoPing2")
                //.WithOpenApi()         // if you use Swagger
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status503ServiceUnavailable);

            return endpoints;
        }

        private static async Task<IResult> HandleMongoPingAsync(MongoPingHandler handler) => await handler.HandleAsync();

        private static async Task<IResult> Ping(MongoPingHandler handler) => await handler.Ping();
    }
}
