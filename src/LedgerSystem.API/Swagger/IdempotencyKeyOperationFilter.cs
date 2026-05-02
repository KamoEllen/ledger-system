using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LedgerSystem.API.Swagger;

public sealed class IdempotencyKeyOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var isPost = context.ApiDescription.HttpMethod
            ?.Equals("POST", StringComparison.OrdinalIgnoreCase) ?? false;

        var path = context.ApiDescription.RelativePath ?? string.Empty;

        if (!isPost || !path.StartsWith("api/transfers", StringComparison.OrdinalIgnoreCase))
            return;

        operation.Parameters ??= new List<OpenApiParameter>();
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "Idempotency-Key",
            In = ParameterLocation.Header,
            Required = true,
            Schema = new OpenApiSchema { Type = "string", Format = "uuid" },
            Description = "Unique UUID per request. Reusing the same key replays the original response instead of creating a duplicate transfer.",
            Example = new Microsoft.OpenApi.Any.OpenApiString(Guid.NewGuid().ToString())
        });
    }
}