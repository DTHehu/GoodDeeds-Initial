using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace GoodDeedsApi.OpenApi;

/// <summary>
/// Declares the bearer scheme, which is what puts the Authorize button in
/// Swagger UI and Scalar. Without it neither can call a protected endpoint.
/// </summary>
public sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "Opaque token from POST /api/auth/login",
            In = ParameterLocation.Header,
            Description = "Paste the accessToken returned by /api/auth/login."
        };

        return Task.CompletedTask;
    }
}

/// <summary>
/// Marks only the operations that actually require authorization, so anonymous
/// endpoints such as /api/auth/login and /health are not shown as locked.
/// </summary>
public sealed class BearerSecurityRequirementTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;

        bool allowsAnonymous = metadata.OfType<IAllowAnonymous>().Any();
        bool requiresAuth = metadata.OfType<IAuthorizeData>().Any();

        if (allowsAnonymous || !requiresAuth)
        {
            return Task.CompletedTask;
        }

        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer")] = []
            }
        ];

        return Task.CompletedTask;
    }
}
